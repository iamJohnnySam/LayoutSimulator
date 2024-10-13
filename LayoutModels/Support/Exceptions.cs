using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels.Support
{
    class ErrorResponse : Exception
    {
        public ErrorResponse(ErrorCodes code)
        {
        }
    }

    class NackResponse : Exception
    {
        public NackResponse(NackCodes code)
        {
        }
    }
}
