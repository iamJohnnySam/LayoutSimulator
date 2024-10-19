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
        public Dictionary<String, List<CommandTypes>> CommandMap { get; set; } = new() 
        {
            { "ROBOTPICK", new List<CommandTypes> { CommandTypes.Pick } },
            { "ROBOTPLACE", new List<CommandTypes> { CommandTypes.Place } },
            { "LOAD", new List<CommandTypes> { CommandTypes.DoorOpen } },
            { "UNLOAD", new List<CommandTypes> { CommandTypes.DoorClose } },
            { "DOOROPEN", new List<CommandTypes> { CommandTypes.DoorOpen } },
            { "DOORCLOSE", new List<CommandTypes> { CommandTypes.DoorClose } },
            { "LOADANDMAP", new List<CommandTypes> { CommandTypes.Map } },
            { "REMAP", new List<CommandTypes> { CommandTypes.DoorClose, CommandTypes.Map } },
            { "DOCK", new List<CommandTypes> { CommandTypes.Dock } },
            { "SDOCK", new List<CommandTypes> { CommandTypes.Sdock } },
            { "UNDOCK", new List<CommandTypes> { CommandTypes.Undock } },
            { "PROCESS", new List<CommandTypes> { CommandTypes.Process0 } },
            { "ALIGN", new List<CommandTypes> { CommandTypes.Process1 } },
            { "SERVOON", new List<CommandTypes> { CommandTypes.PowerOn } },
            { "SERVOOFF", new List<CommandTypes> { CommandTypes.PowerOff } },
            { "HOME", new List<CommandTypes> { CommandTypes.Home } },
            { "READRFID", new List<CommandTypes> { CommandTypes.ReadPod } },
            { "OCR", new List<CommandTypes> { CommandTypes.ReadSlot } },
            { "POD", new List<CommandTypes> { CommandTypes.Pod } },
            { "PAYLOAD", new List<CommandTypes> { CommandTypes.Payload } }
        };
        public Dictionary<String, List<CommandArgTypes>> CommandArgs { get; set; } = new() 
        {
            { "ROBOTPICK", new List<CommandArgTypes> { CommandArgTypes.EndEffector, CommandArgTypes.TargetStation, CommandArgTypes.Slot } },
            { "ROBOTPLACE", new List<CommandArgTypes> { CommandArgTypes.EndEffector, CommandArgTypes.TargetStation, CommandArgTypes.Slot } },
            { "LOAD", new List<CommandArgTypes> { } },
            { "UNLOAD", new List<CommandArgTypes> { } },
            { "DOOROPEN", new List<CommandArgTypes> { } },
            { "DOORCLOSE", new List<CommandArgTypes> { } },
            { "LOADANDMAP", new List<CommandArgTypes> { } },
            { "REMAP", new List<CommandArgTypes> { } },
            { "DOCK", new List<CommandArgTypes> { } },
            { "SDOCK", new List<CommandArgTypes> { CommandArgTypes.PodID } },
            { "UNDOCK", new List<CommandArgTypes> { } },
            { "PROCESS", new List<CommandArgTypes> { } },
            { "ALIGN", new List<CommandArgTypes> { CommandArgTypes.Ignore } },
            { "SERVOON", new List<CommandArgTypes> { } },
            { "SERVOOFF", new List<CommandArgTypes> { } },
            { "HOME", new List<CommandArgTypes> { } },
            { "READRFID", new List<CommandArgTypes> { } },
            { "OCR", new List<CommandArgTypes> { } },
            { "POD", new List<CommandArgTypes> { CommandArgTypes.Capacity, CommandArgTypes.Type } },
            { "PAYLOAD", new List<CommandArgTypes> { CommandArgTypes.PodID, CommandArgTypes.Slot} }

        };
        public Dictionary<ResponseTypes, String> ResponseMap { get; set; } = new() 
        {
            { ResponseTypes.Ack, "ACK" },
            { ResponseTypes.Nack, "NAK" },
            { ResponseTypes.Success, "SUCCESS" },
            { ResponseTypes.Error, "ERROR" }
        };
    }
}
