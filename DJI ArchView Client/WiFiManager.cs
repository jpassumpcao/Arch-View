using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFi;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace DJI_ArchView_Client
{
    class WiFiManager
    {
        public string Status { get; private set; }

        public string DJIConnectionStatus { get; private set; } = "Disconnected";

        public WiFiManager()
        {
            _ = ScanForNetworks();
            /*Thread t = new Thread(new ThreadStart(async () => await WifiMonitor()));
            t.IsBackground = true;
            t.Start();
            System.Diagnostics.Debug.WriteLine("Thread state: " + t.ThreadState.ToString());*/
        }

        public WiFiAdapter WiFiAdapter { get; private set; }
        public Collection<WiFiSignal> ResultCollection { get; private set; }

        protected async Task Initialize()
        {
            var access = await WiFiAdapter.RequestAccessAsync();
            if (access != WiFiAccessStatus.Allowed)
            {
                
                Status = "WiFiAccessStatus not allowed";
                System.Diagnostics.Debug.WriteLine(Status);
            }
            else
            {
                var wifiAdapterResults =
                  await DeviceInformation.FindAllAsync(WiFiAdapter.GetDeviceSelector());
                if (wifiAdapterResults?.Count >= 1)
                {
                    int i = 0;
                    foreach (var device in wifiAdapterResults)
                    {
                        if (device.Id.Contains("PCI#"))
                        {
                            Status = "Added WiFiAdapter: " + wifiAdapterResults[i].Id;
                            System.Diagnostics.Debug.WriteLine(Status);
                            this.WiFiAdapter = await WiFiAdapter.FromIdAsync(wifiAdapterResults[i].Id);
                        }

                        i++;
                    }
                }
                else
                {
                    Status = "WiFi Adapter not found.";
                    System.Diagnostics.Debug.WriteLine(Status);
                }
            }
        }

        private async Task ScanForNetworks()
        {
            this.ResultCollection = new Collection<WiFiSignal>();
            Status = "Starting WiFiAdapter...";
            System.Diagnostics.Debug.WriteLine(Status);
            await Initialize();
            while (this.WiFiAdapter == null)
            {
                //System.Diagnostics.Debug.WriteLine("null");
                Thread.Sleep(50);
            }
            Status = "WiFi initialized!\r\n Scanning available networks...";
            System.Diagnostics.Debug.WriteLine(Status);
            await this.WiFiAdapter.ScanAsync();
            WiFiSignal ws = null;
            if (WiFiAdapter != null)
            {
                //Create list of available SSIDs
                var report = WiFiAdapter.NetworkReport;
                foreach (var availableNetwork in report.AvailableNetworks)
                {
                    ws = new WiFiSignal()
                    {
                        MacAddress = availableNetwork.Bssid,
                        Ssid = availableNetwork.Ssid,
                        SignalBars = availableNetwork.SignalBars,
                        ChannelCenterFrequencyInKilohertz =
                        availableNetwork.ChannelCenterFrequencyInKilohertz,
                        NetworkKind = availableNetwork.NetworkKind.ToString(),
                        PhysicalKind = availableNetwork.PhyKind.ToString()
                    };
                    ResultCollection.Add(ws);
                }
            }
            else
            {
                Status = "Adapter null";
                System.Diagnostics.Debug.WriteLine(Status);
            }
        }

        public class WiFiSignal
        {
            public string MacAddress, Ssid, NetworkKind, PhysicalKind;
            public byte SignalBars;
            public int ChannelCenterFrequencyInKilohertz;
        }

        public async Task<WiFiSignal> ConnectedWifiSignal()
        {
            WiFiSignal ws = new WiFiSignal();
            Windows.Networking.Connectivity.ConnectionProfile cp = await this.WiFiAdapter.NetworkAdapter.GetConnectedProfileAsync();
            if (cp != null)
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ws.Ssid = cp.ProfileName;
                    //"Adapter access: " + cp.GetNetworkConnectivityLevel().ToString();
                    ws.SignalBars = byte.Parse(cp.GetSignalBars().ToString());
                });
            }

            return ws;

        }

        public async Task<WiFiConnectionResult> ConnectWSSID()
        {
            this.Status = "Cannot connect to DJI adapter";
            WiFiConnectionResult cr = null;
            while (this.WiFiAdapter == null)
                Thread.Sleep(50);
            this.WiFiAdapter.Disconnect();
            var profile = await this.WiFiAdapter.NetworkAdapter.GetConnectedProfileAsync();
            while (profile != null)
            {
                profile = await this.WiFiAdapter.NetworkAdapter.GetConnectedProfileAsync();
            }
            foreach (WiFiAvailableNetwork wan in this.WiFiAdapter.NetworkReport.AvailableNetworks)
            {
                if (wan.Ssid.Contains("MAVIC"))
                {
                    cr = await this.WiFiAdapter.ConnectAsync(wan, WiFiReconnectionKind.Automatic);
                    if (cr != null && cr.ConnectionStatus == WiFiConnectionStatus.Success)
                    {
                        this.Status = "Connected to DJI Mavic";
                        this.DJIConnectionStatus = "Connected";
                    }

                }
            }
            return cr;
        }

    }
}
