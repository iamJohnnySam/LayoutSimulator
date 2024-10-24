using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Communicator
{
    public class TCPClient
    {
        public delegate void MessageReceivedEventHandler(string message);
        public event MessageReceivedEventHandler OnMessageReceived;

        public event EventHandler<string>? OnMessageSend;
        public event EventHandler<bool>? OnConnectChangeEvent;


        public event EventHandler<string>? OnLogEvent;

        private string _serverIp;
        private int _serverPort;

        private TcpClient _client;
        private NetworkStream? _stream;
        private StreamReader? _reader;
        private StreamWriter? _writer;

        public bool isConnected;

        public TCPClient(string serverIp, int serverPort)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
            isConnected = false; // Initial state: not connected
        }

        public async Task StartClientAsync()
        {
            await Task.Run(async () =>
            {
                while (!isConnected)
                {
                    try
                    {
                        OnLogEvent?.Invoke(this, $"Attempting to connect to {_serverIp}:{_serverPort}");
                        _client = new TcpClient();
                        await _client.ConnectAsync(_serverIp, _serverPort); // Async connect
                        _stream = _client.GetStream();
                        _reader = new StreamReader(_stream, Encoding.UTF8);
                        _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
                        isConnected = true;

                        OnLogEvent?.Invoke(this, "Connected to server");
                        OnConnectChangeEvent?.Invoke(this, true);

                        await ReceiveDataAsync();
                    }
                    catch (SocketException)
                    {
                        OnLogEvent?.Invoke(this, "Unable to connect to server. Retrying...");
                        OnConnectChangeEvent?.Invoke(this, false);
                        await Task.Delay(5000); // Wait before retrying (non-blocking)
                    }
                }
            });
        }

        private async Task ReceiveDataAsync()
        {
            byte[] buffer = new byte[1024]; // Buffer to store received data
            try
            {
                while (isConnected)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length); // Read data asynchronously
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead); // Convert bytes to string
                        OnLogEvent?.Invoke(this, message);
                        OnMessageReceived?.Invoke(message); 
                    }
                    else
                    {
                        // No data received, this could mean the connection was closed by the server
                        OnLogEvent?.Invoke(this, "Connection closed by server.");
                        OnConnectChangeEvent?.Invoke(this, false);
                        isConnected = false; // Mark as disconnected
                    }
                }
            }
            catch (IOException ex)
            {
                if (isConnected)
                {
                    OnLogEvent?.Invoke(this, $"Connection lost due to error: {ex.Message}. Waiting to reconnect...");
                    OnConnectChangeEvent?.Invoke(this, false);
                    isConnected = false; // Mark as disconnected
                }
            }
        }

        public async Task SendDataAsync(string messageToSend)
        {
            try
            {
                if (isConnected && !string.IsNullOrEmpty(messageToSend))
                {
                    await _writer.WriteAsync(messageToSend); // Sends the message without any \r\n
                    await _writer.FlushAsync(); // Ensures the data is immediately sent
                    OnMessageSend?.Invoke(this, messageToSend);
                }
                else
                {
                    OnLogEvent?.Invoke(this, "Cannot send message. Either disconnected or message is empty.");
                    OnConnectChangeEvent?.Invoke(this, false);
                }
            }
            catch (IOException)
            {
                if (isConnected)
                {
                    OnLogEvent?.Invoke(this, "Unable to send message. Connection lost.");
                    OnConnectChangeEvent?.Invoke(this, false);
                    isConnected = false;
                }
            }
        }

        public void SendData(string messageToSend)
        {
            try
            {
                if (isConnected && !string.IsNullOrEmpty(messageToSend) && (_writer != null))
                {
                    _writer.WriteAsync(messageToSend); // Sends the message without any \r\n
                    _writer.FlushAsync(); // Ensures the data is immediately sent
                    OnLogEvent?.Invoke(this, $"Sent: {messageToSend}");
                }
                else
                {
                    OnLogEvent?.Invoke(this, "Cannot send message. Either disconnected or message is empty.");
                }
            }
            catch (IOException)
            {
                if (isConnected)
                {
                    OnLogEvent?.Invoke(this, "Unable to send message. Connection lost.");
                    OnConnectChangeEvent?.Invoke(this, false);
                    isConnected = false; // Mark as disconnected
                }
            }
        }

        public void CloseConnection()
        {
            if (isConnected)
            {
                OnLogEvent?.Invoke(this, "Closing connection...");
                isConnected = false;

                // Safely close the network stream and client
                _writer?.Close();
                _reader?.Close();
                _stream?.Close();
                _client?.Close();

                OnLogEvent?.Invoke(this, "Connection closed.");
            }
        }
    }
}
