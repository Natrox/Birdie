using Birdie.Data;
using Birdie.Watcher;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Birdie.Process
{
    /// <summary>
    /// Stub TODO: Expand
    /// </summary>
    public class ProcessData
    {
        #region Methods
        public void AddWatchBaseObject(UInt32 categoryHandle, WatchBaseObject watchBaseObject)
        {
            if (categoryHandle == 0)
            {
                // No category, add to root
                rootWatchBaseObjects.Add(watchBaseObject);
            }
            else if (!watchBaseObjects.ContainsKey(categoryHandle))
            {
                // Could not find the category object
                Debug.Assert(false);
                return;
            }
            else
            {
                WatchBaseObject watchObject = watchBaseObjects[categoryHandle];
                watchObject.Children.Add(watchBaseObject);
                watchBaseObject.Parent = watchObject;
            }

            watchBaseObjects.Add(watchBaseObject.Handle, watchBaseObject);
        }

        public WatchBaseObject RemoveAndGetWatchBaseObject(UInt32 watchBaseObjectHandle)
        {
            if (watchBaseObjects.ContainsKey(watchBaseObjectHandle))
            {
                WatchBaseObject watchBaseObject = watchBaseObjects[watchBaseObjectHandle];
                watchBaseObjects.Remove(watchBaseObjectHandle);

                if (watchBaseObject.Parent == null)
                    RootWatchBaseObjects.Remove(watchBaseObject);
                else
                    watchBaseObject.Parent.Children.Remove(watchBaseObject);

                return watchBaseObject;
            }

            return null;
        }
        #endregion

        #region Properties
        public IntPtr ProcessHandle { get; internal set; }
        public UInt64 ProcessId { get; internal set; }
        public string ProcessName { get; internal set; }
        public WatchObjectContainer RootWatchBaseObjects { get { return rootWatchBaseObjects; } }
        public DataConverter DataConverter { get { return dataConverter; } }
        #endregion

        #region Fields
        private WatchObjectContainer rootWatchBaseObjects = new WatchObjectContainer();
        private Dictionary<UInt32, WatchBaseObject> watchBaseObjects = new Dictionary<UInt32,WatchBaseObject>();
        private DataConverter dataConverter = new DataConverter();
        #endregion
    }
}
