using System;
using System.Diagnostics;
using System.Net.Sockets;

namespace Birdie.Network
{
    internal enum IoStates
    {
        AwaitChallenge,
        AwaitChunkSize,
        AwaitChunkBody
    }

    /// <summary>
    /// Contains all the data necessary for the network thread
    /// </summary>
    internal class ClientContext
    {
        #region Methods
        public ClientContext()
        {
            IoState = IoStates.AwaitChallenge;
        }

        /// <summary>
        /// Returns true if expectedBytes was met.
        /// </summary>
        /// <param name="byAmount">Number of bytes to increment with</param>
        public bool IncrementTotalBytesReceived(int byAmount)
        {
            receivedBytes += byAmount;

            if (receivedBytes >= expectedBytes)
            {
                // It's unexpected to receive more bytes than reported
                Debug.Assert(receivedBytes == expectedBytes);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Disconnect in case of errors
        /// </summary>
        public void Disconnect()
        {
            Socket.Close();
            IsClosed = true;
        }
        #endregion

        #region Properties
        public Process.ProcessData ProcessData { get; set; }
        public int ChunkSize { get; set; }
        public Socket Socket { get; set; }
        public byte[] Data { get { return data; } }
        public int ExpectedBytes { get { return expectedBytes; } }
        public int ReceivedBytes { get { return receivedBytes; } }
        public bool IsClosed { get; set; }
        public bool IsRemote { get; set; }

        public IoStates IoState 
        { 
            get
            {
                return (IoStates)ioState;
            }

            set
            {
                ioState = (int)value;

                if (value == IoStates.AwaitChallenge)
                {
                    data = new byte[sizeof(UInt64)];
                    ChunkSize = 0;
                    expectedBytes = data.Length;
                    receivedBytes = 0;
                }
                else if (value == IoStates.AwaitChunkSize)
                {
                    data = new byte[sizeof(Int32)];
                    ChunkSize = 0;
                    expectedBytes = data.Length;
                    receivedBytes = 0;
                }
                else if (value == IoStates.AwaitChunkBody)
                {
                    data = new byte[ChunkSize];
                    expectedBytes = ChunkSize;
                    receivedBytes = 0;
                }
            }
        }

        #endregion

        #region Fields
        private int expectedBytes = 0;
        private int receivedBytes = 0;
        private int ioState = (int)IoStates.AwaitChallenge;
        private byte[] data = null;
        #endregion
    }
}
