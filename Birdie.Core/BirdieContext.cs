using Birdie.Data;
using Birdie.Interop;
using Birdie.Network;
using Birdie.Process;
using Birdie.Watcher;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Birdie
{
    public static class BirdieContextFactory
    {
        public static IBirdieContext CreateBirdieContext()
        {
            return new BirdieContext();
        }
    }

    internal class BirdieContext : IBirdieContext
    {
        #region Classes
        public static class DataTypes
        {
            public const int RegisterProcess = 1;
            public const int AddWatch = 2;
            public const int RemoveWatchObject = 3;
            public const int AddCategory = 4;
            public const int AddLogMessage = 5;
        }
        #endregion

        #region Events
        public event IBirdieContextDelegates.ProcessDelegate ProcessConnect;
        public event IBirdieContextDelegates.ProcessDelegate ProcessDisconnect;
        public event IBirdieContextDelegates.WatchMemoryObjectDelegate WatchMemoryObjectAdd;
        public event IBirdieContextDelegates.WatchMemoryObjectDelegate WatchMemoryObjectRemove;
        public event IBirdieContextDelegates.WatchCategoryObjectDelegate WatchCategoryObjectAdd;
        public event IBirdieContextDelegates.WatchCategoryObjectDelegate WatchCategoryObjectRemove;
        public event IBirdieContextDelegates.LogMessageDelegate LogMessageAdd;
        #endregion

        #region Methods

        #region IBirdieContext
        public bool Initialize(Config birdieConfig)
        {
            // Don't allow re-use
            if (hasTerminated)
                return false;

            // Set privileges
            Privileges.SetPrivileges();

            // Validate the config first
            bool isConfigValid = birdieConfig.Validate();

            if (!isConfigValid)
                return false;

            Config = birdieConfig;

            // Create the network handler
            networkMain = new NetworkMain(this);

            // Register callbacks
            networkMain.OnClientConnect = ClientConnect;
            networkMain.OnClientDisconnect = ClientDisconnect;
            networkMain.OnCompleteDataReceived = DataReceived;

            bool isNetworkInitialized = networkMain.Start();

            if (!isNetworkInitialized)
                return false;

            return true;
        }

        public void Terminate()
        {
            networkMain.Stop();

            // Set the termination flag to prevent re-use
            hasTerminated = true;
        }
        #endregion

        #region Callbacks
        private void ClientConnect(ClientContext clientContext)
        {
            // We can't do much here yet since we don't have a process Id
        }

        private void ClientDisconnect(ClientContext clientContext)
        {
            lock (AttachedProcesses)
            {
                if (AttachedProcesses.Contains(clientContext.ProcessData))
                    AttachedProcesses.Remove(clientContext.ProcessData);
            }

            if (ProcessDisconnect != null && clientContext.ProcessData != null)
                ProcessDisconnect(clientContext.ProcessData);
        }

        private void DataReceived(ClientContext clientContext)
        {
            // Lock, to be safe
            lock (clientContext)
            {
                switch (BitConverter.ToUInt32(clientContext.Data, 0))
                {
                    case DataTypes.RegisterProcess:
                        RegisterProcess(clientContext);
                        break;

                    case DataTypes.AddWatch:
                        AddWatch(clientContext);
                        break;

                    case DataTypes.RemoveWatchObject:
                        RemoveWatchBaseObject(clientContext);
                        break;

                    case DataTypes.AddCategory:
                        AddCategory(clientContext);
                        break;

                    case DataTypes.AddLogMessage:
                        AddLogMessage(clientContext);
                        break;
                }
            }
        }

        private void RegisterProcess(ClientContext clientContext)
        {
            // Starting offset in 'data'
            int offset = sizeof(UInt32);

            // Remote processes are not supported by the memory watcher,
            // they can use the logging system however
            if (clientContext.IsRemote)
            {
                // Silent return, it's not an error
                return;
            }

            // All registering needs is a process Id
            UInt64 processId = BitConverter.ToUInt64(clientContext.Data, offset);

            ProcessData newProcessData = ProcessReader.OpenProcess((int)processId);

            if (newProcessData == null)
            {
                clientContext.Disconnect();
                return;
            }

            clientContext.ProcessData = newProcessData;

            lock (attachedProcesses)
                attachedProcesses.Add(newProcessData);

            if (ProcessConnect != null)
                ProcessConnect(newProcessData);
        }

        private void AddWatch(ClientContext clientContext)
        {
            // Starting offset in 'data'
            int offset = sizeof(UInt32);

            // Remote processes don't have memory watching support, ignore their pleas
            if (clientContext.IsRemote)
                return;

            // Layout of the AddWatch data chunk:
            // - Length (4b), Type string (*b)
            // - Length (4b), Name string (*b)
            // - Root handle (categories) (4b)
            // - Cross-process handle (4b)
            // - Base ptr (8b)
            // - Max size (4b)
            int typeLength = BitConverter.ToInt32(clientContext.Data, offset); offset += sizeof(Int32);
            string typeString = Encoding.ASCII.GetString(clientContext.Data, offset, typeLength); offset += typeLength;

            int nameLength = BitConverter.ToInt32(clientContext.Data, offset); offset += sizeof(Int32);
            string nameString = Encoding.ASCII.GetString(clientContext.Data, offset, nameLength); offset += nameLength;

            UInt32 rootHandle = BitConverter.ToUInt32(clientContext.Data, offset); offset += sizeof(UInt32);
            UInt32 handle = BitConverter.ToUInt32(clientContext.Data, offset); offset += sizeof(UInt32);
            UInt64 basePtr = BitConverter.ToUInt64(clientContext.Data, offset); offset += sizeof(UInt64);
            UInt32 maxSize = BitConverter.ToUInt32(clientContext.Data, offset); offset += sizeof(UInt32);

            WatchMemoryObject watchMemoryObject = new WatchMemoryObject()
            {
                ProcessData = clientContext.ProcessData,
                BaseAddress = basePtr,
                Name = nameString,
                Type = typeString,
                MaxSize = maxSize,
                Handle = handle
            };

            clientContext.ProcessData.AddWatchBaseObject(rootHandle, watchMemoryObject);

            if (WatchMemoryObjectAdd != null)
                WatchMemoryObjectAdd(watchMemoryObject);
        }

        private void RemoveWatchBaseObject(ClientContext clientContext)
        {
            // Starting offset in 'data'
            int offset = sizeof(UInt32);

            // Remote processes don't have memory watching support, ignore their pleas
            if (clientContext.IsRemote)
                return;

            // RemoveWatch only uses 1 handle variable
            UInt32 handle = BitConverter.ToUInt32(clientContext.Data, offset); offset += sizeof(UInt32);

            // Get the watch object
            WatchBaseObject watchBaseObject = clientContext.ProcessData.RemoveAndGetWatchBaseObject(handle);

            if (watchBaseObject != null)
            {
                if (watchBaseObject.GetType() == typeof(WatchMemoryObject) && WatchMemoryObjectRemove != null)
                    WatchMemoryObjectRemove((WatchMemoryObject)watchBaseObject);
                else if (watchBaseObject.GetType() == typeof(WatchCategoryObject) && WatchCategoryObjectRemove != null)
                    WatchCategoryObjectRemove((WatchCategoryObject)watchBaseObject);
            }
        }

        private void AddCategory(ClientContext clientContext)
        {
            // Starting offset in 'data'
            int offset = sizeof(UInt32);

            // Remote processes don't have memory watching support, ignore their pleas
            if (clientContext.IsRemote)
                return;

            // Layout of the AddCategory data chunk:
            // - Length (4b), Name string (*b)
            // - Root handle (categories) (4b)
            // - Cross-process handle (4b)

            int nameLength = BitConverter.ToInt32(clientContext.Data, offset); offset += sizeof(Int32);
            string nameString = Encoding.ASCII.GetString(clientContext.Data, offset, nameLength); offset += nameLength;

            UInt32 rootHandle = BitConverter.ToUInt32(clientContext.Data, offset); offset += sizeof(UInt32);
            UInt32 handle = BitConverter.ToUInt32(clientContext.Data, offset); offset += sizeof(UInt32);

            WatchCategoryObject watchCategoryObject = new WatchCategoryObject()
            {
                Name = nameString,
                ProcessData = clientContext.ProcessData,
                Handle = handle
            };

            clientContext.ProcessData.AddWatchBaseObject(rootHandle, watchCategoryObject);

            if (WatchCategoryObjectAdd != null)
                WatchCategoryObjectAdd(watchCategoryObject);
        }

        private void AddLogMessage(ClientContext clientContext)
        {
            // Starting offset in 'data'
            int offset = sizeof(UInt32);
            
            // Layout is easy:
            // - Length (4b), Message string (*b)
            // - Length (4b), Filter string (*b)

            int messageLength = BitConverter.ToInt32(clientContext.Data, offset); offset += sizeof(Int32);
            string messageString = Encoding.ASCII.GetString(clientContext.Data, offset, messageLength); offset += messageLength;

            int filterLength = BitConverter.ToInt32(clientContext.Data, offset); offset += sizeof(Int32);
            string filterString = "";

            if (filterLength > 0)
                filterString = Encoding.ASCII.GetString(clientContext.Data, offset, filterLength); offset += filterLength;

            LogMessage logMessage = new LogMessage()
            {
                Filter = filterString,
                Message = messageString,
                MessageOrigin = MessageOrigins.Client
            };

            if (LogMessageAdd != null)
                LogMessageAdd(clientContext.ProcessData, logMessage);
        }
        #endregion

        #endregion

        #region Properties
        public Config Config { get; set; }
        public List<ProcessData> AttachedProcesses { get { return attachedProcesses; } }
        #endregion

        #region Fields
        private List<ProcessData> attachedProcesses = new List<ProcessData>();
        private NetworkMain networkMain = null;
        private bool hasTerminated = false;
        #endregion
    }
}
