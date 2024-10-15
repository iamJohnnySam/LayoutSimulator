using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels.Support
{
    public enum ErrorCodes
    {
        ProgramError,
        PodAlreadyAvailable,
        PodNotAvailable,
        PayloadAlreadyAvailable,
        PayloadNotAvailable,
        SlotsEmpty,
        SlotsNotEmpty,
        NotAccessible,
        PowerOffWhileBusy,
        StationNotReachable,
        UnknownArmState,
        PayloadTypeMismatch,
        SlotIndexMissing,
        IncorrectState
    }
    public enum NackCodes
    {
        CommSpecError,
        CommandError,
        TargetNotExist,
        MissingArguments,
        Busy,
        NotDockable,
        NotMappable,
        StationDoesNotHaveDoor,
        PowerOff,
        EndEffectorMissing,
    }
}
