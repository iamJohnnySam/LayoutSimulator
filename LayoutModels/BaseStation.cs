using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Communicator;

namespace LayoutModels
{
    public class BaseStation
    {
        public string StationID { get; set; } = "ST";
        public List<string> Locations { get; set; } = new();

        public bool Busy { get; set; } = false;

        public bool EnablePassThrough { get; set; } = false;
        TCPClient? PassThroughClient;


        public void EnablePassthrough(string ipAddress, int port)
        {
            EnablePassThrough = true;
            PassThroughClient = new TCPClient(ipAddress, port);
        }

        public void DiablePassthrough()
        {
            EnablePassThrough = false;
            PassThroughClient = null;
        }

    }
}
