using LayoutModels.CommSpecs;
using LayoutModels.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LayoutModels
{
    internal class Translator
    {
        Dictionary<string, List<string>> commands = new();

        private string commandDelimiter = ",";
        private string? commandStart = null;
        private string? commandEnd = null;

        private bool HasTarget = false;
        private string fixedTarget = " ";

        private int transactionIndex = 0;
        private int commandIndex = 1;
        private int targetIndex = 2;
        private int valueStartIndex = 3;

        public ICommSpec CommSpec { get; set; }

        public Translator(string startChar, string endChar, string delimiter, int indexTransaction, int indexCommand, int indexTarget, int indexValueStart, ICommSpec commSpec)
        {
            HasTarget = true;

            commandDelimiter = delimiter;
            commandStart = startChar;
            commandEnd = endChar;

            commandIndex = indexTransaction;
            targetIndex = indexTarget;
            valueStartIndex = indexValueStart;

            CommSpec = commSpec;
        }

        public Translator(string startChar, string endChar, string delimiter, int indexTransaction, int indexCommand, int indexValueStart, string _fixedTarget, ICommSpec commSpec)
        {
            HasTarget = false;

            commandDelimiter = delimiter;
            commandStart = startChar;
            commandEnd = endChar;

            fixedTarget = _fixedTarget;

            commandIndex = indexTransaction;
            valueStartIndex = indexValueStart;

            CommSpec = commSpec;
        }

        private int GetIndex(CommandArgTypes value, List<CommandArgTypes> args)
        {
            int index = args.IndexOf(value);
            if (index == -1)
                throw new ErrorResponse(FaultCodes.NACK_MissingArguments);
            return index;
        }

        public List<RunCommand> Translate(string commandString)
        {
            int pFrom = 0;
            if (commandStart != null)
                pFrom = commandString.IndexOf(commandStart) + commandStart.Length;

            int pTo = 0;
            if (commandEnd != null)
                pTo = commandString.LastIndexOf(commandEnd);

            List<string> vals = commandString.Substring(pFrom, pTo - pFrom).Split(commandDelimiter).ToList();

            string transactionID = vals[transactionIndex];
            string rawCommand = vals[commandIndex];
            string target = "";

            if (HasTarget) 
                target = vals[targetIndex];

            if (!CommSpec.CommandMap.ContainsKey(rawCommand))
                throw new ErrorResponse(FaultCodes.NACK_CommandError);

            if (!CommSpec.CommandArgs.ContainsKey(rawCommand))
                throw new ErrorResponse(FaultCodes.CommSpecError);

            if (CommSpec.CommandArgs[rawCommand].Count != (vals.Count + valueStartIndex))
                throw new ErrorResponse(FaultCodes.NACK_CommandError);

            List<CommandTypes> commands = CommSpec.CommandMap[rawCommand];
            List<RunCommand> runCommands = new List<RunCommand>();

            foreach (CommandTypes command in commands)
            {
                RunCommand runCommand = new();
                runCommand.TransactionID = transactionID;

                if (HasTarget)
                    runCommand.Target = target;
                else
                    runCommand.Target = fixedTarget;

                switch (command)
                {
                    case CommandTypes.PICK:
                        runCommand.Action = CommandTypes.PICK;
                        try { runCommand.EndEffector = Int32.Parse(vals[valueStartIndex + GetIndex(CommandArgTypes.EndEffector, CommSpec.CommandArgs[rawCommand])]); }
                        catch (FormatException) { throw new ErrorResponse(FaultCodes.NACK_MissingArguments); }

                        runCommand.TargetStation = vals[valueStartIndex + GetIndex(CommandArgTypes.TargetStation, CommSpec.CommandArgs[rawCommand])];

                        try { runCommand.Slot = Int32.Parse(vals[valueStartIndex + GetIndex(CommandArgTypes.Slot, CommSpec.CommandArgs[rawCommand])]); }
                        catch (FormatException) { throw new ErrorResponse(FaultCodes.NACK_MissingArguments); }

                        break;

                    case CommandTypes.PLACE:
                        runCommand.Action = CommandTypes.PLACE;
                        try { runCommand.EndEffector = Int32.Parse(vals[valueStartIndex + GetIndex(CommandArgTypes.EndEffector, CommSpec.CommandArgs[rawCommand])]); }
                        catch (FormatException) { throw new ErrorResponse(FaultCodes.NACK_MissingArguments); }

                        runCommand.TargetStation = vals[valueStartIndex + GetIndex(CommandArgTypes.TargetStation, CommSpec.CommandArgs[rawCommand])];

                        try { runCommand.Slot = Int32.Parse(vals[valueStartIndex + GetIndex(CommandArgTypes.Slot, CommSpec.CommandArgs[rawCommand])]); }
                        catch (FormatException) { throw new ErrorResponse(FaultCodes.NACK_MissingArguments); }

                        break;

                    case CommandTypes.DOOR:
                        runCommand.Action = CommandTypes.DOOR;
                        try { runCommand.State = bool.Parse(vals[valueStartIndex + GetIndex(CommandArgTypes.DoorStatus, CommSpec.CommandArgs[rawCommand])]); }
                        catch (ErrorResponse)
                        {
                            try
                            {
                                GetIndex(CommandArgTypes.DoorOpen, CommSpec.CommandArgs[rawCommand]);
                                runCommand.State = false;
                            }
                            catch
                            {
                                GetIndex(CommandArgTypes.DoorClose, CommSpec.CommandArgs[rawCommand]);
                                runCommand.State = true;
                            }
                        }
                        catch (FormatException) { throw new ErrorResponse(FaultCodes.NACK_MissingArguments); }

                        break;

                    case CommandTypes.MAP:
                        runCommand.Action = CommandTypes.MAP;
                        break;

                    case CommandTypes.DOCK:
                        runCommand.Action = CommandTypes.DOCK;
                        runCommand.PodID = vals[valueStartIndex + GetIndex(CommandArgTypes.PodID, CommSpec.CommandArgs[rawCommand])];
                        break;

                    case CommandTypes.UNDOCK:
                        runCommand.Action = CommandTypes.UNDOCK;
                        break;

                    case CommandTypes.PROCESS:
                        runCommand.Action = CommandTypes.PROCESS;
                        break;

                    case CommandTypes.POWER:
                        runCommand.Action = CommandTypes.POWER;
                        try { runCommand.State = bool.Parse(vals[valueStartIndex + GetIndex(CommandArgTypes.PowerStatus, CommSpec.CommandArgs[rawCommand])]); }
                        catch (ErrorResponse)
                        {
                            try
                            {
                                GetIndex(CommandArgTypes.PowerOff, CommSpec.CommandArgs[rawCommand]);
                                runCommand.State = false;
                            }
                            catch
                            {
                                GetIndex(CommandArgTypes.PowerOn, CommSpec.CommandArgs[rawCommand]);
                                runCommand.State = true;
                            }
                        }
                        catch (FormatException) { throw new ErrorResponse(FaultCodes.NACK_MissingArguments); }

                        break;

                    case CommandTypes.HOME:
                        runCommand.Action = CommandTypes.HOME;
                        break;

                }
                runCommands.Add(runCommand);
            }
            return runCommands;
        }

    }
}
