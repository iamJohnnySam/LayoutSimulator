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
        public event EventHandler<string>? OnMessageReceived;

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

        public async Task StartClientAsync(bool startListening)
        {
            await Task.Run(async () =>
            {
                while (!isConnected)
                {
                    try
                    {
                        Console.WriteLine($"Attempting to connect to {_serverIp}:{_serverPort}");
                        _client = new TcpClient();
                        await _client.ConnectAsync(_serverIp, _serverPort); // Async connect
                        _stream = _client.GetStream();
                        _reader = new StreamReader(_stream, Encoding.UTF8);
                        _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
                        isConnected = true;

                        Console.WriteLine("Connected to server");

                        if (startListening)
                            await ReceiveDataAsync();
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine("Unable to connect to server. Retrying...");
                        await Task.Delay(5000); // Wait before retrying (non-blocking)
                    }
                }
            });
        }

        private async Task ReceiveDataAsync()
        {
            try
            {
                string message;
                while (isConnected && (message = await _reader.ReadLineAsync()) != null)
                {
                    OnMessageReceived?.Invoke(this, message);
                    Console.WriteLine($"Received: {message}");
                }
            }
            catch (IOException)
            {
                if (isConnected)
                {
                    Console.WriteLine("Connection lost. Waiting to reconnect...");
                    isConnected = false; // Mark as disconnected
                }
            }
        }

        public string ReceiveData()
        {
            try
            {
                if (_reader != null) {
                    string? message = _reader.ReadLine();
                    return message ?? "";
                }
            }
            catch (IOException)
            {
                if (isConnected)
                {
                    Console.WriteLine("Connection lost. Waiting to reconnect...");
                    isConnected = false;
                }
            }
            return "CONNECTION ERROR";
        }

        public async Task SendDataAsync(string messageToSend)
        {
            try
            {
                if (isConnected && !string.IsNullOrEmpty(messageToSend))
                {
                    await _writer.WriteLineAsync(messageToSend);
                    Console.WriteLine($"Sent: {messageToSend}");
                }
                else
                {
                    Console.WriteLine("Cannot send message. Either disconnected or message is empty.");
                }
            }
            catch (IOException)
            {
                if (isConnected)
                {
                    Console.WriteLine("Unable to send message. Connection lost.");
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
                    _writer.WriteLine(messageToSend);
                    Console.WriteLine($"Sent: {messageToSend}");
                }
                else
                {
                    Console.WriteLine("Cannot send message. Either disconnected or message is empty.");
                }
            }
            catch (IOException)
            {
                if (isConnected)
                {
                    Console.WriteLine("Unable to send message. Connection lost.");
                    isConnected = false; // Mark as disconnected
                }
            }
        }

        public void CloseConnection()
        {
            if (isConnected)
            {
                Console.WriteLine("Closing connection...");
                isConnected = false;

                // Safely close the network stream and client
                _writer?.Close();
                _reader?.Close();
                _stream?.Close();
                _client?.Close();

                Console.WriteLine("Connection closed.");
            }
        }
    }
}
