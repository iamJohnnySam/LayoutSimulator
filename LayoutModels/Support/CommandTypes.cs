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
        DOOROPEN,
        DOORCLOSE,
        MAP,
        DOCK,
        SDOCK,
        UNDOCK,
        PROCESS0,
        PROCESS1,
        PROCESS2,
        PROCESS3,
        PROCESS4,
        PROCESS5,
        PROCESS6,
        PROCESS7,
        PROCESS8,
        PROCESS9,
        POWER,
        POWERON,
        POWEROFF,
        HOME,
        READSLOT,
        READPOD,

        POD,
        PAYLOAD,
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
