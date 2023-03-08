#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.System;

namespace Karen.Interop
{

    public class LRReader : IDisposable
    {

        private AppServiceConnection? Connection;

        private AppDiagnosticInfoWatcher Watcher;

        private SemaphoreSlim Semaphore;

        public LRReader()
        {
            /*var proc = ProcessDiagnosticInfo.GetForProcesses();
            var process = proc.FirstOrDefault(pdi => pdi.IsPackaged && pdi.GetAppDiagnosticInfos().Any(adi => IsPackageFamilyName(adi.AppInfo.PackageFamilyName)));
            if (process != null)
                InitializeConnection(process.GetAppDiagnosticInfos()[0].AppInfo.PackageFamilyName);*/
            Semaphore = new SemaphoreSlim(1);
            Watcher = AppDiagnosticInfo.CreateWatcher();
            Watcher.Added += Watcher_Added;
            Watcher.Removed += Watcher_Removed;
            Watcher.Start();
        }

        private async Task<bool> InitializeConnection(string packageFamilyName)
        {
            Semaphore.Wait();
            try
            {
                if (Connection != null)
                    return false;
                Connection = new AppServiceConnection
                {
                    AppServiceName = "KarenMonitor",
                    PackageFamilyName = packageFamilyName
                };
                Connection.RequestReceived += Connection_RequestReceived;
                Connection.ServiceClosed += Connection_ServiceClosed;
                var status = await Connection.OpenAsync();
                if (status != AppServiceConnectionStatus.Success)
                {
                    DisposeConnection();
                    return false;
                }
                return true;
            }
            finally
            {
                Semaphore.Release();
            }
        }

        private async void Watcher_Added(AppDiagnosticInfoWatcher sender, AppDiagnosticInfoWatcherEventArgs args)
        {
            if (Connection == null && IsPackageFamilyName(args.AppDiagnosticInfo.AppInfo.PackageFamilyName))
                await InitializeConnection(args.AppDiagnosticInfo.AppInfo.PackageFamilyName);
        }

        private void Watcher_Removed(AppDiagnosticInfoWatcher sender, AppDiagnosticInfoWatcherEventArgs args)
        {
            if (Connection != null && IsPackageFamilyName(args.AppDiagnosticInfo.AppInfo.PackageFamilyName))
                DisposeConnection();
        }

        private void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            DisposeConnection();
        }

        private async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var def = args.GetDeferral();
            var msg = args.Request.Message;
            var type = (PacketType)msg["PacketType"];
            switch (type)
            {
                case PacketType.None:
                    break;
                case PacketType.InstanceStart:
                    ((App)Application.Current).Distro.StartApp();
                    var set = new ValueSet
                            {
                                { "PacketType", (int)type },
                            };
                    await args.Request.SendResponseAsync(set);
                    break;
                case PacketType.InstanceStop:
                    ((App)Application.Current).Distro.StopApp();
                    set = new ValueSet
                            {
                                { "PacketType", (int)type },
                            };
                    await args.Request.SendResponseAsync(set);
                    break;
                case PacketType.InstanceStatus:
                    break;
                case PacketType.InstanceSetting:
                    var settingOperation = (SettingOperation)msg["PacketSettingOperation"];
                    var settingType = (SettingType)msg["PacketSettingType"];
                    switch (settingOperation)
                    {
                        case SettingOperation.None:
                            break;
                        case SettingOperation.Load:
                            set = new ValueSet
                            {
                                { "PacketType", (int)type },
                                { "PacketSettingOperation", (int)settingOperation },
                                { "PacketSettingType", (int)settingType },
                                { "PacketValue", typeof(Settings).GetProperty(settingType.ToString()).GetValue(Settings.Default) }
                            };
                            await args.Request.SendResponseAsync(set);
                            break;
                        case SettingOperation.Save:
                            var prop = typeof(Settings).GetProperty(settingType.ToString());
                            prop.SetValue(Settings.Default, msg["PacketValue"]);
                            set = new ValueSet
                            {
                                { "PacketType", (int)type },
                                { "PacketSettingOperation", (int)settingOperation },
                                { "PacketSettingType", (int)settingType },
                                { "PacketValue", typeof(Settings).GetProperty(settingType.ToString()).GetValue(Settings.Default) }
                            };
                            await args.Request.SendResponseAsync(set);
                            break;
                    }
                    break;
                case PacketType.InstanceRepair:
                    ((App)Application.Current).Distro.Repair();
                    break;
            }
            def.Complete();
        }

        private void DisposeConnection()
        {
            Semaphore.Wait();
            try
            {
                if (Connection == null)
                    return;
                Connection.RequestReceived -= Connection_RequestReceived;
                Connection.ServiceClosed -= Connection_ServiceClosed;
                Connection.Dispose();
                Connection = null;
            }
            finally
            {
                Semaphore.Release();
            }
        }

        public void Dispose()
        {
            Watcher.Stop();
            DisposeConnection();
            Semaphore.Dispose();
        }

        private bool IsPackageFamilyName(string packageFamilyName)
        {
            return packageFamilyName == "63705Guerra24.LRReader_pd6jswmanqqw0" || packageFamilyName == "Guerra24.LRReader_3fr0p4qst6948";
        }
    }

    public enum PacketType
    {
        None, InstanceStart, InstanceStop, InstanceStatus, InstanceSetting, InstanceRepair
    }

    public enum SettingOperation
    {
        None, Load, Save
    }
    public enum SettingType
    {
        None, ContentFolder, ThumbnailFolder, StartServerAutomatically, StartWithWindows, NetworkPort,
        ForceDebugMode, UseWSL2
    }

}
