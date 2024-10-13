using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LayoutModels.Support;

namespace LayoutModels
{
    public struct Job(string transactionID, CommandTypes action, string target, string value, int endEffector, string station, int slot)
    {
        public string TransactionID { get; set; } = transactionID;
        public CommandTypes Action { get; set; } = action;
        public string Target { get; set; } = target;
        public string PodID { get; set; } = value;
        public bool State { get; set; }
        public int EndEffector { get; set; } = endEffector;
        public string TargetStation { get; set; } = station;
        public int Slot { get; set; } = slot;
    }
}
