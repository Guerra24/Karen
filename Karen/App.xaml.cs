using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using HideConsoleOnCloseManaged;
using Karen.Interop;
using Windows.ApplicationModel;
using Windows.UI.Popups;
using Screen = System.Windows.Forms.Screen;

namespace Karen
{
    /// <summary>
    /// Simple application. Check the XAML for comments.
    /// </summary>
    public partial class App : Application
    {
        private TaskbarIcon notifyIcon;
        public WslDistro Distro { get; set; }

        private KarenPopup Popup;

        private LRReader LRReader;

        public void ToastNotification(string text)
        {
            notifyIcon.ShowBalloonTip("LANraragi", text, notifyIcon.Icon, true);
        }

        public void ShowConfigWindow()
        {
            var mainWindow = Application.Current.MainWindow;

            if (mainWindow == null || mainWindow.GetType() != typeof(MainWindow))
                mainWindow = new MainWindow();

            mainWindow.Show();

            if (mainWindow.WindowState == WindowState.Minimized)
                mainWindow.WindowState = WindowState.Normal;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Only one instance of the bootloader allowed at a time
            var exists = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1;
            if (exists)
            {
                ShowMessageDialog("Another instance of the application is already running.", "Close");
                Current.Shutdown();
            }

            Settings.Default.MigrateUserConfigToMSIX();

            Distro = new WslDistro();

            // If the currently installed version is more recent than the one saved in settings, run the installer to update the distro
            // This is only required in MSIX mode/Package Identity, as the MSI installer updates the distro automatically.
            if (new DesktopBridge.Helpers().IsRunningAsUwp())
            {
                bool needsUpgrade = Version.TryParse(Settings.Default.Version, out var oldVersion) && oldVersion < GetVersion();
                if (!Distro.CheckDistro() || needsUpgrade)
                {
                    Settings.Default.Karen = true;
                    Package.Current.GetAppListEntriesAsync().GetAwaiter().GetResult()
                        .First(app => app.AppUserModelId == Package.Current.Id.FamilyName + "!Installer").LaunchAsync().GetAwaiter().GetResult();
                    Current.Shutdown();
                    return;
                }
            }

            // First time ?
            if (Settings.Default.FirstLaunch)
            {
                ShowConfigWindow();
                ShowMessageDialog("Looks like this is your first time running the app! Please setup your Content Folder in the Settings.", "Ok");
            }

            // Create the Taskbar Icon now so it appears in the tray
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");

            LRReader = new LRReader();

            // Initialize console as early as possible
            Kernel32.AllocConsole();
            Kernel32.HideConsole();
            if (RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
                HideConsoleOnClose.Enable();

            // Check if server starts with app 
            if (Settings.Default.StartServerAutomatically && Distro.Status == AppStatus.Stopped)
            {
                ToastNotification("LANraragi is starting automagically...");
                Distro.StartApp();
            }
            else
                ToastNotification("The Launcher is now running! Please click the icon in your Taskbar.");
            Popup = new KarenPopup();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LRReader.Dispose();
            if (notifyIcon != null)
                notifyIcon.Dispose(); //the icon would clean up automatically, but this is cleaner
            Kernel32.FreeConsole();
            try
            {
                Distro.StopApp();
            }
            finally
            {
                base.OnExit(e);
            }
        }

        private static Version GetVersion()
        {
            var version = new DesktopBridge.Helpers().IsRunningAsUwp() ? Package.Current.Id.Version : new PackageVersion();
            return new Version(version.Major, version.Minor, version.Build, version.Revision);
        }

        public static void ShowMessageDialog(string content, string button, IntPtr window = new IntPtr())
        {
            if (Current.MainWindow == null)
            {
                var msg = new MessageDialog(content, "LANraragi");
                msg.Commands.Add(new UICommand(button));
                if (window == IntPtr.Zero)
                    window = User32.GetDesktopWindow();
                ((IInitializeWithWindow)(object)msg).Initialize(window);
                msg.ShowAsync().GetAwaiter().GetResult();
            }
            else
            {
                var box = new Wpf.Ui.Controls.MessageBox();
                box.Title = "LANraragi";
                box.MicaEnabled = true;
                box.Height = 160;
                box.Content = new TextBlock
                {
                    Text = content,
                    TextWrapping = TextWrapping.WrapWithOverflow
                };
                box.ButtonLeftName = button;
                box.ButtonLeftClick += (_, _) => box.Close();
                box.ButtonRightClick += (_, _) => box.Close();
                box.Show();
            }
        }

        private void TaskbarIcon_PopupOpened(object sender, RoutedEventArgs e)
        {
            if (Popup == null)
                return;
            PositionWindowOnScreen(Popup);
            Popup.Show();
            Popup.UpdateProperties();
            Popup.Activate();
        }

        public static void PositionWindowOnScreen(Window window)
        {
            Screen activeScreen = Screen.FromPoint(System.Windows.Forms.Control.MousePosition);
            double dpi = activeScreen.WorkingArea.Width / SystemParameters.PrimaryScreenWidth;

            double xPositionToSet = System.Windows.Forms.Control.MousePosition.X - window.Width * dpi / 2;
            double yPositionToSet = System.Windows.Forms.Control.MousePosition.Y - 32 * dpi;

            double distanceToEdgeX = xPositionToSet + window.Width * dpi - activeScreen.WorkingArea.Width + activeScreen.WorkingArea.X;
            double distanceToEdgeY = yPositionToSet + window.Height * dpi - activeScreen.WorkingArea.Height + activeScreen.WorkingArea.Y;

            if (distanceToEdgeX > 0)
            {
                xPositionToSet -= distanceToEdgeX;
            }

            if (distanceToEdgeY > 0)
            {
                yPositionToSet -= window.Height * dpi;
            }

            window.Left = Math.Max(xPositionToSet / dpi, 0);
            window.Top = Math.Max(yPositionToSet / dpi, 0);
        }
    }
}
