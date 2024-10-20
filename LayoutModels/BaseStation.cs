using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Communicator;
using LayoutCommands;
using LayoutModels.CommSpecs;

namespace LayoutModels
{
    public class BaseStation
    {
        public string StationID { get; set; } = "ST";
        public List<string> Locations { get; set; } = [];
        public bool Busy { get; set; } = false;
        public bool EnablePassThrough { get; set; } = false;
        public Translator? Translator { get; set; }

        TCPClient? PassThroughClient;


        public void ProcessWait(float SecsTime)
        {
            if (!EnablePassThrough || (PassThroughClient != null && PassThroughClient.isConnected))
               Thread.Sleep((int)(SecsTime * 1000));
        }

        public void EnablePassthrough(string ipAddress, int port, CommandStructure comS, ResponseStructure resS, ICommSpec commSpec)
        {
            EnablePassThrough = true;
            PassThroughClient = new TCPClient(ipAddress, port);

            Translator = new Translator(comS, resS, commSpec);
        }

        public void DiablePassthrough()
        {
            EnablePassThrough = false;
            PassThroughClient = null;
        }

        public string sendCommad(Job job)
        {
            if (Translator != null && PassThroughClient != null)
            {
                // PassThroughClient.SendDataAsync(Translator.TranslateCommandToString(job));

                // await
            }
            // TODO: Implement
            return string.Empty;
        }
    }
}
