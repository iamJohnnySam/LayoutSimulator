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
            CommandMap.Add("ROBOTPICK", new List<CommandTypes> { CommandTypes.PICK });
            CommandMap.Add("ROBOTPLACE", new List<CommandTypes> { CommandTypes.PLACE });
            CommandMap.Add("LOAD", new List<CommandTypes> { CommandTypes.DOOROPEN });
            CommandMap.Add("UNLOAD", new List<CommandTypes> { CommandTypes.DOORCLOSE });
            CommandMap.Add("DOOROPEN", new List<CommandTypes> { CommandTypes.DOOROPEN });
            CommandMap.Add("DOORCLOSE", new List<CommandTypes> { CommandTypes.DOORCLOSE });
            CommandMap.Add("LOADANDMAP", new List<CommandTypes> { CommandTypes.MAP });
            CommandMap.Add("REMAP", new List<CommandTypes> { CommandTypes.DOORCLOSE, CommandTypes.MAP });
            CommandMap.Add("DOCK", new List<CommandTypes> { CommandTypes.DOCK });
            CommandMap.Add("SDOCK", new List<CommandTypes> { CommandTypes.SDOCK });
            CommandMap.Add("UNDOCK", new List<CommandTypes> { CommandTypes.UNDOCK });
            CommandMap.Add("PROCESS", new List<CommandTypes> { CommandTypes.PROCESS0 });
            CommandMap.Add("ALIGN", new List<CommandTypes> { CommandTypes.PROCESS1 });
            CommandMap.Add("SERVOON", new List<CommandTypes> { CommandTypes.POWERON });
            CommandMap.Add("SERVOOFF", new List<CommandTypes> { CommandTypes.POWEROFF });
            CommandMap.Add("HOME", new List<CommandTypes> { CommandTypes.HOME });

            CommandMap.Add("READRFID", new List<CommandTypes> { CommandTypes.READPOD });
            CommandMap.Add("OCR", new List<CommandTypes> { CommandTypes.READSLOT });

            CommandMap.Add("POD", new List<CommandTypes> { CommandTypes.POD });
            CommandMap.Add("PAYLOAD", new List<CommandTypes> { CommandTypes.PAYLOAD });
           

            CommandArgs.Add("ROBOTPICK", new List<CommandArgTypes> { CommandArgTypes.EndEffector, CommandArgTypes.TargetStation, CommandArgTypes.Slot });
            CommandArgs.Add("ROBOTPLACE", new List<CommandArgTypes> { CommandArgTypes.EndEffector, CommandArgTypes.TargetStation, CommandArgTypes.Slot });
            CommandArgs.Add("LOAD", new List<CommandArgTypes> { });
            CommandArgs.Add("UNLOAD", new List<CommandArgTypes> { });
            CommandArgs.Add("DOOROPEN", new List<CommandArgTypes> { });
            CommandArgs.Add("DOORCLOSE", new List<CommandArgTypes> { });
            CommandArgs.Add("LOADANDMAP", new List<CommandArgTypes> { });
            CommandArgs.Add("REMAP", new List<CommandArgTypes> { });
            CommandArgs.Add("DOCK", new List<CommandArgTypes> { });
            CommandArgs.Add("SDOCK", new List<CommandArgTypes> { CommandArgTypes.PodID });
            CommandArgs.Add("UNDOCK", new List<CommandArgTypes> { });
            CommandArgs.Add("PROCESS", new List<CommandArgTypes> { });
            CommandArgs.Add("ALIGN", new List<CommandArgTypes> { CommandArgTypes.Ignore });
            CommandArgs.Add("SERVOON", new List<CommandArgTypes> { });
            CommandArgs.Add("SERVOOFF", new List<CommandArgTypes> { });
            CommandArgs.Add("HOME", new List<CommandArgTypes> { });

            CommandArgs.Add("READRFID", new List<CommandArgTypes> { });
            CommandArgs.Add("OCR", new List<CommandArgTypes> { });

            CommandArgs.Add("POD", new List<CommandArgTypes> { CommandArgTypes.Capacity, CommandArgTypes.Type });
            CommandArgs.Add("PAYLOAD", new List<CommandArgTypes> { CommandArgTypes.PodID, CommandArgTypes.Slot});


            ResponseMap.Add(ResponseTypes.ACK, "ACK");
            ResponseMap.Add(ResponseTypes.NACK, "NAK");
            ResponseMap.Add(ResponseTypes.SUCCESS, "SUCCESS");
            ResponseMap.Add(ResponseTypes.ERROR, "ERROR");
        }
    }
}
