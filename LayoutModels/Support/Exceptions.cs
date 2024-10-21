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
        public string ErrorMessage { get; private set; }

        public ErrorResponse(ErrorCodes code, string message) : base($"An error occurred: {code}")
        {
            Code = code;
            ErrorMessage = message;
        }
    }

    class NackResponse : Exception
    {
        public NackCodes Code { get; private set; }
        public string ErrorMessage {  get; private set; }

        public NackResponse(NackCodes code, string message) : base($"An error occurred: {code}")
        {
            Code = code;
            ErrorMessage = message;
        }
    }
}
