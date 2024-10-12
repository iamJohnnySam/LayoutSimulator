using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels
{
    internal static class TimeKeeper
    {
        public static void ProcessWait(float SecsTime)
        {
            Thread.Sleep((int)(SecsTime * 1000));
        }
    }
}
