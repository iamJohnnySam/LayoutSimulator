using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels.Support
{
    public enum MapCodes
    {
        Empty = 0,
        Available = 1,
        Double = 2,
        Cross = 3,
    }

    public enum ManipulatorArmStates
    {
        extended,
        retracted
    }

    public enum SimulatorStates
    {
        Initialized,
        AutoRun,
        Uninitialized
    }
}
