using Birdie.Data;
using Birdie.Process;
using Birdie.Watcher;
using System;
using System.Collections.Generic;

namespace Birdie
{
    public static class IBirdieContextDelegates
    {
        public delegate void ProcessDelegate(ProcessData processData);
        public delegate void WatchMemoryObjectDelegate(WatchMemoryObject watchMemoryObject);
        public delegate void WatchCategoryObjectDelegate(WatchCategoryObject watchCategoryObject);
        public delegate void LogMessageDelegate(ProcessData processData, LogMessage logMessage);
    }

    public interface IBirdieContext
    {
        #region Events
        event IBirdieContextDelegates.ProcessDelegate ProcessConnect;
        event IBirdieContextDelegates.ProcessDelegate ProcessDisconnect;
        event IBirdieContextDelegates.WatchMemoryObjectDelegate WatchMemoryObjectAdd;
        event IBirdieContextDelegates.WatchMemoryObjectDelegate WatchMemoryObjectRemove;
        event IBirdieContextDelegates.WatchCategoryObjectDelegate WatchCategoryObjectAdd;
        event IBirdieContextDelegates.WatchCategoryObjectDelegate WatchCategoryObjectRemove;
        event IBirdieContextDelegates.LogMessageDelegate LogMessageAdd;
        #endregion

        #region Methods

        #region Main control methods
        /// <summary>
        /// Initialize the context.
        /// </summary>
        /// <param name="birdieConfig">A valid config object</param>
        /// <returns>True if successful, else false</returns>
        bool Initialize(Config birdieConfig);

        /// <summary>
        /// Closes the context.
        /// </summary>
        void Terminate();
        #endregion 

        #endregion

        #region Properties
        Config Config { get; }

        // Not thread-safe, please use lock()
        List<ProcessData> AttachedProcesses { get; }
        #endregion
    }
}
