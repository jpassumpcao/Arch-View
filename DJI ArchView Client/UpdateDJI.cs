using DJI.WindowsSDK.Components;

namespace DJI_ArchView_Client
{
    internal class UpdateDJI
    {
        private FlightControllerHandler ua;
        private BatteryHandler battery;
        private GimbalHandler gimbal;

        public UpdateDJI(FlightControllerHandler ua, BatteryHandler battery, GimbalHandler gimbal)
        {
            this.ua = ua;
            this.battery = battery;
            this.gimbal = gimbal;
        }
    }
}