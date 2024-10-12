using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels.Support
{
    class ErrorResponse : Exception
    {
        public ErrorResponse(FaultCodes code)
        {
        }
    }
}
