using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DJI.WindowsSDK;
using DJI.WindowsSDK.Mission.Waypoint;
using Windows.Storage.Streams;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using DJIUWPSample.ViewModels;
using System.Threading;

// O modelo de item de Página em Branco está documentado em https://go.microsoft.com/fwlink/?LinkId=234238

namespace DJI_ArchView_Client
{

    /// <summary>
    /// Uma página vazia que pode ser usada isoladamente ou navegada dentro de um Quadro.
    /// </summary>
    public sealed partial class WaypointMap : Page
    {
        private MapIcon aircraftMapIcon = null;
        MapElementsLayer routeLayer = new MapElementsLayer();
        MapElementsLayer waypointLayer = new MapElementsLayer();
        MapElementsLayer locationLayer = new MapElementsLayer();

        public WaypointMap()
        {
            this.InitializeComponent();
            MyLandmarks = new List<MapElement>();
            WaypointLocations = new List<BasicGeoposition>();
            FlyZoneMap.Layers.Add(routeLayer);
            FlyZoneMap.Layers.Add(waypointLayer);
            FlyZoneMap.Layers.Add(locationLayer);

            DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0).StateChanged += WaypointMap_StateChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).AltitudeChanged += WaypointMap_AltitudeChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).AircraftLocationChanged += WaypointMap_AircraftLocationChanged;
            WaypointMissionState = DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0).GetCurrentState();
            DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0).ExecutionStateChanged += WaypointMap_ExecutionStateChanged;
        }


        private async void WaypointMap_ExecutionStateChanged(WaypointMissionHandler sender, WaypointMissionExecutionState? value)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (value.HasValue)
                {
                    lbMissionState.Text = "Mission state: "+value.Value.state.ToString();
                }
            });
        }

        private async void WaypointMap_AircraftLocationChanged(object sender, LocationCoordinate2D? value)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (value.HasValue)
                {
                    AircraftLocation = value.Value;
                }
            });
        }

        private async void WaypointMap_AltitudeChanged(object sender, DoubleMsg? value)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (value.HasValue)
                {
                    AircraftAltitude = value.Value.value;
                }
            });
        }

        private async void WaypointMap_StateChanged(DJI.WindowsSDK.Mission.Waypoint.WaypointMissionHandler sender, WaypointMissionStateTransition? value)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                WaypointMissionState = value.HasValue ? value.Value.current : WaypointMissionState.UNKNOWN;
            });
        }

        private async void WaypointMap_IsSimulatorStartedChanged(object sender, BoolMsg? value)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                IsSimulatorStart = value.HasValue && value.Value.value;
            });
        }

        public String SimulatorLatitude { set; get; }
        public String SimulatorLongitude { set; get; }
        public String SimulatorSatelliteCount { set; get; }
        bool _isSimulatorStart = false;
        public bool IsSimulatorStart
        {
            get
            {
                return _isSimulatorStart;
            }
            set
            {
                _isSimulatorStart = value;
                //OnPropertyChanged(nameof(IsSimulatorStart));
                //OnPropertyChanged(nameof(SimulatorState));
            }
        }
        public String SimulatorState
        {
            get
            {
                return _isSimulatorStart ? "Open" : "Close";
            }
        }
        private WaypointMissionState _waypointMissionState;
        public WaypointMissionState WaypointMissionState
        {
            get
            {
                return _waypointMissionState;
            }
            set
            {
                _waypointMissionState = value;
                //OnPropertyChanged(nameof(WaypointMissionState));
            }
        }

        private double _aircraftAltitude = 0;
        public double AircraftAltitude
        {
            get
            {
                return _aircraftAltitude;
            }
            set
            {
                _aircraftAltitude = value;
                //OnPropertyChanged(nameof(AircraftAltitude));
            }
        }

        private LocationCoordinate2D _aircraftLocation = new LocationCoordinate2D() { latitude = 0, longitude = 0 };
        public LocationCoordinate2D AircraftLocation
        {
            get
            {
                return _aircraftLocation;
            }
            set
            {
                _aircraftLocation = value;
                //OnPropertyChanged(nameof(AircraftLocation));
            }
        }

        private WaypointMission _waypointMission;
        public WaypointMission WaypointMission
        {
            get { return _waypointMission; }
            set
            {
                _waypointMission = value;
                //OnPropertyChanged(nameof(WaypointMission));
            }
        }



        private void AircraftLocationChange(LocationCoordinate2D value)
        {
            if (aircraftMapIcon == null)
            {
                aircraftMapIcon = new MapIcon()
                {
                    NormalizedAnchorPoint = new Point(0.5, 0.5),
                    ZIndex = 1,
                    Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/phantom.svg")),
                };
                locationLayer.MapElements.Add(aircraftMapIcon);
            }
            aircraftMapIcon.Location = new Geopoint(new BasicGeoposition() { Latitude = value.latitude, Longitude = value.longitude });
        }

        private void FlyZoneMap_MapTapped(Windows.UI.Xaml.Controls.Maps.MapControl sender, Windows.UI.Xaml.Controls.Maps.MapInputEventArgs args)
        {
            //to get a basicgeoposition of wherever the user clicks on the map
            BasicGeoposition basgeo_edit_position = args.Location.Position;

            //just checking to make sure it works
            Debug.WriteLine("tapped - lat: " + basgeo_edit_position.Latitude.ToString() + "  lon: " + basgeo_edit_position.Longitude.ToString());

            CreatePoint(basgeo_edit_position);
            
        }

        public List<BasicGeoposition> WaypointLocations;

        private List<MapElement> MyLandmarks;

        private void ClearPoints()
        {
            WaypointLocations.Clear();
            MyLandmarks.Clear();
            FlyZoneMap.Layers.Remove(LandmarksLayer);

        }

        private MapElementsLayer LandmarksLayer = new MapElementsLayer();
        private void CreatePoint(BasicGeoposition bg)
        {
            WaypointLocations.Add(bg);

            Geopoint snPoint = new Geopoint(bg);

            var spaceNeedleIcon = new MapIcon
            {
                Location = snPoint,
                NormalizedAnchorPoint = new Point(0.5,0.5),
                ZIndex = 1,
                Title = WaypointLocations.Count.ToString()
            };

            MyLandmarks.Add(spaceNeedleIcon);

            LandmarksLayer = new MapElementsLayer
            {
                ZIndex = 1,
                MapElements = MyLandmarks
            };

            FlyZoneMap.Layers.Add(LandmarksLayer);

            //FlyZoneMap.Center = snPoint;
            //FlyZoneMap.ZoomLevel = 14;

            if (!btnStartMission.IsEnabled)
                btnStartMission.IsEnabled = true;
            //if (!btnStartSimulation.IsEnabled)
            //    btnStartSimulation.IsEnabled = true;
        }

        private async void btnLoadCoord_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.FileTypeFilter.Add(".txt");

            int counter = 0;
            string line;
            BasicGeoposition bg = new BasicGeoposition();

            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                if (WaypointLocations.Count >=1)
                    ClearPoints();

                // Application now has read/write access to the picked file
                Stream st = await file.OpenStreamForReadAsync();
                StreamReader fileStream = new System.IO.StreamReader(st, System.Text.Encoding.UTF8);
                while ((line = fileStream.ReadLine()) != null)
                {
                    string[] coord = line.Split(';');
                    double lat = double.Parse(coord[0]);
                    double lon = double.Parse(coord[1]);
                    bg = new BasicGeoposition();
                    bg.Latitude = lat;
                    bg.Longitude = lon;
                    CreatePoint(bg);
                    counter++;
                }
            }

            FlyZoneMap.Center = new Geopoint(bg);
            FlyZoneMap.ZoomLevel = 18;

        }

        private async void btnSaveCoord_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });
            savePicker.SuggestedFileName = "New Coordinate Document";

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                IEnumerable<string> lines = new string[] { };
                
                foreach (BasicGeoposition gp in WaypointLocations)
                {
                    string lat = gp.Latitude.ToString();
                    string lon = gp.Longitude.ToString();
                    lines = lines.Concat(new[] { lat+";"+lon });
                }
                // write to file
                await FileIO.WriteLinesAsync(file, lines, Windows.Storage.Streams.UnicodeEncoding.Utf8);

                Windows.Storage.Provider.FileUpdateStatus status = await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);

            }
        }

        public double Altitude;

        private async void btnStartMission_Click(object sender, RoutedEventArgs e)
        {
            var v = new BoolMsg();
            v.value = true;
            var setStation = await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).SetGroundStationModeEnabledAsync(v);


            // Verify flight values

            if (txHeight.Text.Length < 1 || txMaxSpeed.Text.Length < 1 || double.Parse(txHeight.Text) < 3 || double.Parse(txHeight.Text) > 30 || double.Parse(txMaxSpeed.Text) < 2 || double.Parse(txMaxSpeed.Text) > 15)
            {
                ContentDialog IncorrectValues = new ContentDialog
                {
                    Title = "Incorrect Values",
                    Content = "Flight Height must be between 3-30 m \r\nMax Flight Speed must be between 2-15 m/s",
                    CloseButtonText = "Ok"
                };

                ContentDialogResult result = await IncorrectValues.ShowAsync();
                return;
            }

            var WayPoint = DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0);
            var currState = WayPoint.GetCurrentState();
            lbWaypointState.Text = "Waypoint state: " + currState.ToString();
            ContentDialog currStateWindow = new ContentDialog
            {
                Title = "Waypoint State",
                Content = "State: "+currState.ToString(),
                CloseButtonText = "Ok"
            };

            ContentDialogResult result1 = await currStateWindow.ShowAsync();

            // Create mission waypoints
            int i = 0;
            List<Waypoint> MissionWayPoints = new List<Waypoint>();
            foreach (BasicGeoposition c in WaypointLocations)
            {
                if (i>0)
                {
                    Waypoint wp = new Waypoint()
                    {
                        location = new LocationCoordinate2D()
                        {
                            latitude = c.Latitude,
                            longitude = c.Longitude
                        },
                        altitude = (double.Parse(txHeight.Text) + this.Altitude),
                        gimbalPitch = -30,
                        turnMode = WaypointTurnMode.CLOCKWISE,
                        heading = 0,
                        actionRepeatTimes = 1,
                        actionTimeoutInSeconds = 60,
                        cornerRadiusInMeters = 0.2,
                        speed = 0,
                        shootPhotoTimeInterval = -1,
                        shootPhotoDistanceInterval = -1,
                        waypointActions = new List<WaypointAction>()

                    };
                    MissionWayPoints.Add(wp);
                }
                i++;
            }

            var mission = new WaypointMission() {
                waypointCount = 0,
                maxFlightSpeed = double.Parse(txMaxSpeed.Text),
                autoFlightSpeed = 5,
                finishedAction = WaypointMissionFinishedAction.GO_HOME,
                headingMode = WaypointMissionHeadingMode.TOWARD_POINT_OF_INTEREST,
                flightPathMode = WaypointMissionFlightPathMode.NORMAL,
                gotoFirstWaypointMode = WaypointMissionGotoFirstWaypointMode.SAFELY,
                exitMissionOnRCSignalLostEnabled = false,
                pointOfInterest = new LocationCoordinate2D()
                {
                    latitude = WaypointLocations.FirstOrDefault().Latitude,
                    longitude = WaypointLocations.FirstOrDefault().Longitude
                },
                gimbalPitchRotationEnabled = true,
                repeatTimes = 0,
                missionID = 0,
                waypoints = MissionWayPoints
            };

            currState = WayPoint.GetCurrentState();

            if (currState == WaypointMissionState.READY_TO_UPLOAD || currState == WaypointMissionState.READY_TO_EXECUTE)
            {
                var sdk = DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0).LoadMission(mission); // Load mission
                if (sdk != SDKError.NO_ERROR)
                {
                    currStateWindow.Content = sdk;
                    result1 = await currStateWindow.ShowAsync();
                    return;
                }
            }
            else
            {
                return;
            }

            //upload into aircraft
            try
            {
                SDKError err = await DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0).UploadMission(); // Upload mission
                if (err != SDKError.NO_ERROR)
                {
                    String dialogMsg = "Failed to upload => " + err.ToString();
                    Debug.WriteLine(dialogMsg);
                    return;
                }
                else
                {
                    Debug.WriteLine("Success to Upload");
                    DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0).UploadStateChanged += WaypointMap_UploadStateChanged1;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            
            if (currState == WaypointMissionState.READY_TO_EXECUTE)
            {
                currStateWindow.Content = "Success! ";
                result1 = await currStateWindow.ShowAsync();
                //execute mission
                //var missionState = await DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0).StartMission();
                //WaypointMissionState state = DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0).GetCurrentState();
                ////WaypointMap_StateChanged(DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0), missionState. );
            }
        }

        private void WaypointMap_UploadStateChanged1(WaypointMissionHandler sender, WaypointMissionUploadState? value)
        {
            lbMissionState.Text = "Mission: " + value.GetValueOrDefault().ToString();
        }
    }
}
