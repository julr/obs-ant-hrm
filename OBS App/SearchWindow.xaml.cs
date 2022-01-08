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

namespace ObsHeartRateMonitor
{
    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow : Window
    {
        private readonly Ant.Device.HeartRateMonitor heartRateMonitor;
        public SearchWindow(Ant.Device.HeartRateMonitor heartRateMonitor)
        {
            InitializeComponent();
            this.Closing += SearchWindow_Closing;
            this.heartRateMonitor = heartRateMonitor;
            heartRateMonitor.SensorFound += HeartRateMonitor_SensorFound;
            heartRateMonitor.SensorNotFound += HeartRateMonitor_SensorNotFound;
            heartRateMonitor.StartSearch();
        }

        private void HeartRateMonitor_SensorNotFound(object? sender, EventArgs e)
        {
            MessageBox.Show("No sensor found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Dispatcher.BeginInvoke(new Action(() => { Close(); }));
        }

        private void HeartRateMonitor_SensorFound(object? sender, ushort e)
        {
            Dispatcher.BeginInvoke(new Action(() => { TextBoxSensor.Text = e.ToString(); }));
        }

        private void SearchWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            heartRateMonitor.Stop();
        }        
    }
}
