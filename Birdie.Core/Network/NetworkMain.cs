using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

// Partially based on http://msdn.microsoft.com/en-us/library/bb907001.aspx

namespace Birdie.Network
{
    /// <summary>
    /// This class takes care of most of the network functionality
    /// </summary>
    internal class NetworkMain
    {
        #region Delegates, Events
        public delegate void ClientContextDelegate(ClientContext clientContext);
        #endregion

        #region Methods
        public NetworkMain(IBirdieContext birdieContext)
        {
            this.birdieContext = birdieContext;
            localEndPoint = new IPEndPoint(IPAddress.Any, birdieContext.Config.ListenPort);
        }

        ~NetworkMain()
        {
            if (listenSocket != null)
            {
                Stop();
            }
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        /// <returns>True if successful, else false.</returns>
        public bool Start()
        {
            try
            {
                listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.Bind(localEndPoint);
                listenSocket.Listen(int.MaxValue);

                reusableAcceptEventArgs.Completed += AcceptCompleted;

                StartAccept();
            }
            catch (SystemException exception)
            {
                // Connection failed
                Debug.WriteLine(String.Format(@"BirdieCore: Socket creation failed with exception: {0}", exception.ToString()));
                return false;
            }

            return true;
        }

        public void Stop()
        {
            if (listenSocket != null)
            {
                listenSocket.Close();
                listenSocket = null;

                lock (clientContextList)
                {
                    foreach (ClientContext ctx in clientContextList)
                        ctx.Socket.Close();

                    clientContextList.Clear();
                }
            }
        }

        #region Client Accept
        private void StartAccept()
        {
            // Clear the socket since it's being reused
            reusableAcceptEventArgs.AcceptSocket = null;

            bool blocking = listenSocket.AcceptAsync(reusableAcceptEventArgs);

            if (!blocking)
                ProcessAccept();
        }

        private void AcceptCompleted(object sender, SocketAsyncEventArgs asyncEventArgs)
        {
            ProcessAccept();
        }

        private void ProcessAccept()
        {
            // Closing down...
            if (reusableAcceptEventArgs.SocketError == SocketError.OperationAborted)
                return;

            ClientContext clientContext = new ClientContext() { Socket = reusableAcceptEventArgs.AcceptSocket };

            SocketAsyncEventArgs newAsyncEventArgs = new SocketAsyncEventArgs();
            newAsyncEventArgs.UserToken = clientContext;
            newAsyncEventArgs.Completed += ClientNetworkOperationCompleted;
            newAsyncEventArgs.SetBuffer(clientContext.Data, 0, clientContext.ExpectedBytes);

            if (!IPAddress.IsLoopback(((IPEndPoint)clientContext.Socket.RemoteEndPoint).Address))
                clientContext.IsRemote = true;

            lock (clientContextList)
                clientContextList.Add(clientContext);

            if (OnClientConnect != null)
                OnClientConnect(clientContext);

            bool blocking = clientContext.Socket.ReceiveAsync(newAsyncEventArgs);

            if (!blocking)
                ProcessReceive(newAsyncEventArgs);

            // Start the next accept
            StartAccept();
        }
        #endregion

        #region Client Receive/Send
        private void ClientNetworkOperationCompleted(object sender, SocketAsyncEventArgs asyncEventArgs)
        {
            switch (asyncEventArgs.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(asyncEventArgs);
                    break;
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs asyncEventArgs)
        {
            ClientContext clientContext = asyncEventArgs.UserToken as ClientContext;
            bool disconnect = false;

            if (asyncEventArgs.BytesTransferred > 0 && asyncEventArgs.SocketError == SocketError.Success)
            {
                bool complete = clientContext.IncrementTotalBytesReceived(asyncEventArgs.BytesTransferred);

                if (complete)
                {
                    switch (clientContext.IoState)
                    {
                        case IoStates.AwaitChallenge:
                            {
                                UInt64 challengeKey = BitConverter.ToUInt64(clientContext.Data, 0);

                                // Check if the challenge key matches
                                if (challengeKey != birdieContext.Config.ChallengeKey)
                                    disconnect = true;

                                clientContext.IoState = IoStates.AwaitChunkSize;
                            }
                            break;
                        case IoStates.AwaitChunkSize:
                            {
                                clientContext.ChunkSize = BitConverter.ToInt32(clientContext.Data, 0);
                                clientContext.IoState = IoStates.AwaitChunkBody;
                            }
                            break;

                        case IoStates.AwaitChunkBody:
                            {
                                if (OnCompleteDataReceived != null)
                                    OnCompleteDataReceived(clientContext);

                                if (clientContext.IsClosed)
                                    disconnect = true;

                                clientContext.IoState = IoStates.AwaitChunkSize;
                            }
                            break;
                    }

                    asyncEventArgs.SetBuffer(clientContext.Data, 0, clientContext.ExpectedBytes);

                    if (!disconnect)
                    {
                        bool blocking = clientContext.Socket.ReceiveAsync(asyncEventArgs);

                        if (!blocking)
                            ProcessReceive(asyncEventArgs);
                    }
                }
            }
            else
                disconnect = true;

            if (disconnect)
            {
                // Client error or disconnection, exit
                if (!clientContext.IsClosed)
                    clientContext.Socket.Close();

                lock (clientContextList)
                {
                    if (clientContextList.Contains(clientContext))
                        clientContextList.Remove(clientContext);
                }

                if (OnClientDisconnect != null)
                    OnClientDisconnect(clientContext);
            }
        }
        #endregion
        #endregion

        #region Properties
        public ClientContextDelegate OnCompleteDataReceived { get; set; }
        public ClientContextDelegate OnClientConnect { get; set; }
        public ClientContextDelegate OnClientDisconnect { get; set; }
        #endregion

        #region Fields
        private List<ClientContext> clientContextList = new List<ClientContext>();
        private SocketAsyncEventArgs reusableAcceptEventArgs = new SocketAsyncEventArgs();
        private IPEndPoint localEndPoint = null;
        private Socket listenSocket = null;
        private IBirdieContext birdieContext = null;
        #endregion
    }
}
