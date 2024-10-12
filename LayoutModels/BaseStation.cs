using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels
{
    internal class BaseStation
    {
        public string StationID { get; set; } = "ST";
        public List<string> Locations { get; set; } = new();

        public bool Busy { get; set; } = false;

    }
}
