using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Net.NetworkInformation;
using System.Configuration;


namespace VSCode
{
    // State object for receiving data from remote device.  
    // public class StateObject {  
    //     // Client socket.  
    //     public Socket workSocket = null;  
    //     // Size of receive buffer.  
    //     public const int BufferSize = 256;  
    //     // Receive buffer.  
    //     public byte[] buffer = new byte[BufferSize];  
    //     // Received data string.  
    //     public StringBuilder sb = new StringBuilder();  
    // }  

    public class AsynchronousClient
    {
        // The port number for the remote device.  
        private const int port = 8080;

        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        // The response from the remote device.  
        private static String response = String.Empty;

        // internal static string GetLocalIPv4(NetworkInterfaceType _type)
        // {
        //     string output = "";
        //     foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
        //     {
        //         if (item.NetworkInterfaceType == _type && item.OperationalStatus == OperationalStatus.Up)
        //         {
        //             IPInterfaceProperties adapterProperties = item.GetIPProperties();

        //             if (adapterProperties.GatewayAddresses.FirstOrDefault() != null)
        //             {
        //                 foreach (UnicastIPAddressInformation ip in adapterProperties.UnicastAddresses)
        //                 {
        //                     if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        //                     {
        //                         output = ip.Address.ToString();
        //                     }
        //                 }
        //             }
        //         }
        //     }

        //     return output;
        // }

        public static void StartClient()
        {
            // Connect to a remote device.  
            try
            {


                // Establish the remote endpoint for the socket.  
                // The name of the   
                // remote device is "host.contoso.com".  
                // IPHostEntry ipHostInfo =  Dns.GetHostEntry("vigilant_meitner"); 
                //Dns.GetHostName()
                //Console.WriteLine(Dns.GetHostName());
                //string str= GetLocalIPv4(NetworkInterfaceType.Ethernet);
                //IPHostEntry ipHostInfo =  Dns.GetHostEntry("192.168.99.100");
                //IPHostEntry ipHostInfo =  Dns.GetHostEntry("172.17.0.2");
                //  Console.WriteLine(Dns.GetHostName("server"));
                //IPHostEntry ipHostInfo = Dns.GetHostEntry("35.202.7.16");
                var serverDNS = Environment.GetEnvironmentVariable("serverdns");
                Console.WriteLine(serverDNS);
                bool KeepBooking = true;
                int tradeCount = 0;
                while (KeepBooking)
                {
                    IPHostEntry ipHostInfo = Dns.GetHostEntry(serverDNS);

                    // IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName()); 
                    IPAddress ipAddress = ipHostInfo.AddressList[0];
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, 8090);



                    // Create a TCP/IP socket.  
                    Socket client = new Socket(ipAddress.AddressFamily,
                        SocketType.Stream, ProtocolType.Tcp);
                    Console.WriteLine(client.LocalEndPoint);
                    //client.ReceiveTimeout =10000;
                    //client.SendTimeout =10000;
                    // Connect to the remote endpoint.  
                    client.BeginConnect(remoteEP,
                        new AsyncCallback(ConnectCallback), client);
                    connectDone.WaitOne();



                    tradeCount++;
                    Console.WriteLine("Book Trade");

                    DateTime d = DateTime.Now.AddDays(2);
                    // Send test data to the remote device.
                    if ((tradeCount % 2) == 0)
                    {
                        Send(client, "InstrumentType:SPOT,amount:" + tradeCount * 1000 + ",TradeId:" + tradeCount + ",CCYPair:EURGBP,valuedate:" + d.ToString("dd/MM/yyyy") + ",Rate:1.5<EOF>");
                    }
                    else
                    {
                        d.AddDays(2);
                        Send(client, "InstrumentType:FWD,amount:" + tradeCount * 1000 + ",TradeId:" + tradeCount + ",CCYPair:USDGBP,valuedate:" + d.ToString("dd/MM/yyyy") + ",Rate:1.5<EOF>");
                    }
                    sendDone.WaitOne();

                    // Receive the response from the remote device.  

                    Receive(client);
                    receiveDone.WaitOne();
                  //Thread.Sleep(500);
                  // Write the response to the console.  
                 // tradeCount++;

                  // var key= Console.ReadKey(); 
                    // if(key.KeyChar=='N') break;
                 // Release the socket.  
                    // client.Shutdown(SocketShutdown.Send);  
                    //client.Close(); 

                    Console.WriteLine("press y to continue");
                    var key = Console.ReadKey();
                    if (key.Key != ConsoleKey.Y)
                    {
                        KeepBooking = false;
                    }


                }


            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.  
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Receive(Socket client)
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket   
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Get the rest of the data.  
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.  
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                        Console.WriteLine("Response received : {0}", response);
                    }
                    // Signal that all tes have been received.  
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // public static int Main(String[] args) {  
        //     StartClient();  
        //     return 0;  
        // }  
    }
}