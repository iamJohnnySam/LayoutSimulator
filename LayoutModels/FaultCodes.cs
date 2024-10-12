using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels
{
    public enum FaultCodes
    {
        ProgramError,
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
        StationDoesNotHaveDoor
    }
}
