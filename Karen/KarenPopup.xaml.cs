using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using Karen.Interop;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Karen
{

    public partial class KarenPopup : UiWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Version => ((App)Application.Current).Distro.Version;
        public AppStatus DistroStatus => ((App)Application.Current).Distro.Status;
        public bool IsStarted => DistroStatus == AppStatus.Started;
        public bool IsStopped => DistroStatus == AppStatus.Stopped;
        public bool IsNotInstalled => DistroStatus == AppStatus.NotInstalled;

        public KarenPopup()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Watcher.Watch(this, BackgroundType.Mica);
            UpdateProperties();
        }

        private void Show_Config(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).ShowConfigWindow();

            Hide();
        }

        public void UpdateProperties()
        {
            PropertyChanged(this, new PropertyChangedEventArgs("DistroStatus"));
            PropertyChanged(this, new PropertyChangedEventArgs("IsStarted"));
            PropertyChanged(this, new PropertyChangedEventArgs("IsStopped"));
            PropertyChanged(this, new PropertyChangedEventArgs("Version"));
            // HideConsoleOnClose does not work on Arm64
            ShowConsole.Visibility = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void Show_Console(object sender, RoutedEventArgs e)
        {
            Kernel32.ShowConsole();
        }

        private void Shutdown_App(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Start_Distro(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).Distro.StartApp();
            UpdateProperties();
        }

        private void Stop_Distro(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).Distro.StopApp();
            UpdateProperties();
        }

        private void Open_Webclient(object sender, RoutedEventArgs e)
        {
            Process.Start("http://localhost:" + Settings.Default.NetworkPort);
        }

        private void Open_Distro(object sender, RoutedEventArgs e)
        {
            Process.Start(@"\\wsl$\lanraragi\home\koyomi\lanraragi");
        }

        private void Install_Distro(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).Distro.Repair();
        }

        private void Window_Deactivated(object sender, System.EventArgs e)
        {
            Hide();
        }
    }
}
