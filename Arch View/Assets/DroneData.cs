using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

[System.Serializable]public class DroneData : MonoBehaviour
{
    [Header("Drone variables")]
    public double latitude = 0;
    public double longitude = 0;
    public double altitude = 0;
    public double velocity = 0;
    public double yaw = 0;
    public double pitch = 0;
    public double roll = 0;
    public double headYaw = 0;
    public double initialAltitude = 0;
    public int battery, GPSSignal, WIFISignal = 0;
    public Mapbox.Unity.Location.Location droneLocation;
    public byte[] CameraFeed;

    [Header("UI Objects")]
    public GameObject YawDirection;
    public GameObject TextBottom, GPS, Wifi, Battery, MainCamera, transformLocation, player;
    public MeshRenderer CameraPlane;

    private static class ImagingDrone  {

    }

    // Start is called before the first frame update
    void Start()
    {
        droneLocation.LatitudeLongitude.x = longitude;
        droneLocation.LatitudeLongitude.x = latitude;
        initialAltitude = 0;

        UpdateCamera();
    }

    // Update is called once per frame
    void Update()
    {
        // Correct initial altitude
        if (initialAltitude == -1 && altitude != 0)
            initialAltitude = altitude;

        UpdateCameraAttitude();
        UpdateYawSymbol();
        UpdateBatterySymbol();
        UpdateGPSSymbol();
        UpdateWiFiSymbol();
        UpdateLabel();
        UpdateCameraPosition();
    }


    /// <summary>
    /// Update blue camera gimbal symbol on UI
    /// </summary>
    private void UpdateYawSymbol()
    {
        var rect = YawDirection.GetComponent<RectTransform>();
        Vector3 vectorAngle = new Vector3();
        float newZ = Convert.ToSingle(headYaw) - rect.rotation.eulerAngles.z;
        vectorAngle.Set(0, 0, newZ);
        rect.Rotate(vectorAngle);
    }

    private void UpdateBatterySymbol()
    {
        UnityEngine.UI.Image[] batteryImage = Battery.GetComponentsInChildren<UnityEngine.UI.Image>();
        batteryImage[0].sprite = Resources.Load<Sprite>("Battery" + battery);
    }

    private void UpdateGPSSymbol()
    {
        UnityEngine.UI.Image[] gpsImage = GPS.GetComponentsInChildren<UnityEngine.UI.Image>();
        gpsImage[0].sprite = Resources.Load<Sprite>("GPS" + GPSSignal);
    }

    private void UpdateWiFiSymbol()
    {
        UnityEngine.UI.Image[] wifiImage = Wifi.GetComponentsInChildren<UnityEngine.UI.Image>();
        wifiImage[0].sprite = Resources.Load<Sprite>("WIFI" + WIFISignal);
    }

    private void UpdateCameraAttitude()
    {
        Vector3 vectorAngle = new Vector3();

        Camera camera = MainCamera.GetComponent<Camera>();
        float newX = -Convert.ToSingle(pitch) - camera.transform.rotation.eulerAngles.x;
        float newY = Convert.ToSingle(yaw) - camera.transform.rotation.eulerAngles.y;
        float newZ = -Convert.ToSingle(roll) - camera.transform.rotation.eulerAngles.z;
        vectorAngle.Set(newX, newY, newZ);
        camera.transform.Rotate(vectorAngle);
        camera.transform.Translate(new Vector3(0, (Convert.ToSingle(altitude) - camera.transform.position.y), 0));
        float newMY = Convert.ToSingle(yaw);

    }

    private void UpdateCameraAltitude()
    {
        float newYTranslate = Convert.ToSingle(altitude) - player.transform.position.y;
        player.transform.Translate(new Vector3(0, newYTranslate, 0));
    }

    private void UpdateLabel()
    {
        var flightHeight = altitude - initialAltitude;
        string text = $"Altitude: {altitude}m; Flight height: {flightHeight}m; Velocity: {velocity}m/s";
        Text textElement = TextBottom.GetComponent<Text>();
        textElement.text = text;
    }

    private void UpdateCameraPosition()
    {
        Mapbox.Unity.Map.AbstractMap abs = transformLocation.GetComponent<Mapbox.Unity.Map.AbstractMap>();
        var newY = longitude; //- abs.CenterLatitudeLongitude.x;
        var newX = latitude; //- abs.CenterLatitudeLongitude.y;
        //abs.SetCenterLatitudeLongitude(new Mapbox.Utils.Vector2d(newX, newY));
        abs.UpdateMap(new Mapbox.Utils.Vector2d(newX, newY), 16f);
    }

    private void UpdateCamera()
    {
        Debug.Log(WebCamTexture.devices[0].name);
        Debug.Log(WebCamTexture.devices[1].name);
        Debug.Log(WebCamTexture.devices[2].name);
        WebCamTexture camTexture = new WebCamTexture(WebCamTexture.devices[2].name);
        camTexture.Play();
        CameraPlane.material.mainTexture = camTexture;
    }

}
