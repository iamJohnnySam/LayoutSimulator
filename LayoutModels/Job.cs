using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LayoutModels.Support;

namespace LayoutModels
{
    public struct Job()
    {
        public string RawAction { get; set; }
        public string RawCommand {  get; set; }
        public string TransactionID { get; set; }
        public CommandTypes Action { get; set; }
        public string Target { get; set; } = "None";
        public string PodID { get; set; } = string.Empty;
        public bool State { get; set; }
        public int EndEffector { get; set; }
        public string TargetStation { get; set; }
        public int Slot { get; set; }


        public int Capacity { get; set; }
        public string PayloadType { get; set; }
    }
}
