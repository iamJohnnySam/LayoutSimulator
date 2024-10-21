using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels.Support
{
    public enum ErrorCodes
    {
        SimulatorStopped,
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
        IncorrectState,
        ModuleError,
        TimedOut
    }
    public enum NackCodes
    {
        SimulatorNotStarted,
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
        ModuleNack
    }
}
