using System;
using Birdie.Process;
using Birdie.Interop;
using Birdie.Data;

namespace Birdie.Watcher
{
    /// <summary>
    /// This class contains information about a specific region in a processes' memory.
    /// </summary>
    public class WatchMemoryObject : WatchBaseObject
    {
        #region Methods
        public void ReadMemory()
        {
            LastError = "";
            Data = ProcessReader.ReadProcessMemory(this);

            if (Data != null)
                DataAsString = DataConverter.Convert(this);

            // Make the error visible
            if (LastError != "")
                DataAsString = LastError;
        }
        #endregion

        #region Properties
        public UInt64 MaxSize { get; internal set; }
        public string LastError { get; internal set; }
        public byte[] Data { get; internal set; }

        public UInt64 BaseAddress 
        { 
            get
            {
                return baseAddress;
            }

            internal set
            {
                baseAddress = value;
                BaseAddressAsString = string.Format("0x{0:X}", value);
            }
        }
        #endregion

        #region Fields
        private UInt64 baseAddress = 0;
        #endregion
    }
}
