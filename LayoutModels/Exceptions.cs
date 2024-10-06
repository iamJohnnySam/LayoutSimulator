using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels
{
    class ErrorResponse : Exception
    {
        public ErrorResponse(string message)
        {
        }
    }
}
