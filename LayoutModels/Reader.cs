using LayoutModels.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels
{
    public class Reader: BaseStation, ITarget
    {
        // EVENTS
        public event EventHandler<LogMessage>? OnLogEvent;

        // PROPERTIES
        public Station TargetStation { get; set; }
        public int SlotID { get; set; }
        private bool ReadSlot { get; set; }

        // CONSTRUCTORS
        public Reader(string readerID, Station targetStation, int slot) 
        {
            StationID = readerID;
            TargetStation = targetStation;
            SlotID = slot;
            ReadSlot = true;

            targetStation.PairReader(readerID, slot);
            OnBaseLogEvent += Reader_OnBaseLogEvent;
        }
        public Reader(string readerID, Station targetStation)
        {
            StationID = readerID;
            TargetStation = targetStation;
            SlotID = 1;

            if (!TargetStation.PodDockable)
                ReadSlot = true;
            else
                ReadSlot = false;

            targetStation.PairReader(readerID);
            OnBaseLogEvent += Reader_OnBaseLogEvent;
        }

        // EVENT HANDLING
        private void Reader_OnBaseLogEvent(object? sender, LogMessage e)
        {
            OnLogEvent?.Invoke(this, e);
        }

        // INTERNAL COMMANDS
        public void CheckAvailable() { }

        // COMMANDS
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
                    throw new ErrorResponse(ErrorCodes.PayloadNotAvailable, $"Reader {StationID} did not have any payload on {TargetStation.StationID} slot {SlotID} to read.");
            }
            else
            {
                value = TargetStation.PodID;
                OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Reader {ReadID} returned Pod ID {value} at {TargetStation.StationID}"));
            }
            return value;
        }
    }
}
