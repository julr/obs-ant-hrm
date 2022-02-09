using Bluegrams.Application;
using System;
using System.Diagnostics;
using System.IO;
using System.Timers;
using System.Windows;

namespace ObsHeartRateMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Ant.Dongle antDongle;
        private readonly Ant.Device.HeartRateMonitor heartRateMonitor;
        private readonly Timer logTimer;
        private int lastLogValue;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public MainWindow()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            PortableSettingsProvider.SettingsFileName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".config";
            PortableSettingsProvider.SettingsDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName);
            PortableSettingsProvider.ApplyProvider(Properties.Settings.Default);

            InitializeComponent();

            Application.Current.DispatcherUnhandledException += UnhandledException;

            int sensor = Properties.Settings.Default.Sensor;
            if (sensor != 0) TextBoxSensorId.Text = sensor.ToString();
            CheckBoxLogEnabled.IsChecked = Properties.Settings.Default.IsLogEnabled;
            ComboBoxLogRate.SelectedIndex = Properties.Settings.Default.LogRefreshRate;
            CheckBoxReconnect.IsChecked = Properties.Settings.Default.AutoReconnect;

            try
            {
                antDongle = new Ant.Dongle();
                antDongle.Initialize();

                heartRateMonitor = new Ant.Device.HeartRateMonitor(antDongle.Channels[0]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }

            logTimer = new Timer
            {
                AutoReset = true
            };
            logTimer.Elapsed += LogTimer_Elapsed;
        }

        private void UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("An unhandled exception occurred: " + e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }

        private void ButtonSearch_Click(object sender, RoutedEventArgs e)
        {
            var searchWindow = new SearchWindow(heartRateMonitor);
            searchWindow.ShowDialog();
        }

        private void ButtonConnectDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if ((string)ButtonConnectDisconnect.Content == "Connect")
            {
                Connect();
            }
            else
            {
                Disconnect();
            }
        }

        private void HeartRateMonitor_NewSensorDataReceived(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { TextBlockHeartRate.Text = heartRateMonitor.HeartRate.ToString(); }));
        }
        private void HeartRateMonitor_SensorNotFound(object? sender, EventArgs e)
        {
            MessageBox.Show("Sensor not found or disconnected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Dispatcher.BeginInvoke(new Action(() => { Disconnect(); }));
        }

        private void LogTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            int heartRate = heartRateMonitor.HeartRate;
            if ((heartRate > 0) && (lastLogValue != heartRate))
            {
                File.WriteAllText(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".txt", heartRate.ToString());
                lastLogValue = heartRate;
            }
        }

        private void Connect()
        {
            if (int.TryParse(TextBoxSensorId.Text, out int sensorId))
            {
                if ((sensorId >= 0) && (sensorId <= 65535))
                {
                    ButtonConnectDisconnect.Content = "Disconnect";
                    TextBoxSensorId.IsEnabled = false;
                    ButtonSearch.IsEnabled = false;
                    GroupBoxObs.IsEnabled = false;
                    CheckBoxReconnect.IsEnabled = false;
                    heartRateMonitor.NewSensorDataReceived += HeartRateMonitor_NewSensorDataReceived;

                    bool reconnect = CheckBoxReconnect.IsChecked ?? false;
                    if (!reconnect)
                        heartRateMonitor.SensorNotFound += HeartRateMonitor_SensorNotFound;
                    heartRateMonitor.Start((ushort)sensorId, reconnect);

                    if (sensorId > 0)
                    {
                        Properties.Settings.Default.Sensor = sensorId;
                    }
                    Properties.Settings.Default.IsLogEnabled = CheckBoxLogEnabled.IsChecked ?? false;
                    Properties.Settings.Default.LogRefreshRate = ComboBoxLogRate.SelectedIndex;
                    Properties.Settings.Default.AutoReconnect = reconnect;
                    Properties.Settings.Default.Save();

                    if (CheckBoxLogEnabled.IsChecked ?? false)
                    {
                        logTimer.Interval = (ComboBoxLogRate.SelectedIndex + 1) * 1000;
                        logTimer.Start();
                    }
                }
                else
                {
                    MessageBox.Show("Sensor ID must be between 0 (=Any) and 65535");
                }
            }
            else
            {
                MessageBox.Show("Sensor ID must be a number");
            }
        }

        private void Disconnect()
        {
            ButtonConnectDisconnect.Content = "Connect";
            TextBoxSensorId.IsEnabled = true;
            ButtonSearch.IsEnabled = true;
            GroupBoxObs.IsEnabled = true;
            CheckBoxReconnect.IsEnabled = true;
            heartRateMonitor.NewSensorDataReceived -= HeartRateMonitor_NewSensorDataReceived;
            heartRateMonitor.SensorNotFound -= HeartRateMonitor_SensorNotFound;
            heartRateMonitor.Stop();
            logTimer.Stop();
        }

    }
}
