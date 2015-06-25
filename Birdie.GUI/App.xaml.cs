using Birdie.Watcher;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Birdie
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Methods
        protected override void OnStartup(StartupEventArgs e)
        {
            // Initialize the Birdie context
            birdieContext.Initialize(new Config { ListenPort = 11037, ChallengeKey = BitConverter.ToUInt64(Encoding.ASCII.GetBytes("11037Bir"), 0) });

            // Set the dispatcher
            WatchObjectContainer.Dispatcher = Dispatcher;

            this.MainWindow = new WatchWindow(birdieContext);
            this.MainWindow.Show();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Terminate Birdie
            birdieContext.Terminate();

            base.OnExit(e);
        }
        #endregion

        #region Fields
        private IBirdieContext birdieContext = BirdieContextFactory.CreateBirdieContext();
        #endregion
    }
}
