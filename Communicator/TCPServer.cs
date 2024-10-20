using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Communicator
{
    public class TCPServer
    {
        public event EventHandler<string>? OnMessageReceived;

        private readonly IPAddress ipAddress;
        private readonly int port;
        private TcpListener? server;
        private TcpClient? client;
        private NetworkStream? stream;
        private Thread? listenerThread;
        private volatile bool isRunning;

        public TCPServer(string ipAddress, int port)
        {
            this.ipAddress = IPAddress.Parse(ipAddress);
            this.port = port;
        }
        public void Start()
        {
            isRunning = true;
            listenerThread = new Thread(new ThreadStart(ListenForClients));
            listenerThread.Start();
        }

        // Stop the server
        public void Stop()
        {
            isRunning = false;
            client?.Close();
            server?.Stop();
        }

        // Listen for incoming clients
        private void ListenForClients()
        {
            server = new TcpListener(ipAddress, port);
            try { server.Start(); }
            catch (System.Net.Sockets.SocketException) { }

            while (isRunning)
            {
                try
                {
                    Console.WriteLine($"Waiting for a client connection on port {port}...");
                    client = server.AcceptTcpClient();
                    Console.WriteLine($"{port} - Client connected!");

                    // Handle client connection
                    stream = client.GetStream();

                    // Start listening for messages from the client
                    while (isRunning && client.Connected)
                    {
                        try
                        {
                            byte[] buffer = new byte[1024];
                            int bytesRead = stream.Read(buffer, 0, buffer.Length);
                            if (bytesRead > 0)
                            {
                                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                                OnMessageReceived?.Invoke(this, message);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{port} - Connection lost: " + ex.Message);
                            Reconnect();
                        }
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"{port} - Socket exception: " + ex.Message);
                    Reconnect();
                }
            }
        }

        // Send message to the connected client
        public void SendMessage(string message)
        {
            if (client != null && client.Connected)
            {
                byte[] buffer = Encoding.ASCII.GetBytes(message);
                if (stream  != null)
                    stream.Write(buffer, 0, buffer.Length);
            }
        }

        // Reconnect logic if connection drops
        private void Reconnect()
        {
            client?.Close();
            Console.WriteLine($"{port} - Reconnecting...");
            ListenForClients();
        }
    }
}
