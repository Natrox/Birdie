using Birdie.Process;
using Birdie.Watcher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Birdie
{
    /// <summary>
    /// Interaction logic for WatchWindow.xaml
    /// </summary>
    public partial class WatchWindow : Window
    {
        #region Methods
        public WatchWindow(IBirdieContext birdieContext)
        {
            InitializeComponent();

            BirdieContext = birdieContext;

            // Register some event handling
            birdieContext.ProcessConnect += BirdieProcessConnect;
            birdieContext.ProcessDisconnect += BirdieProcessDisconnect;
            birdieContext.WatchCategoryObjectAdd += BirdieWatchObjectAdd;
            birdieContext.WatchCategoryObjectRemove += BirdieWatchObjectRemove;
            birdieContext.WatchMemoryObjectAdd += BirdieWatchObjectAdd;
            birdieContext.WatchMemoryObjectRemove += BirdieWatchObjectRemove;

            // Set up auto expand
            TreeListView.TreeListViewItem.AutoExpand = true;
        }

        void BirdieWatchObjectAdd(WatchBaseObject watchObject)
        {
            Dispatcher.Invoke(() =>
            {
                ProcessTabItem processTabItem = processDataToTabDictionary[watchObject.ProcessData];

                if (watchObject.GetType() == typeof(WatchMemoryObject))
                    processTabItem.WatcherControl.MonitoredObjects.Add((WatchMemoryObject)watchObject);
            });
        }

        void BirdieWatchObjectRemove(WatchBaseObject watchObject)
        {
            Dispatcher.Invoke(() =>
            {
                ProcessTabItem processTabItem = processDataToTabDictionary[watchObject.ProcessData];

                if (watchObject.GetType() == typeof(WatchMemoryObject))
                    processTabItem.WatcherControl.MonitoredObjects.Remove((WatchMemoryObject)watchObject);
            });
        }

        #region Birdie Events
        void BirdieProcessConnect(ProcessData processData)
        {
            Dispatcher.Invoke(() =>
            {
                ProcessTabItem processTabItem = new ProcessTabItem(processData)
                {
                    Header = String.Format("{0} ({1})", processData.ProcessName, processData.ProcessId)
                };

                processDataToTabDictionary.Add(processData, processTabItem);
                processTabControl.Items.Add(processTabItem);

                if (processTabControl.Items.Count == 1)
                    processTabControl.SelectedIndex = 0;
            });
        }

        void BirdieProcessDisconnect(ProcessData processData)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    if (processDataToTabDictionary.ContainsKey(processData))
                        processTabControl.Items.Remove(processDataToTabDictionary[processData]);
                });
            }
            catch (TaskCanceledException)
            { }
        }
        #endregion

        #endregion

        #region Properties
        public IBirdieContext BirdieContext { get; private set; }
        #endregion

        #region Fields
        private Dictionary<ProcessData, ProcessTabItem> processDataToTabDictionary = new Dictionary<ProcessData, ProcessTabItem>();
        #endregion
    }
}
