using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels
{
    internal class Payload
    {
        public Payload(string payloadID, string payloadType)
        {
            PayloadID = payloadID;
            PayloadType = payloadType;
            PayloadErrorStaus = false;
            PayloadState = "unprocessed";
        }

        public string PayloadID { get; set; }
        public string PayloadType { get; private set; }
        public bool PayloadErrorStaus { get; set; }
        public string PayloadState { get; set; }

    }
}
