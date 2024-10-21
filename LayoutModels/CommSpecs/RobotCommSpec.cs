using LayoutModels.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LayoutCommands;

namespace LayoutModels.CommSpecs
{
    public class RobotCommSpec : ICommSpec
    {
        public Dictionary<String, List<CommandType>> CommandMap { get; set; } = new()
        {
            { "GETS", new List<CommandType> { CommandType.Pick } },
            { "PUTS", new List<CommandType> { CommandType.Place } },
            { "SERV", new List<CommandType> { CommandType.PowerOn } },
            { "STOP", new List<CommandType> { CommandType.PowerOff } }
        };
        public Dictionary<String, List<CommandArgType>> CommandArgs { get; set; } = new()
        {
            { "GETS", new List<CommandArgType> { CommandArgType.EndEffector, CommandArgType.TargetStation, CommandArgType.Slot } },
            { "PUTS", new List<CommandArgType> { CommandArgType.EndEffector, CommandArgType.TargetStation, CommandArgType.Slot } },
            { "SERV", new List<CommandArgType> { } },
            { "STOP", new List<CommandArgType> { } }
        };
        public Dictionary<ResponseType, String> ResponseMap { get; set; } = new()
        {
            { ResponseType.Ack, "Ack" },
            { ResponseType.Nack, "Nak" },
            { ResponseType.Success, "Success" },
            { ResponseType.Error, "Error" }
        };
        public Dictionary<string, string> StationMapping { get; set; } = new()
        {
            { "L1", "P1" },
            { "L2", "P2" },
            { "L3", "P3" },
            { "P1", "P4" },
            { "P2", "P5" },
        };
    }
}
