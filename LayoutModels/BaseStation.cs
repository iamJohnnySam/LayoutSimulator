using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Communicator;
using Google.Protobuf.WellKnownTypes;
using LayoutCommands;
using LayoutModels.CommSpecs;
using LayoutModels.Support;

namespace LayoutModels
{
    public class BaseStation
    {
        public event EventHandler<LogMessage>? OnLogEvent;

        public string StationID { get; set; } = "ST";
        public List<string> Locations { get; set; } = [];
        public bool Busy { get; set; } = false;
        public bool EnablePassThrough { get; set; } = false;
        public Translator? Translator { get; set; }

        private int IndependentTransactionID { get; set; } = 1;
        private Dictionary<string, Dictionary<ResponseType, string>> RecievedResponses { get; set; } = new();


        TCPClient? PassThroughClient;

        Stopwatch stopwatch = new Stopwatch();


        public void ProcessWait(float SecsTime)
        {
            if (!EnablePassThrough || (PassThroughClient != null && !PassThroughClient.isConnected))
               Thread.Sleep((int)(SecsTime * 1000));
        }

        public void EnablePassthrough(string ipAddress, int port, CommandStructure comS, ResponseStructure resS, ICommSpec commSpec)
        {
            EnablePassThrough = true;
            PassThroughClient = new TCPClient(ipAddress, port);

            Translator = new Translator(comS, resS, commSpec);

            _ = PassThroughClient.StartClientAsync();

            PassThroughClient.OnMessageReceived += PassThroughClient_OnMessageReceived1; ;
        }

        private void PassThroughClient_OnMessageReceived1(string recievedMessage)
        {
            Console.WriteLine("Here");
            if (Translator != null)
            {
                (string transactionID, ResponseType responseType, string reply) = Translator.TranslateResponseToMessage(recievedMessage);

                RecievedResponses[transactionID] = new Dictionary<ResponseType, string> { { responseType, reply } };
            }
        }

        public void DiablePassthrough()
        {
            EnablePassThrough = false;
            PassThroughClient = null;
        }

        public string PassThroughCommand(Job job)
        {
            if (Translator != null && PassThroughClient != null && PassThroughClient.isConnected)
            {
                Job tempJob = new Job(job);
                tempJob.TransactionID = IndependentTransactionID++.ToString("D3");

                if (IndependentTransactionID > 999)
                    IndependentTransactionID = 1;

                PassThroughClient.SendData(Translator.TranslateCommandToString(tempJob));

                stopwatch.Start();
                TimeSpan timeout = TimeSpan.FromMinutes(1);

                while (true)
                {
                    if (RecievedResponses.TryGetValue(tempJob.TransactionID, out Dictionary<ResponseType, string>? value))
                    {
                        string? reply;
                        if (value.TryGetValue(ResponseType.Success, out reply))
                        {
                            RecievedResponses.Remove(tempJob.TransactionID);
                            return reply;
                        }
                        else if (value.TryGetValue(ResponseType.Nack, out reply))
                        {
                            RecievedResponses.Remove(tempJob.TransactionID);
                            throw new NackResponse(NackCodes.ModuleNack, reply);
                        }
                        else if (value.TryGetValue(ResponseType.Error, out reply))
                        {
                            RecievedResponses.Remove(tempJob.TransactionID);
                            throw new ErrorResponse(ErrorCodes.ModuleError, reply);
                        }
                        else
                        {
                            value.Remove(ResponseType.Ack);
                        }
                    }
                    if (stopwatch.Elapsed >= timeout)
                    {
                        OnLogEvent?.Invoke(this, new LogMessage($"External {StationID} Timedout"));
                        break;
                    }
                    Thread.Sleep(100);
                }
                throw new ErrorResponse(ErrorCodes.TimedOut, "Waiting for command for 1 minute");
            }
            else
            {
                return string.Empty;
            }
            // return (ResponseType.Nack, string.Empty);
        }
    }
}
