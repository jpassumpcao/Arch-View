using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.WiFi;
using DJI.WindowsSDK;
using DJI.WindowsSDK.Components;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.UI.Core;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using Windows.ApplicationModel.Core;
using System.Windows.Input;
using Windows.ApplicationModel.Background;
using DJIVideoParser;
using Windows.UI;
using System.Globalization;
using Windows.Graphics.Imaging;
using Windows.Graphics.Display;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml.Hosting;
using Windows.UI.ViewManagement;

namespace DJI_ArchView_Client
{
    /// <summary>
    /// Uma página vazia que pode ser usada isoladamente ou navegada dentro de um Quadro.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string V = "Drone Camera";
        private TCPClient client;
        private WiFiManager WiFiManager;
        private GetDJIData DJI;
        public SwapChainPanel VideoSwapChainPanel;
        public ListBox DebugListBox;

        private StatusView statusView = new StatusView();

        //Início do sistema UWP
        public MainPage()
        {

            this.InitializeComponent();

            //Cria uma ListBox na qual é inserido todo processo de verificação e conexão do sistema
            DebugListBox = new ListBox();
            this.NavigationViewControl.SelectedItem = this.StatusView;

            //Cria uma janela que exibirá a imagem da câmera
            CreateCamWindowAsync();

            var cont = (StatusView)Content.Content;
            DebugListBox = cont.ListBox;

            //Cria um cliente TCP que se comunicará com a plataforma Unity
            client = new TCPClient(DebugListBox);

            //Inicia separadamente o WiFiManager resposável por se conectar com o drone
            Thread t = new Thread(() =>
            {
                Thread.Sleep(1000);
                startWiFi();
            });
            t.Start();

            var bounds = DisplayInformation.GetForCurrentView();

            // Get the screen resolution (APIs available from 14393 onward).
            ApplicationView.PreferredLaunchViewSize = new Size(bounds.ScreenWidthInRawPixels, bounds.ScreenHeightInRawPixels);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;


        }

        private async void CreateCamWindowAsync()
        {
            var bounds = DisplayInformation.GetForCurrentView();

            AppWindow appWindow = await AppWindow.TryCreateAsync();
            //appWindow.Title = V;
            Frame appWindowContentFrame = new Frame();
            appWindowContentFrame.Navigate(typeof(CameraFeeder));
            CameraFeeder page = (CameraFeeder)appWindowContentFrame.Content;
            ElementCompositionPreview.SetAppWindowContent(appWindow, appWindowContentFrame);
            appWindow.RequestSize(new Size(bounds.ScreenWidthInRawPixels, bounds.ScreenHeightInRawPixels));
            await appWindow.TryShowAsync();
            VideoSwapChainPanel = page.SwapChainPanel;

            appWindow.Closed += delegate
            {
                page = null;
                appWindow = null;
            };


            //CoreApplicationView newView = CoreApplication.CreateNewView();
            //int newViewId = 0;
            //await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            //{
            //    Frame frame = new Frame();
            //    frame.Navigate(typeof(CameraFeeder), "CameraFeeder");
            //    Window.Current.Content = frame;

            //    // You have to activate the window in order to show it later.
            //    Window.Current.Activate();

            //    newViewId = ApplicationView.GetForCurrentView().Id;
            //});
            //bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);

        }

        /// <summary>
        /// Call WiFiManager Class
        /// </summary>
        private async void startWiFi()
        {
            this.WiFiManager = new WiFiManager();

            _ = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                DebugListBox.Items.Add(this.WiFiManager.Status);
            });

            while (this.WiFiManager.WiFiAdapter == null)
                Thread.Sleep(50);

            _ = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                DebugListBox.Items.Add(this.WiFiManager.Status);
            });

            WiFiConnectionResult cr = await this.WiFiManager.ConnectWSSID();

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                DebugListBox.Items.Add(this.WiFiManager.Status);
            });

            while (cr == null)
                Thread.Sleep(50);

            if (cr.ConnectionStatus == WiFiConnectionStatus.Success)
            {
                Debug.WriteLine("Connected to drone");
                Task t = new Task(() =>
                {
                    _ = WifiMonitor();
                });
                t.Start();




                Thread dji = new Thread(async () =>
               {

                   GetDJIData DJIData = new GetDJIData(this);

                   int bl = 0;
                   int gl = 0;
                   try
                   {

                       while (DJIData != null)
                       {
                           await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                             {
                                 string Message = null;
                                 Message = "Streaming{";
                                 DJI = DJIData;
                                 latitude.Text = Math.Round(DJIData.GPS.Latitude, 4).ToString();
                                 Message += latitude.Text + ";";
                                 longitude.Text = Math.Round(DJIData.GPS.Longitude, 4).ToString();
                                 Message += longitude.Text + ";";
                                 altitude.Text = DJIData.Altitude.ToString();
                                 Message += altitude.Text + ";";
                                 velocity.Text = DJIData.LinearVelocity.ToString();
                                 Message += velocity.Text + ";";
                                 flymode.Text = DJIData.FlyMode;
                                 attitude.Text = DJIData.UAAttitude.Yaw.ToString() + "; " + DJIData.UAAttitude.Pitch.ToString() + "; " + DJIData.UAAttitude.Roll.ToString();
                                 Message += attitude.Text + ";";
                                 yaw.Text = DJIData.GimbalYaw.ToString();
                                 Message += yaw.Text + ";";
                                 cameraHandler = DJIData.cameraHandler;
                                 flightControllerHandler = DJIData.flightControllerHandler;
                                 if (bl != DJIData.BatteryLevel)
                                 {
                                     string imgString = "ms-appx:///Assets/Battery" + DJIData.BatteryLevel.ToString() + ".png";
                                     BitmapImage img = new BitmapImage(new Uri(imgString));
                                     this.BatteryLevelSymbol.Source = img;
                                     bl = DJIData.BatteryLevel;
                                 }
                                 Message += DJIData.BatteryLevel.ToString() + ";";
                                 if (gl != DJIData.GPSSignalLevel)
                                 {
                                     string imgString1 = "ms-appx:///Assets/GPS" + DJIData.GPSSignalLevel.ToString() + ".png";
                                     BitmapImage img1 = new BitmapImage(new Uri(imgString1));
                                     this.GPSSignalSymbol.Source = img1;
                                     gl = DJIData.GPSSignalLevel;
                                 }
                                 Message += DJIData.GPSSignalLevel + ";";
                                 Message += "3}";
                                 Thread t1 = new Thread(() => client.SendMessage(Message));
                                 t1.IsBackground = true;
                                 t1.Start();
                                 if (client.LastMessage != null)
                                 {
                                     if (client.LastMessage == "GoToHome")
                                         tsAutoLanding.IsOn = true;
                                     if (client.LastMessage == "TakeOffOrLanding")
                                         tsTakeOff.IsOn = true;
                                     client.LastMessage = null;
                                 }
                                 Thread.Sleep(33);
                             });
                       }

                   }
                   catch (Exception e)
                   {
                       Debug.WriteLine(e.Message);
                   }

               });

                dji.Start();


            }
            else
                Debug.WriteLine("Not connected to drone =(");
        }

        private DJIVideoParser.Parser videoParser;

        private async void Instance_SDKRegistrationStateChanged(SDKRegistrationState state, SDKError errorCode)
        {
            //try
            //{
            //    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            //    {
            //        //Raw data and decoded data listener
            //        if (videoParser == null)
            //        {
            //            videoParser = new DJIVideoParser.Parser();
            //            videoParser.Initialize(delegate (byte[] data)
            //            {
            //                //Note: This function must be called because we need DJI Windows SDK to help us to parse frame data.
            //                return DJISDKManager.Instance.VideoFeeder.ParseAssitantDecodingInfo(0, data);
            //            });

            //            await Task.Delay(2000);
            //            //Set the swapChainPanel to display and set the decoded data callback.
            //            videoParser.SetSurfaceAndVideoCallback(0, 1, VideoSwapChainPanel, ReceiveDecodedData);
            //            DJISDKManager.Instance.VideoFeeder.GetPrimaryVideoFeed(0).VideoDataUpdated += OnVideoPush;
            //            Debug.WriteLine("Data: " + videoParser.GetType().ToString());

            //        }


            //        //get the camera type and observe the CameraTypeChanged event.
            //        DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).CameraTypeChanged += GetDJIData_CameraTypeChanged;
            //        var type = await DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).GetCameraTypeAsync();
            //        GetDJIData_CameraTypeChanged(this, type.value);
            //    });
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine("Parse error: " + e.Message);
            //}
        }

        //public SwapChainPanel swapChainPanel;

        private void UninitializeVideoFeedModule()
        {
            if (DJISDKManager.Instance.SDKRegistrationResultCode == SDKError.NO_ERROR)
            {
                videoParser.SetSurfaceAndVideoCallback(0, 0, null, null);
                DJISDKManager.Instance.VideoFeeder.GetPrimaryVideoFeed(0).VideoDataUpdated -= OnVideoPushAsync;
            }
        }


        public async Task WifiMonitor()
        {
            byte? Signal = 0;

            while (Signal >= 0)
            {
                if (this.WiFiManager.WiFiAdapter != null)
                {
                    Windows.Networking.Connectivity.ConnectionProfile cp = await this.WiFiManager.WiFiAdapter.NetworkAdapter.GetConnectedProfileAsync();
                    _ = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if (cp != null)
                        {
                            if (cp.GetSignalBars() != Signal)
                            {
                                string imgString = "ms-appx:///Assets/WIFI" + cp.GetSignalBars().ToString() + ".png";
                                BitmapImage img = new BitmapImage(new Uri(imgString));
                                this.WifiSignalSymbol.Source = img;
                                Signal = cp.GetSignalBars();
                                this.WifiSignalSymbol.Opacity = 1;
                                if (this.client != null)
                                    client.SendMessage("WiFiSignal=" + cp.GetSignalBars());
                            }
                        }
                        else
                        {
                            if (Signal > 0)
                            {
                                string imgString = "ms-appx:///Assets/WIFI0.png";
                                BitmapImage img = new BitmapImage(new Uri(imgString));
                                this.WifiSignalSymbol.Source = img;
                                this.WifiSignalSymbol.Opacity = 0.75;
                                if (client != null)
                                    client.SendMessage("WiFiSignal=0");
                                Signal = 0;
                            }
                        }

                    });
                    Thread.Sleep(1000);
                }
            }

        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DJI.UninitializeVideoFeedModule();
        }

        private CameraHandler cameraHandler;

        private FlightControllerHandler flightControllerHandler;

        private RemoteControllerHandler remoteControllerHandler;


        private async void WorkMode_Click(object sender, RoutedEventArgs e)
        {
            if (DJI == null)
            {
                return;
            }

            var current = await cameraHandler.GetCameraWorkModeAsync();
            var currMode = current.value?.value;
            if (currMode != CameraWorkMode.RECORD_VIDEO)//.PLAYBACK && currMode != CameraWorkMode.TRANSCODE)
            {
                var msg = new CameraWorkModeMsg
                {
                    value = CameraWorkMode.RECORD_VIDEO
                };
                var retCode = await cameraHandler.SetCameraWorkModeAsync(msg);
                WorkMode.Background = new SolidColorBrush(retCode != 0 ? Color.FromArgb(128, 0, 255, 0) : Color.FromArgb(128, 255, 0, 0));
                DJI.videoParser.SetSurfaceAndVideoCallback(0, 0, VideoSwapChainPanel, DJI.ReceiveDecodedData);
            }
            else
            {
                var msg = new CameraWorkModeMsg
                {
                    value = CameraWorkMode.SHOOT_PHOTO
                };
                var retCode = await cameraHandler.SetCameraWorkModeAsync(msg);
                WorkMode.Background = new SolidColorBrush(retCode == 0 ? Color.FromArgb(128, 0, 255, 0) : Color.FromArgb(128, 255, 0, 0));
                //DJI.initializeVideoFeeder(swapChainPanel);
                DJI.videoParser.SetSurfaceAndVideoCallback(0, 0, VideoSwapChainPanel, DJI.ReceiveDecodedData);

            }



            current = await cameraHandler.GetCameraWorkModeAsync();
            currMode = current.value?.value;

            if (currMode == CameraWorkMode.SHOOT_PHOTO)
            {
                TakePicture.Visibility = Visibility.Visible;
                WorkMode.Content = "Photo Mode";
            }

            else
            {
                TakePicture.Visibility = Visibility.Collapsed;
                WorkMode.Content = "Video Mode";
            }
            DebugListBox.Items.Add(currMode.ToString());

        }


        public async void initializeVideoFeeder()
        {
            //Raw data and decoded data listener
            if (videoParser == null)
            {
                videoParser = new DJIVideoParser.Parser();
            }
            videoParser.Initialize(delegate (byte[] data)
            {
                //Note: This function must be called because we need DJI Windows SDK to help us to parse frame data.
                return DJISDKManager.Instance.VideoFeeder.ParseAssitantDecodingInfo(0, data);
            });

            //Set the swapChainPanel to display and set the decoded data callback.
            videoParser.SetSurfaceAndVideoCallback(0, 0, VideoSwapChainPanel, ReceiveDecodedData);
            DJISDKManager.Instance.VideoFeeder.GetPrimaryVideoFeed(0).VideoDataUpdated += OnVideoPushAsync;
            //get the camera type and observe the CameraTypeChanged event.
            DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).CameraTypeChanged += GetDJIData_CameraTypeChanged;
            var type = await DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).GetCameraTypeAsync();
            GetDJIData_CameraTypeChanged(this, type.value);

            //});
        }

        private void GetDJIData_CameraTypeChanged(object sender, CameraTypeMsg? value)
        {
            if (value != null)
            {
                switch (value.Value.value)
                {
                    case CameraType.MAVIC_2_ZOOM:
                        this.videoParser.SetCameraSensor(AircraftCameraType.Mavic2Zoom);
                        break;
                    case CameraType.MAVIC_2_PRO:
                        this.videoParser.SetCameraSensor(AircraftCameraType.Mavic2Pro);
                        break;
                    default:
                        this.videoParser.SetCameraSensor(AircraftCameraType.Others);
                        break;
                }

            }
        }

        //raw data
        void OnVideoPushAsync(VideoFeed sender, byte[] bytes)
        {
            try
            {
                videoParser.PushVideoData(0, 0, bytes, bytes.Length);

            }
            catch (Exception ex)
            {
                Debug.WriteLine("PushError: " + ex.Message);
            }
        }

        //Decode data. Do nothing here. This function would return a bytes array with image data in RGBA format.
        async void ReceiveDecodedData(byte[] data, int width, int height)
        {
        }

        private async void tsTakeoff_Toggled(object sender, RoutedEventArgs e)
        {

            if (DJI == null)
            {
                ContentDialog cd = new ContentDialog()
                {
                    Title = "Error",
                    Content = "No DJI Object founded.",
                    CloseButtonText = "OK"
                };
                ContentDialogResult result = await cd.ShowAsync();
                return;
            }

            if (tsTakeOff.IsOn)
            {
                var takeoff = await flightControllerHandler.StartTakeoffAsync();
                if (takeoff != SDKError.NO_ERROR)
                {
                    var motor = await flightControllerHandler.GetMotorStartFailureErrorAsync();
                    ContentDialog cd = new ContentDialog()
                    {
                        Title = "Error",
                        Content = motor.value.Value.value.ToString(),
                        CloseButtonText = "OK"
                    };
                    ContentDialogResult result = await cd.ShowAsync();
                    return;
                }
                tsTakeOff.IsEnabled = false;
                tsAutoLanding.IsEnabled = true;
                tsAutoLanding.IsOn = false;

                if (client != null)
                    client.SendMessage("TakeOffCompleted");

            }
            else
            {
                var takeoff = await flightControllerHandler.StopTakeoffAsync();
                //takeoff.Start();
                tsTakeOff.IsEnabled = true;
            }


        }

        private async void tsAutoLanding_Toggled(object sender, RoutedEventArgs e)
        {
            if (tsAutoLanding.IsOn)
            {
                var landing = await flightControllerHandler.StartAutoLandingAsync();
                if (landing != SDKError.NO_ERROR)
                {
                    ContentDialog cd = new ContentDialog()
                    {
                        Title = "Error",
                        Content = landing.ToString(),
                        CloseButtonText = "OK"
                    };
                    ContentDialogResult result = await cd.ShowAsync();
                    return;
                }
                tsTakeOff.IsEnabled = true;
                tsTakeOff.IsOn = false;
                tsAutoLanding.IsEnabled = false;

                if (client != null)
                    client.SendMessage("LandingCompleted");

            }
            else
            {
                var landing = await flightControllerHandler.StopAutoLandingAsync();
                tsAutoLanding.IsEnabled = true;
            }
        }

        private void StatusView_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            this.Content.Navigate(typeof(StatusView));
        }

        private void WaypointMap_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            this.Content.Navigate(typeof(WaypointMap));
            var cont = (WaypointMap)Content.Content;
            cont.Altitude = double.Parse(this.altitude.Text);
        }

        private void NavigationViewControl_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (((NavigationViewItem)args.SelectedItem).Tag == null)
                return;
            switch (((NavigationViewItem)args.SelectedItem).Tag.ToString())
            {
                case "waypoint":
                    Content.Navigate(typeof(WaypointMap));
                    var cont1 = (WaypointMap)Content.Content;
                    cont1.Altitude = double.Parse(this.altitude.Text);
                    break;
                default:
                    Content.Navigate(typeof(StatusView));
                    var cont = (StatusView)Content.Content;
                    DebugListBox = cont.ListBox;
                    break;
            }
        }

        private Dictionary<string, string> UnityFunctions = new Dictionary<string, string>();

        private async void btnCompass_Click(object sender, RoutedEventArgs e)
        {
            var calibration = await flightControllerHandler.StartCompasCalibrationAsync();
        }
    }
}
