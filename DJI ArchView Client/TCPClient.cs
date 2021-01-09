using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Windows.UI.Xaml.Controls;
using Windows.UI.Core;
using System.Diagnostics;

namespace DJI_ArchView_Client
{

    sealed partial class TCPClient
    {
		
		private TcpClient socketConnection;
		private Thread clientReceiveThread;
		private string serverMessage;

		private TextBlock Text;
		private ListBox listBox;

		public TCPClient(ListBox list)
		{
			listBox = list;
			ConnectToTcpServer();
		}

		public string LastMessage;

		private void ConnectToTcpServer()
		{
			try
			{
				//clientReceiveThread = new Thread(new ThreadStart(ListenForData));
				clientReceiveThread = new Thread(() => ListenForData());
				clientReceiveThread.IsBackground = true;
				clientReceiveThread.Start();
				if (Text != null)
					listBox.Items.Add("Connected to TCP Server");
			}
			catch (Exception e)
			{
				Console.WriteLine("On client connect exception " + e);
			}
		}

		private void ListenForData()
		{
			try
			{
				socketConnection = new TcpClient("localhost", 55124);
				byte[] bytes = new byte[1024];
				while (true)
				{
					// Get a stream object for reading 				
					using (NetworkStream stream = socketConnection.GetStream())
					{
						int length;
						// Read incomming stream into byte arrary. 					
						while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
						{
							var incommingData = new byte[length];
							Array.Copy(bytes, 0, incommingData, 0, length);
							// Convert byte array to string message. 						
							serverMessage = Encoding.UTF8.GetString(incommingData);
							this.LastMessage = serverMessage;
							//_ = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
							//                            () =>
							//                            {
							//                                //listBox.Items.Add(serverMessage);
							//                            });
							Console.WriteLine("\r\nServer message received as: " + serverMessage);
						}
						//stream.Flush();
					}
				}
			}

			catch (SocketException socketException)
			{
				Console.WriteLine("Socket exception: " + socketException);
			}
			//this.Text.Text = serverMessage;
		}


		/// <summary> 	
		/// Send message to server using socket connection. 	
		/// </summary> 	
		public void SendMessage(string clientMessage)
		{
			if (socketConnection == null)
			{
				return;
			}
			try
			{
				// Get a stream object for writing. 			
				NetworkStream stream = socketConnection.GetStream();
				if (stream.CanWrite)
				{
					// Convert string message to byte array.  
					byte[] clientMessageAsByteArray = Encoding.UTF8.GetBytes(clientMessage);

					// Write byte array to socketConnection stream.    
					stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
				}
			}
			catch (SocketException socketException)
			{
				Console.WriteLine("Socket exception: " + socketException);
			}
		}
	}
}
