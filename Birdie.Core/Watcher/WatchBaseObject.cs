using Birdie.Process;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Birdie.Watcher
{
    /// <summary>
    /// Defines the base of all watch entries.
    /// </summary>
    public class WatchBaseObject : INotifyPropertyChanged
    {
        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
        internal WatchBaseObject()
        {
            Children = new WatchObjectContainer();
        }
        #endregion

        #region Properties
        public string Name
        {
            get { return name; }
            set
            {
                name = value;

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Name"));
            }
        }

        public string BaseAddressAsString
        {
            get { return baseAddressAsString; }
            internal set
            {
                baseAddressAsString = value;

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("BaseAddressAsString"));
            }
        }

        public object DataAsObject
        {
            get { return dataAsObject; }
            internal set
            {
                dataAsObject = value;

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("DataAsObject"));
            }
        }

        public UInt32 Handle { get; internal set; }
        public ProcessData ProcessData { get; internal set; }
        public string NonUniqueName { get; internal set; }
        public string Type { get; internal set; }
        public WatchBaseObject Parent { get; internal set; }
        public WatchObjectContainer Children { get; internal set; }
        public object UserData { get; set; }
        #endregion

        #region Fields
        public string name = "";
        public string baseAddressAsString = "";
        public object dataAsObject = null;
        #endregion
    }
}
