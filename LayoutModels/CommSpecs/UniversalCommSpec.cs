using LayoutModels.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels.CommSpecs
{
    public class UniversalCommSpec : ICommSpec
    {
        public Dictionary<String, List<CommandTypes>> CommandMap { get; set; } = new();
        public Dictionary<String, List<CommandArgTypes>> CommandArgs { get; set; } = new();
        public Dictionary<ResponseTypes, String> ResponseMap { get; set; } = new();

        public UniversalCommSpec()
        {
            CommandMap.Add("GET", new List<CommandTypes> { CommandTypes.PICK });
            CommandMap.Add("PUT", new List<CommandTypes> { CommandTypes.PLACE });
            CommandMap.Add("LOAD", new List<CommandTypes> { CommandTypes.DOOR });
            CommandMap.Add("UNLOAD", new List<CommandTypes> { CommandTypes.DOOR });
            CommandMap.Add("LOADMAP", new List<CommandTypes> { CommandTypes.MAP });
            CommandMap.Add("REMAP", new List<CommandTypes> { CommandTypes.MAP });
            CommandMap.Add("DOCK", new List<CommandTypes> { CommandTypes.DOCK });
            CommandMap.Add("UNDOCK", new List<CommandTypes> { CommandTypes.UNDOCK });
            CommandMap.Add("PROCESS", new List<CommandTypes> { CommandTypes.PROCESS });
            CommandMap.Add("POWER", new List<CommandTypes> { CommandTypes.POWER });
            CommandMap.Add("HOME", new List<CommandTypes> { CommandTypes.HOME });

            CommandArgs.Add("GET", new List<CommandArgTypes> { CommandArgTypes.EndEffector, CommandArgTypes.TargetStation, CommandArgTypes.Slot });
            CommandArgs.Add("PUT", new List<CommandArgTypes> { CommandArgTypes.EndEffector, CommandArgTypes.TargetStation, CommandArgTypes.Slot });
            CommandArgs.Add("LOAD", new List<CommandArgTypes> {CommandArgTypes.DoorOpen });
            CommandArgs.Add("UNLOAD", new List<CommandArgTypes> { CommandArgTypes.DoorClose });
            CommandArgs.Add("LOADMAP", new List<CommandArgTypes> { });
            CommandArgs.Add("REMAP", new List<CommandArgTypes> { });
            CommandArgs.Add("DOCK", new List<CommandArgTypes> { CommandArgTypes.PodID });
            CommandArgs.Add("UNDOCK", new List<CommandArgTypes> { });
            CommandArgs.Add("PROCESS", new List<CommandArgTypes> { });
            CommandArgs.Add("POWER", new List<CommandArgTypes> { CommandArgTypes.PowerStatus });
            CommandArgs.Add("HOME", new List<CommandArgTypes> { });

            ResponseMap.Add(ResponseTypes.ACK, "ACK");
            ResponseMap.Add(ResponseTypes.NACK, "NAK");
            ResponseMap.Add(ResponseTypes.SUCCESS, "SUCCESS");
            ResponseMap.Add(ResponseTypes.ERROR, "ERROR");
        }
    }
}
