using Birdie.Process;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Birdie.Watcher
{
    /// <summary>
    /// Interaction logic for WatcherControl.xaml
    /// </summary>
    public partial class WatcherControl : UserControl
    {
        #region Methods
        public WatcherControl(ProcessData processData)
        {
            InitializeComponent();

            this.processData = processData;

            // Furnish the treeview
            watcherTreeView.DataContext = processData.RootWatchBaseObjects;

            // Set up the timer
            timer.Interval = 50.0;
            timer.Elapsed += timer_Elapsed;
            timer.Start();
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    foreach (WatchMemoryObject watchMemoryObject in monitoredObjects)
                        watchMemoryObject.ReadMemory();
                });
            }
            catch (TaskCanceledException)
            { }
        }
        #endregion

        #region Properties
        public List<WatchMemoryObject> MonitoredObjects { get { return monitoredObjects; } }
        #endregion

        #region Fields
        private ProcessData processData = null;
        private List<WatchMemoryObject> monitoredObjects = new List<WatchMemoryObject>();
        private Timer timer = new Timer();
        #endregion
    }
}
