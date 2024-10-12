using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels.Support
{
    public enum FaultCodes
    {
        ProgramError,
        CommSpecError,
        NACK_CommandError,
        NACK_MissingArguments,
        Busy,
        NotDockable,
        PodAlreadyAvailable,
        PodNotAvailable,
        PayloadAlreadyAvailable,
        PayloadNotAvailable,
        SlotsEmpty,
        SlotsNotEmpty,
        NotAccessible,
        PayloadTypeMismatch,
        NotMappable,
        StationDoesNotHaveDoor,
        PowerOff,
        PowerOffWhileBusy,
        EndEffectorMissing,
        SlotIndexMissing,
        StationNotReachable,
        UnknownArmState,
    }
}
