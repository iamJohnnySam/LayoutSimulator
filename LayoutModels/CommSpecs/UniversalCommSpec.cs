using LayoutModels.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LayoutCommands;

namespace LayoutModels.CommSpecs
{
    public class UniversalCommSpec : ICommSpec
    {
        public Dictionary<String, List<CommandType>> CommandMap { get; set; } = new() 
        {
            { "ROBOTPICK", new List<CommandType> { CommandType.Pick } },
            { "ROBOTPLACE", new List<CommandType> { CommandType.Place } },
            { "LOAD", new List<CommandType> { CommandType.DoorOpen } },
            { "UNLOAD", new List<CommandType> { CommandType.DoorClose } },
            { "DOOROPEN", new List<CommandType> { CommandType.DoorOpen } },
            { "DOORCLOSE", new List<CommandType> { CommandType.DoorClose } },
            { "LOADANDMAP", new List<CommandType> { CommandType.Map } },
            { "REMAP", new List<CommandType> { CommandType.DoorClose, CommandType.Map } },
            { "DOCK", new List<CommandType> { CommandType.Dock } },
            { "SDOCK", new List<CommandType> { CommandType.Sdock } },
            { "UNDOCK", new List<CommandType> { CommandType.Undock } },
            { "PROCESS", new List<CommandType> { CommandType.Process0 } },
            { "ALIGN", new List<CommandType> { CommandType.Process1 } },
            { "SERVOON", new List<CommandType> { CommandType.PowerOn } },
            { "SERVOOFF", new List<CommandType> { CommandType.PowerOff } },
            { "HOME", new List<CommandType> { CommandType.Home } },
            { "READRFID", new List<CommandType> { CommandType.ReadPod } },
            { "OCR", new List<CommandType> { CommandType.ReadSlot } },
            { "POD", new List<CommandType> { CommandType.Pod } },
            { "PAYLOAD", new List<CommandType> { CommandType.Payload } },
            { "START", new List<CommandType> { CommandType.StartSim } }
        };
        public Dictionary<String, List<CommandArgType>> CommandArgs { get; set; } = new();
        public Dictionary<ResponseTypes, String> ResponseMap { get; set; } = new();

        public UniversalCommSpec()
        {

            CommandArgs.Add("ROBOTPICK", new List<CommandArgType> { CommandArgType.EndEffector, CommandArgType.TargetStation, CommandArgType.Slot });
            CommandArgs.Add("ROBOTPLACE", new List<CommandArgType> { CommandArgType.EndEffector, CommandArgType.TargetStation, CommandArgType.Slot });
            CommandArgs.Add("LOAD", new List<CommandArgType> { });
            CommandArgs.Add("UNLOAD", new List<CommandArgType> { });
            CommandArgs.Add("DOOROPEN", new List<CommandArgType> { });
            CommandArgs.Add("DOORCLOSE", new List<CommandArgType> { });
            CommandArgs.Add("LOADANDMAP", new List<CommandArgType> { });
            CommandArgs.Add("REMAP", new List<CommandArgType> { });
            CommandArgs.Add("DOCK", new List<CommandArgType> { });
            CommandArgs.Add("SDOCK", new List<CommandArgType> { CommandArgType.PodID });
            CommandArgs.Add("UNDOCK", new List<CommandArgType> { });
            CommandArgs.Add("PROCESS", new List<CommandArgType> { });
            CommandArgs.Add("ALIGN", new List<CommandArgType> { CommandArgType.Ignore });
            CommandArgs.Add("SERVOON", new List<CommandArgType> { });
            CommandArgs.Add("SERVOOFF", new List<CommandArgType> { });
            CommandArgs.Add("HOME", new List<CommandArgType> { });

            CommandArgs.Add("READRFID", new List<CommandArgType> { });
            CommandArgs.Add("OCR", new List<CommandArgType> { });

            CommandArgs.Add("POD", new List<CommandArgType> { CommandArgType.Capacity, CommandArgType.Type });
            CommandArgs.Add("PAYLOAD", new List<CommandArgType> { CommandArgType.PodID, CommandArgType.Slot});


            ResponseMap.Add(ResponseTypes.Ack, "ACK");
            ResponseMap.Add(ResponseTypes.Nack, "NAK");
            ResponseMap.Add(ResponseTypes.Success, "SUCCESS");
            ResponseMap.Add(ResponseTypes.Error, "ERROR");
        }
    }
}
