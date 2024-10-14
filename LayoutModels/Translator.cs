﻿using LayoutModels.CommSpecs;
using LayoutModels.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LayoutModels
{
    public class Translator
    {
        public event EventHandler<LogMessage>? Log;

        Dictionary<string, List<string>> commands = new();

        private string commandDelimiter = ",";
        private string? commandStart = null;
        private string? commandEnd = null;

        private bool HasTarget = false;
        private string? fixedTarget = null;

        private int transactionIndex = 0;
        private int commandIndex = 1;
        private int targetIndex = 2;
        private int valueStartIndex = 3;

        public ICommSpec CommSpec { get; set; }

        public Translator(CommandStructure commStruct, ICommSpec commSpec)
        {
            HasTarget = !commStruct.DedicatedPort;

            commandDelimiter = commStruct.Delimiter;
            commandStart = commStruct.StartCharacter;
            commandEnd = commStruct.EndCharacter;

            transactionIndex = commStruct.IndexTransaction;
            commandIndex = commStruct.IndexCommand;
            targetIndex = commStruct.IndexTarget;
            fixedTarget = commStruct.FixedTarget;
            valueStartIndex = commStruct.IndexValueStart;

            CommSpec = commSpec;
        }

        private int GetIndex(CommandArgTypes value, List<CommandArgTypes> args)
        {
            int index = args.IndexOf(value);
            if (index == -1)
                throw new NackResponse(NackCodes.MissingArguments);
            return index;
        }

        public List<Job> Translate(string commandString)
        {
            int pFrom = 0;
            if (commandStart != null)
                pFrom = commandString.IndexOf(commandStart) + commandStart.Length;

            int pTo = commandString.Length;
            if (commandEnd != null)
                pTo = commandString.LastIndexOf(commandEnd);

            string recValue = commandString.Substring(pFrom, pTo - pFrom);

            List<string> vals = recValue.Split(commandDelimiter).ToList();

            string transactionID = vals[transactionIndex];
            string rawCommand = vals[commandIndex];
            string target = "";

            if (HasTarget) 
                target = vals[targetIndex];

            Log?.Invoke(this, new LogMessage(transactionID, $"Receieved Command {rawCommand} in string {recValue}."));

            if (!CommSpec.CommandMap.ContainsKey(rawCommand))
                throw new NackResponse(NackCodes.CommandError);

            if (!CommSpec.CommandArgs.ContainsKey(rawCommand))
                throw new NackResponse(NackCodes.CommSpecError);

            if (vals.Count != (CommSpec.CommandArgs[rawCommand].Count + valueStartIndex))
                throw new NackResponse(NackCodes.CommandError);

            List<CommandTypes> commands = CommSpec.CommandMap[rawCommand];
            List<Job> runCommands = new List<Job>();

            foreach (CommandTypes command in commands)
            {
                Job runCommand = new();
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
                        catch (FormatException) { throw new NackResponse(NackCodes.MissingArguments); }

                        runCommand.TargetStation = vals[valueStartIndex + GetIndex(CommandArgTypes.TargetStation, CommSpec.CommandArgs[rawCommand])];

                        try { runCommand.Slot = Int32.Parse(vals[valueStartIndex + GetIndex(CommandArgTypes.Slot, CommSpec.CommandArgs[rawCommand])]); }
                        catch (FormatException) { throw new NackResponse(NackCodes.MissingArguments); }

                        break;

                    case CommandTypes.PLACE:
                        runCommand.Action = CommandTypes.PLACE;
                        try { runCommand.EndEffector = Int32.Parse(vals[valueStartIndex + GetIndex(CommandArgTypes.EndEffector, CommSpec.CommandArgs[rawCommand])]); }
                        catch (FormatException) { throw new NackResponse(NackCodes.MissingArguments); }

                        runCommand.TargetStation = vals[valueStartIndex + GetIndex(CommandArgTypes.TargetStation, CommSpec.CommandArgs[rawCommand])];

                        try { runCommand.Slot = Int32.Parse(vals[valueStartIndex + GetIndex(CommandArgTypes.Slot, CommSpec.CommandArgs[rawCommand])]); }
                        catch (FormatException) { throw new NackResponse(NackCodes.MissingArguments); }

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
                        catch (FormatException) { throw new NackResponse(NackCodes.MissingArguments); }

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

                        if (vals[valueStartIndex + GetIndex(CommandArgTypes.PowerStatus, CommSpec.CommandArgs[rawCommand])] == "0")
                        {
                            runCommand.State = false;
                        }
                        else if (vals[valueStartIndex + GetIndex(CommandArgTypes.PowerStatus, CommSpec.CommandArgs[rawCommand])] == "1")
                        {
                            runCommand.State = true;
                        }
                        else
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
