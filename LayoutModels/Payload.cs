using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels
{
    internal class Payload
    {
        public event EventHandler<LogMessage>? Log;

        public Payload(string payloadID, string payloadType)
        {
            PayloadID = payloadID;
            PayloadType = payloadType;
        }

        public string PayloadID { get; set; }
        public string PayloadType { get; private set; }

        private bool payloadErrorStaus = false;
        public bool PayloadErrorStaus {
            get { return payloadErrorStaus; }
            set {
                payloadErrorStaus = value;
                Log?.Invoke(this, new LogMessage($"Payload {PayloadID} error state updated to {value}"));
            }
        }

        private string payloadState = "unprocessed";
        public string PayloadState { 
            get { return payloadState; }
            set {
                payloadState = value;
                Log?.Invoke(this, new LogMessage($"Payload {PayloadID} state updated to {value}"));
            }
        }

    }
}
