using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels.Support
{
    class ErrorResponse : Exception
    {
        public ErrorCodes Code { get; private set; }
        public ErrorResponse(ErrorCodes code) : base($"An error occurred: {code}")
        {
            Code = code;
        }
    }

    class NackResponse : Exception
    {
        public NackCodes Code { get; private set; }
        public NackResponse(NackCodes code) : base($"An error occurred: {code}")
        {
            Code = code;
        }
    }
}
