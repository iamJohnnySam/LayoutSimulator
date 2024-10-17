using LayoutModels.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels
{
    public class Reader: ITarget
    {
        public event EventHandler<LogMessage>? OnLogEvent;

        public string ReaderID { get; set; }
        public Station TargetStation { get; set; }
        public int SlotID { get; set; }
        private bool ReadSlot { get; set; }

        public Reader(string readerID, Station targetStation, int slot) 
        {
            ReaderID = readerID;
            TargetStation = targetStation;
            SlotID = slot;
            ReadSlot = true;
        }

        public Reader(string readerID, Station targetStation)
        {
            ReaderID = readerID;
            TargetStation = targetStation;
            SlotID = 1;

            if (!TargetStation.PodDockable)
                ReadSlot = true;
            else
                ReadSlot = false;
        }

        public string ReadID(string transactionID)
        {
            string value;
            if (ReadSlot)
            {
                if (TargetStation.slots.ContainsKey(SlotID))
                {
                    value = TargetStation.slots[SlotID].PayloadID;
                    OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Reader {ReadID} returned slot ID {value} at {TargetStation.StationID}"));
                }
                else
                    throw new ErrorResponse(ErrorCodes.PayloadNotAvailable);
            }
            else
            {
                if ((TargetStation.State != StationState.UnDocked) && TargetStation.PodDockable)
                {
                    value = TargetStation.PodID;
                    OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Reader {ReadID} returned Pod ID {value} at {TargetStation.StationID}"));
                }
                else
                    throw new ErrorResponse(ErrorCodes.PodNotAvailable);
            }
            return value;
        }

        public void CheckAvailable(){ }
    }
}
