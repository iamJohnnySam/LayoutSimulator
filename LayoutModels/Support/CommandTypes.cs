using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels.Support
{
    public enum CommandTypes
    {
        PICK,
        PLACE,
        DOOR,
        MAP,
        DOCK,
        UNDOCK,
        PROCESS,
        POWER,
        HOME
    }

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
        PowerOff
    }

    public enum ResponseTypes
    {
        ACK,
        NACK,
        SUCCESS,
        ERROR
    }
}
