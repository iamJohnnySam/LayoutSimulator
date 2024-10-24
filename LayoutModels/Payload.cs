﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LayoutModels.Support;

namespace LayoutModels
{
    public class Payload
    {
        public event EventHandler<LogMessage>? Log;

        public Payload(string payloadID, string payloadType, string lotID, int slot)
        {
            PayloadID = payloadID;
            PayloadType = payloadType;
            LotID = lotID;
            Slot = slot;
        }

        public string PayloadID { get; set; }
        public string PayloadType { get; private set; }
        public string LotID { get; set; }
        public int Slot { get; set; }

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
