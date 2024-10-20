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
        // TODO:
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
        public Dictionary<String, List<CommandArgType>> CommandArgs { get; set; } = new()
        {
            { "ROBOTPICK", new List<CommandArgType> { CommandArgType.EndEffector, CommandArgType.TargetStation, CommandArgType.Slot } },
            { "ROBOTPLACE", new List<CommandArgType> { CommandArgType.EndEffector, CommandArgType.TargetStation, CommandArgType.Slot } },
            { "LOAD", new List<CommandArgType> { } },
            { "UNLOAD", new List<CommandArgType> { } },
            { "DOOROPEN", new List<CommandArgType> { } },
            { "DOORCLOSE", new List<CommandArgType> { } },
            { "LOADANDMAP", new List<CommandArgType> { } },
            { "REMAP", new List<CommandArgType> { } },
            { "DOCK", new List<CommandArgType> { } },
            { "SDOCK", new List<CommandArgType> { CommandArgType.PodId } },
            { "UNDOCK", new List<CommandArgType> { } },
            { "PROCESS", new List<CommandArgType> { } },
            { "ALIGN", new List<CommandArgType> { CommandArgType.Ignore } },
            { "SERVOON", new List<CommandArgType> { } },
            { "SERVOOFF", new List<CommandArgType> { } },
            { "HOME", new List<CommandArgType> { } },
            { "READRFID", new List<CommandArgType> { } },
            { "OCR", new List<CommandArgType> { } },
            { "POD", new List<CommandArgType> { CommandArgType.Capacity, CommandArgType.Type } },
            { "PAYLOAD", new List<CommandArgType> { CommandArgType.PodId, CommandArgType.Slot} },
            { "START", new List<CommandArgType> { } }
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
