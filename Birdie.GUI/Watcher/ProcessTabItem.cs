using Birdie.Process;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Birdie.Watcher
{
    public class ProcessTabItem : TabItem
    {
        #region Methods
        public ProcessTabItem(ProcessData processData)
            : base()
        {
            ProcessData = processData;
            WatcherControl = new WatcherControl(ProcessData);
            this.Content = WatcherControl;
            this.InvalidateVisual();
        }
        #endregion

        #region Properties
        public ProcessData ProcessData { get; set; }
        public WatcherControl WatcherControl { get; private set; }
        #endregion 
    }
}
