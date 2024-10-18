using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels.Support
{

    public enum CommandArgTypes
    {
        EndEffector,
        Slot,
        TargetStation,
        PodID,
        DoorStatus,
        DoorOpen,
        DoorClose,
        PowerStatus,
        PowerOn,
        PowerOff,

        Capacity,
        Type,

        Ignore
    }

    public enum ResponseTypes
    {
        ACK,
        NACK,
        SUCCESS,
        ERROR
    }
}
