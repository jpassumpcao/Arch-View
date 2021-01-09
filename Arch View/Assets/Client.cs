using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class Client : MonoBehaviour
{
    #region private members 	
    /// <summary> 	
    /// TCPListener to listen for incomming TCP connection 	
    /// requests. 	
    /// </summary> 	
    private TcpListener tcpListener;
    /// <summary> 
    /// Background thread for TcpServer workload. 	
    /// </summary> 	
    private Thread tcpListenerThread;
    /// <summary> 	
    /// Create handle to connected tcp client. 	
    /// </summary> 	
    private TcpClient connectedTcpClient;
    #endregion

    // Use this for initialization
    void Start()
    {
        // Start TcpServer background thread 		
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequests));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    SendMessage();
        //}
    }

    /// <summary> 	
    /// Runs in background TcpServerThread; Handles incomming TcpClient requests 	
    /// </summary> 	
    private void ListenForIncommingRequests()
    {
        try
        {
            // Create listener on localhost port 8052. 			
            tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 55124);
            tcpListener.Start();
            Debug.Log("Server is listening");
            Byte[] bytes = new Byte[1024];
            while (true)
            {
                Debug.Log("Waiting for a connection...");
                connectedTcpClient = tcpListener.AcceptTcpClient();
                Debug.Log("Connected!");
                Thread t = new Thread(new ParameterizedThreadStart(HandleDeivce));
                t.Start(connectedTcpClient);

                //using (connectedTcpClient = tcpListener.AcceptTcpClient())
                //{
                //    // Get a stream object for reading 					
                //    using (NetworkStream stream = connectedTcpClient.GetStream())
                //    {
                //        int length;
                //        // Read incomming stream into byte arrary. 						
                //        while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                //        {
                //            var incommingData = new byte[length];
                //            Array.Copy(bytes, 0, incommingData, 0, length);
                //            // Convert byte array to string message. 							
                //            string clientMessage = Encoding.UTF8.GetString(incommingData);
                //            Debug.Log("client message received as: " + clientMessage);
                //            GetStreaming(clientMessage);
                //        }
                //    }
                //}
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }

    public void HandleDeivce(System.Object obj)
    {
        TcpClient client = (TcpClient)obj;
        var stream = client.GetStream();
        string imei = String.Empty;

        string data = null;
        Byte[] bytes = new Byte[1024]; //Acceptable byte number
        int length;
        try
        {
            while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
            {
                string hex = BitConverter.ToString(bytes);
                data = Encoding.UTF8.GetString(bytes, 0, length);
                Console.WriteLine("{1}: Received: {0}", data, Thread.CurrentThread.ManagedThreadId);

                string str = "Hey Device!";
                byte[] reply = System.Text.Encoding.UTF8.GetBytes(str);
                stream.Write(reply, 0, reply.Length);
                Console.WriteLine("{1}: Sent: {0}", str, Thread.CurrentThread.ManagedThreadId);

                //var incommingData = new byte[length];
                //Array.Copy(bytes, 0, incommingData, 0, length);
                //// Convert byte array to string message. 							
                //string clientMessage = Encoding.UTF8.GetString(incommingData);
                //Debug.Log("client message received as: " + clientMessage);
                Thread t = new Thread(() => GetStreaming(data));
                t.IsBackground = true;
                t.Start();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: {0}", e.ToString());
            client.Close();
        }
    }


    /// <summary> 	
    /// Send message to client using socket connection. 	
    /// </summary> 	
    public new void SendMessage(string str)
    {
        if (connectedTcpClient == null)
        {
            return;
        }

        try
        {
            // Get a stream object for writing. 			
            NetworkStream stream = connectedTcpClient.GetStream();
            if (stream.CanWrite)
            {
                // Convert string message to byte array.                 
                byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(str);
                // Write byte array to socketConnection stream.               
                stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
                Debug.Log("Server sent his message - should be received by client");
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }

    public DroneData droneData;

    /// <summary>
    /// Send updated drone data to DroneData class
    /// </summary>
    /// <param name="message"></param>
    private void GetStreaming(string data)
    {
        data = data.Remove(0, 10); // Remove string 'Streaming{'
        List<string> list;
        list = data.Split(';').ToList();
        Debug.Log("Streaming = " + data);
        droneData.latitude = double.Parse(list[0]);  //-22.987086;
        droneData.longitude = double.Parse(list[1]); // -46.954450; Colocar depois!!!!!!
        droneData.altitude = double.Parse(list[2]);
        droneData.velocity = double.Parse(list[3]);
        droneData.yaw = double.Parse(list[4]);
        droneData.pitch = double.Parse(list[5]);
        droneData.roll = double.Parse(list[6]);
        droneData.headYaw = double.Parse(list[7]);
        droneData.battery = int.Parse(list[8]);
        droneData.GPSSignal = int.Parse(list[9]);
        droneData.WIFISignal = int.Parse(list[10].Substring(0,1));
    }
}