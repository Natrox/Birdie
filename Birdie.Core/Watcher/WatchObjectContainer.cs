using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;
using System.Linq;

namespace Birdie.Watcher
{
    public class WatchObjectContainer : ObservableCollection<WatchBaseObject>
    {
        #region Methods
        // From http://stackoverflow.com/questions/2104614/updating-an-observablecollection-in-a-separate-thread
        public new void Add(WatchBaseObject watchBaseObject)
        {
            Action del = () =>
            {
                ResolveDuplicateName(watchBaseObject);

                base.Add(watchBaseObject);
            };

            if (Dispatcher != null)
                Dispatcher.Invoke(del);
            else
                del();
        }

        public new void Remove(WatchBaseObject watchBaseObject)
        {
            Action del = () =>
            {
                base.Remove(watchBaseObject);
            };

            if (Dispatcher != null)
                Dispatcher.Invoke(del);
            else
                del();
        }

        private void ResolveDuplicateName(WatchBaseObject watchBaseObject)
        {
            watchBaseObject.NonUniqueName = watchBaseObject.Name;

            if (!nameDictionary.ContainsKey(watchBaseObject.Name))
                nameDictionary.Add(watchBaseObject.Name, 1);
            else
            {
                watchBaseObject.Name =
                    string.Format("{0} ({1})", watchBaseObject.Name, nameDictionary[watchBaseObject.Name]);

                nameDictionary[watchBaseObject.NonUniqueName]++;
            }
        }
        #endregion

        #region Properties
        public static Dispatcher Dispatcher { get; set; }
        #endregion

        #region Fields
        private Dictionary<string, int> nameDictionary = new Dictionary<string, int>();
        #endregion
    }
}
