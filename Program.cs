using System;

namespace VSCode
{
    class Program
    {
        static void Main(string[] args)
        {
            // Start the server  
            //TcpHelper.StartServer(5678);  
            //TcpHelper.Listen(); // Start listening.


           // AsynchronousSocketListener.StartListening(); 

           AsynchronousClient.StartClient();
            return;
        }
    }
}
