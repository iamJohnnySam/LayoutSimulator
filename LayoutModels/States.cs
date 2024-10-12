using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels
{
    public enum MapCodes
    {
        Empty = 0,
        Available = 1,
        Double = 2,
        Cross = 3,
    }

    public enum DoorStates
    {
        Open,
        Close,
        Opening,
        Closing,
        Mapping
    }
}
