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
            timer.Interval = 0.25;
            timer.Elapsed += timer_Elapsed;
            timer.Start();
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();

            try
            {
                Dispatcher.Invoke(() =>
                {
                    // Sliding loop
                    if (monitoredObjects.Count > 0)
                    {
                        for (int i = 0; i < 50; i++)
                        {
                            if (currentIndex >= monitoredObjects.Count)
                                currentIndex = 0;

                            monitoredObjects.ElementAt(currentIndex).ReadMemory();
                            currentIndex += 1;
                        }
                    }
                });
            }
            catch (TaskCanceledException)
            { }

            timer.Start();
        }
        #endregion

        #region Properties
        public List<WatchMemoryObject> MonitoredObjects { get { return monitoredObjects; } }
        #endregion

        #region Fields
        private ProcessData processData = null;
        private List<WatchMemoryObject> monitoredObjects = new List<WatchMemoryObject>();
        private Timer timer = new Timer();
        private int currentIndex = 0;
        #endregion
    }
}
