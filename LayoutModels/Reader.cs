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
        public Station TargetStation { get; set; }
        public int SlotID { get; set; }
        private bool ReadSlot { get; set; }

        public Reader(Station targetStation, int slot) 
        {
            TargetStation = targetStation;
            SlotID = slot;
            ReadSlot = true;
        }

        public Reader(Station targetStation)
        {
            TargetStation = targetStation;
            SlotID = 1;

            if (!TargetStation.PodDockable)
                ReadSlot = true;
            else
                ReadSlot = false;
        }

        public string ReadID()
        {
            if (ReadSlot)
            {
                if (TargetStation.slots.ContainsKey(SlotID))
                    return TargetStation.slots[SlotID].PayloadID;
                else
                    throw new ErrorResponse(ErrorCodes.PayloadNotAvailable);
            }
            else
            {
                if (TargetStation.StatusPodDocked)
                    return TargetStation.PodID;
                else
                    throw new ErrorResponse(ErrorCodes.PodNotAvailable);
            }
        }

        public void CheckAvailable(){ }
    }
}
