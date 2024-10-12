using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Communicator
{
    internal class Server
    {
        private string _serverIp;
        private int _serverPort;
        private Socket _socket;
        private CancellationTokenSource _cts;

        public event Action<string> OnMessageReceived;
        public event Action OnConnected;
        public event Action OnDisconnected;

        private bool _isConnected = false;
        private const int _reconnectDelay = 5000; // 5 seconds


        public void SocketClient(string serverIp, int serverPort)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
            _cts = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            await ConnectAsync();
            _ = ListenForMessagesAsync();
        }

        private async Task ConnectAsync()
        {
            while (!_isConnected)
            {
                try
                {
                    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    var ipAddress = IPAddress.Parse(_serverIp);
                    var remoteEndPoint = new IPEndPoint(ipAddress, _serverPort);

                    await _socket.ConnectAsync(remoteEndPoint);
                    _isConnected = true;

                    OnConnected?.Invoke();
                    Console.WriteLine("Connected to server.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Connection failed: {ex.Message}. Retrying in {_reconnectDelay / 1000} seconds...");
                    await Task.Delay(_reconnectDelay);
                }
            }
        }

        private async Task ListenForMessagesAsync()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                if (_socket != null && _socket.Connected)
                {
                    try
                    {
                        var buffer = new byte[1024];
                        int bytesRead = await _socket.ReceiveAsync(buffer, SocketFlags.None);

                        if (bytesRead > 0)
                        {
                            var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            OnMessageReceived?.Invoke(message);
                        }
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine("Connection lost.");
                        _isConnected = false;
                        OnDisconnected?.Invoke();
                        await HandleReconnectionAsync();
                    }
                }
                else
                {
                    await HandleReconnectionAsync();
                }
            }
        }

        private async Task HandleReconnectionAsync()
        {
            _socket.Close();
            _socket.Dispose();
            _isConnected = false;
            Console.WriteLine("Reconnecting...");
            await ConnectAsync();
        }

        public async Task SendMessageAsync(string message)
        {
            if (_isConnected && _socket.Connected)
            {
                var messageBytes = Encoding.UTF8.GetBytes(message);
                await _socket.SendAsync(messageBytes, SocketFlags.None);
            }
            else
            {
                Console.WriteLine("Cannot send message, not connected.");
            }
        }

        public void Stop()
        {
            _cts.Cancel();
            _socket.Close();
            _socket.Dispose();
            _isConnected = false;
        }
    }
}
