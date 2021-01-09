using DJI.WindowsSDK;
using DJI.WindowsSDK.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using DJIVideoParser;
using Windows.UI.Xaml.Controls;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.ApplicationModel.UserDataTasks.DataProvider;

namespace DJI_ArchView_Client
{
    class GetDJIData
    {

        public class Location
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }

        public Location GPS { get; private set; } = new Location();

        public string FlyMode { get; set; } = " ";

        public double LinearVelocity { get; private set; }

        public double? Altitude { get; private set; } = 0;

        public class Attitude
        {
            public double Yaw { get; set; }
            public double Pitch { get; set; }
            public double Roll { get; set; }
        }

        public Attitude UAAttitude { get; private set; } = new Attitude();

        public double GimbalYaw { get; private set; } = 0;

        public int GPSSignalLevel { get; private set; } = 0;

        public int BatteryLevel { get; private set; }

        public MainPage MainPage;

        public GetDJIData(MainPage mp)//FlightControllerHandler ua, BatteryHandler battery, GimbalHandler gimbal)
        {
            this.MainPage = mp;

            DJISDKManager.Instance.SDKRegistrationStateChanged += Instance_SDKRegistrationEvent;

            //Replace with your registered App Key. Make sure your App Key matched your application's package name on DJI developer center.
            DJISDKManager.Instance.RegisterApp("2dbaa750f517e61f408702e2");
        }

        public DJIVideoParser.Parser videoParser;


        private async void Instance_SDKRegistrationEvent(SDKRegistrationState state, SDKError resultCode)
        {

            if (resultCode == SDKError.NO_ERROR)
            {
                cameraHandler = DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0);
                flightControllerHandler = DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0);
                remoteControllerHandler = DJISDKManager.Instance.ComponentManager.GetRemoteControllerHandler(0, 0);
                var current = await cameraHandler.GetCameraWorkModeAsync();
                var currMode = current.value?.value;
                if (currMode != CameraWorkMode.PLAYBACK && currMode != CameraWorkMode.TRANSCODE)
                {
                    var msg = new CameraWorkModeMsg
                    {
                        value = CameraWorkMode.TRANSCODE
                    };
                    var retCode = await cameraHandler.SetCameraWorkModeAsync(msg);
                }
                else
                {
                    var msg = new CameraWorkModeMsg
                    {
                        value = CameraWorkMode.SHOOT_PHOTO
                    };
                    var retCode = await cameraHandler.SetCameraWorkModeAsync(msg);
                }
                await Task.Delay(2000);

                System.Diagnostics.Debug.WriteLine("Register app successfully.");

                await cameraHandler.SetCameraWorkModeAsync(new CameraWorkModeMsg { value = CameraWorkMode.SHOOT_PHOTO });

                await Task.Delay(2000);

                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    //Carrega os dados do drone

                    //Battery
                    DJISDKManager.Instance.ComponentManager.GetBatteryHandler(0, 0).ChargeRemainingInPercentChanged += GetDJIData_FullChargeCapacityChanged;
                    var percent = await DJISDKManager.Instance.ComponentManager.GetBatteryHandler(0, 0).GetChargeRemainingInPercentAsync();
                    GetDJIData_FullChargeCapacityChanged(this, percent.value);

                    //Attitude
                    DJISDKManager.Instance.ComponentManager.GetGimbalHandler(0, 0).GimbalAttitudeChanged += GetDJIData_GimbalAttitudeChanged;
                    var att = await DJISDKManager.Instance.ComponentManager.GetGimbalHandler(0, 0).GetGimbalAttitudeAsync();
                    GetDJIData_GimbalAttitudeChanged(this, att.value);

                    //HeadYaw (giro da câmera em relação à frente do drone)
                    DJISDKManager.Instance.ComponentManager.GetGimbalHandler(0, 0).YawRelativeToBodyHeadingChanged += GetDJIData_YawRelativeToBodyHeadingChanged;
                    var yaw = await DJISDKManager.Instance.ComponentManager.GetGimbalHandler(0, 0).GetYawRelativeToBodyHeadingAsync();
                    GetDJIData_YawRelativeToBodyHeadingChanged(this, yaw.value);

                    //Location
                    DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).AircraftLocationChanged += GetDJIData_AircraftLocationChanged;
                    var loc = await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).GetAircraftLocationAsync();
                    GetDJIData_AircraftLocationChanged(this, loc.value);

                    //GPS Signal
                    DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).GPSSignalLevelChanged += GetDJIData_GPSSignalLevelChanged;
                    var gpss = await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).GetGPSSignalLevelAsync();
                    GetDJIData_GPSSignalLevelChanged(this, gpss.value);

                    //Flight Mode
                    DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).FlightModeChanged += GetDJIData_FlightModeChanged;
                    var mode = await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).GetFlightModeAsync();
                    GetDJIData_FlightModeChanged(this, mode.value);

                    //Altitude
                    DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).AltitudeChanged += GetDJIData_AltitudeChanged;
                    var alt = await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).GetAltitudeAsync();
                    GetDJIData_AltitudeChanged(this, alt.value);

                    //Velocity
                    DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).VelocityChanged += GetDJIData_VelocityChanged;
                    var vel = await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).GetVelocityAsync();
                    GetDJIData_VelocityChanged(this, vel.value);

                    //Calibra o magnetômetro
                    await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).StartCompasCalibrationAsync();

                    //Desativa o modo Novice que impediria decolar com o drone
                    var v = new BoolMsg();
                    v.value = false;
                    var noviceMode = await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).SetNoviceModeEnabledAsync(v);


                });

                //Inicializa o feed da câmera na janela criada a partir da MainPage
                initializeVideoFeeder(MainPage.VideoSwapChainPanel);
            }
            //Somente caso não ocorra o registro da API
            else
            {
                System.Diagnostics.Debug.WriteLine("Register SDK failed, the error is: ");
                System.Diagnostics.Debug.WriteLine(resultCode.ToString());
            }
        }

        public async void initializeVideoFeeder(SwapChainPanel swapChainPanel)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {

                //Raw data and decoded data listener
                if (videoParser == null)
                {
                    videoParser = new DJIVideoParser.Parser();
                }
                await Task.Delay(500);
                videoParser.Initialize(delegate (byte[] data)
                {
                    //Note: This function must be called because we need DJI Windows SDK to help us to parse frame data.
                    return DJISDKManager.Instance.VideoFeeder.ParseAssitantDecodingInfo(0, data);
                });

                await Task.Delay(500);
                //Set the swapChainPanel to display and set the decoded data callback.
                videoParser.SetSurfaceAndVideoCallback(0, 0, swapChainPanel, ReceiveDecodedData);
                DJISDKManager.Instance.VideoFeeder.GetPrimaryVideoFeed(0).VideoDataUpdated += OnVideoPushAsync;
                await Task.Delay(500);
                //get the camera type and observe the CameraTypeChanged event.
                DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).CameraTypeChanged += GetDJIData_CameraTypeChanged;
                var type = await DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).GetCameraTypeAsync();
                await Task.Delay(500);
                GetDJIData_CameraTypeChanged(this, type.value);

            });
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

        public byte[] VideoFeedBytes;

        //raw data
        void OnVideoPushAsync(VideoFeed sender, byte[] bytes)
        {
            try
            {
                videoParser.PushVideoData(0, 0, bytes, bytes.Length);
                VideoFeedBytes = bytes;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("PushError: " + ex.Message);
            }
        }

        public enum Command
        {
            Start, Stop
        }

        public async Task<DJI.WindowsSDK.SDKError> TakeOffAsync(Command cmd)
        {
            Task<SDKError> tsk = null;
                if (cmd == Command.Start)
                    tsk = DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).StartTakeoffAsync();
                else
                    tsk = DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).StopTakeoffAsync();
            
            return await tsk;
        }

        public async Task<DJI.WindowsSDK.SDKError> AutoLandinAsync(Command cmd)
        {
            Task<SDKError> tsk = null;
                if (cmd == Command.Start)
                    tsk = DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).StartAutoLandingAsync();
                else
                    tsk = DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).StopAutoLandingAsync();
            return await tsk;
        }


        //Decode data. Do nothing here. This function would return a bytes array with image data in RGBA format.
        public async void ReceiveDecodedData(byte[] data, int width, int height)
        {
        }

        private void GetDJIData_GPSSignalLevelChanged(object sender, FCGPSSignalLevelMsg? value)
        {
            if (value.HasValue)
            {
                string gpssignal = value.Value.value.ToString();
                char signal = gpssignal[gpssignal.Length - 1];
                this.GPSSignalLevel = int.Parse(signal.ToString());
                System.Diagnostics.Debug.WriteLine(value.Value.value.ToString());
            }
        }

        private void GetDJIData_VelocityChanged(object sender, Velocity3D? value)
        {
            if (value.HasValue)
            {
                this.LinearVelocity = CalculateLinearVelocity(value.Value.x, value.Value.y, value.Value.z);
            }
        }

        private void GetDJIData_AltitudeChanged(object sender, DoubleMsg? value)
        {
            if (value.HasValue)
            {
                this.Altitude = value.Value.value;
            }
        }

        private void GetDJIData_FlightModeChanged(object sender, FCFlightModeMsg? value)
        {
            if (value.HasValue)
            {
                this.FlyMode = value.Value.value.ToString();
            }
        }

        private void GetDJIData_AircraftLocationChanged(object sender, LocationCoordinate2D? value)
        {
            if (value.HasValue)
            {
                this.GPS.Latitude = value.Value.latitude;
                this.GPS.Longitude = value.Value.longitude;
            }
        }

        private void GetDJIData_YawRelativeToBodyHeadingChanged(object sender, DoubleMsg? value)
        {
            if (value.HasValue)
            {
                this.GimbalYaw = value.Value.value;
            }
        }

        private void GetDJIData_GimbalAttitudeChanged(object sender, DJI.WindowsSDK.Attitude? value)
        {
            if (value.HasValue)
            {
                this.UAAttitude.Yaw = value.Value.yaw;
                this.UAAttitude.Pitch = value.Value.pitch;
                this.UAAttitude.Roll = value.Value.roll;
            }
        }

        private void GetDJIData_FullChargeCapacityChanged(object sender, IntMsg? value)
        {
            if (value.HasValue)
            {
                this.BatteryLevel = GetBatteryLevel(value.Value.value);
            }
        }


        /// <summary>
        /// Convert battery remaining percentage into level
        /// </summary>
        /// <param name="im">Percentage integer [0-100]</param>
        /// <returns>Level integer value between 0 and 4</returns>
        public int GetBatteryLevel (int im)
        {
            int bl = 0;
            switch(im)
            {
                case int i when i <= 20:
                    bl = 0;
                    break;
                case int i when i <= 40:
                    bl = 1;
                    break;
                case int i when i <= 60:
                    bl = 2;
                    break;
                case int i when i <= 80:
                    bl = 3;
                    break;
                case int i when i <= 100:
                    bl = 4;
                    break;
            }
            return bl;
        }

        /// <summary>
        /// Calculate linear velocity from 3 axis
        /// </summary>
        /// <param name="x">Velocity in x</param>
        /// <param name="y">Velocity in y</param>
        /// <param name="z">Velocity in z</param>
        /// <returns>Linear velocity in double</returns>
        public double CalculateLinearVelocity(double x, double y, double z)
        {
            double v;
            v = Math.Sqrt(Math.Abs(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2)));
            return v;
        }

        public CameraHandler cameraHandler;
        public FlightControllerHandler flightControllerHandler;
        public RemoteControllerHandler remoteControllerHandler;

        /// <summary>
        /// Developer should change the work mode to transcode or playback in order to enhance download speed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void WorkMode(object sender, RoutedEventArgs e)
        {
            var current = await cameraHandler.GetCameraWorkModeAsync();
            var currMode = current.value?.value;
            if (currMode != CameraWorkMode.PLAYBACK && currMode != CameraWorkMode.TRANSCODE)
            {
                var msg = new CameraWorkModeMsg
                {
                    value = CameraWorkMode.TRANSCODE
                };
                var retCode = await cameraHandler.SetCameraWorkModeAsync(msg);
                Button btn = new Button();
                btn.Background = new SolidColorBrush(retCode == 0 ? Color.FromArgb(128, 0, 255, 0) : Color.FromArgb(128, 255, 0, 0));
            }
            else
            {
                var msg = new CameraWorkModeMsg
                {
                    value = CameraWorkMode.SHOOT_PHOTO
                };
                var retCode = await cameraHandler.SetCameraWorkModeAsync(msg);
                //ModeBtn.Background = new SolidColorBrush(retCode != 0 ? Color.FromArgb(128, 0, 255, 0) : Color.FromArgb(128, 255, 0, 0));
            }

        }

        public async void UninitializeVideoFeedModule()
        {
            if (DJISDKManager.Instance.SDKRegistrationResultCode == SDKError.NO_ERROR)
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    videoParser.SetSurfaceAndVideoCallback(0, 0, null, null);
                    DJISDKManager.Instance.VideoFeeder.GetPrimaryVideoFeed(0).VideoDataUpdated -= OnVideoPushAsync;
                });
            }
        }


    }
}
