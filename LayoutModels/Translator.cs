using LayoutModels.CommSpecs;
using LayoutModels.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;

namespace LayoutModels
{
    public class Translator(CommandStructure commStruct, ResponseStructure respStruct, ICommSpec commSpec)
    {
        public event EventHandler<LogMessage>? OnLogEvent;

        Dictionary<string, List<string>> commands = new();
        public CommandStructure ComS { get; set; } = commStruct;
        public ResponseStructure RspS { get; set; } = respStruct;
        public ICommSpec CommSpec { get; set; } = commSpec;

        private List<CommandTypes> IgnoreTargetCommands = [ CommandTypes.POD, CommandTypes.PAYLOAD ];


        private int GetIndex(CommandArgTypes value, List<CommandArgTypes> args)
        {
            int index = args.IndexOf(value);
            if (index == -1)
                throw new NackResponse(NackCodes.MissingArguments);
            return index;
        }

        public List<Job> TranslateCommand(string commandString)
        {
            int pFrom = 0;
            if (ComS.StartCharacter != null)
                pFrom = commandString.IndexOf(ComS.StartCharacter) + ComS.StartCharacter.Length;

            int pTo = commandString.Length;
            if (ComS.EndCharacter != null)
                pTo = commandString.LastIndexOf(ComS.EndCharacter);

            string recValue = commandString.Substring(pFrom, pTo - pFrom);

            List<string> vals = recValue.Split(ComS.Delimiter.ToCharArray()).ToList();

            string transactionID = vals[ComS.IndexTransaction];
            string rawCommand = vals[ComS.IndexCommand].ToUpper();

            OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Receieved Command {rawCommand} in string {recValue}."));

            if (!CommSpec.CommandMap.ContainsKey(rawCommand))
                throw new NackResponse(NackCodes.CommandError);

            if (!CommSpec.CommandArgs.ContainsKey(rawCommand))
                throw new NackResponse(NackCodes.CommSpecError);

            List<CommandTypes> commands = CommSpec.CommandMap[rawCommand];
            List<Job> runCommands = new List<Job>();

            if (commands.Count != 1 || !IgnoreTargetCommands.Contains(commands[0]))
                if (vals.Count != (CommSpec.CommandArgs[rawCommand].Count + ComS.IndexValueStart))
                    throw new NackResponse(NackCodes.CommandError);

            OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Commands identified {commands.Count} command(s)."));

            foreach (CommandTypes command in commands)
            {
                Job runCommand = new();
                runCommand.TransactionID = transactionID;

                if (!IgnoreTargetCommands.Contains(command))
                {
                    if (!ComS.DedicatedPort)
                        runCommand.Target = vals[ComS.IndexTarget];
                    else
                        runCommand.Target = ComS.FixedTarget ?? "P1";
                }
                    
                OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Translating command {command.ToString()}."));
                runCommand.Action = command;

                switch (command)
                {
                    case CommandTypes.PICK:
                        try { runCommand.EndEffector = Int32.Parse(vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.EndEffector, CommSpec.CommandArgs[rawCommand])]); }
                        catch (FormatException) { throw new NackResponse(NackCodes.MissingArguments); }

                        runCommand.TargetStation = vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.TargetStation, CommSpec.CommandArgs[rawCommand])];

                        try { runCommand.Slot = Int32.Parse(vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.Slot, CommSpec.CommandArgs[rawCommand])]); }
                        catch (FormatException) { throw new NackResponse(NackCodes.MissingArguments); }

                        break;

                    case CommandTypes.PLACE:
                        try { runCommand.EndEffector = Int32.Parse(vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.EndEffector, CommSpec.CommandArgs[rawCommand])]); }
                        catch (FormatException) { throw new NackResponse(NackCodes.MissingArguments); }

                        runCommand.TargetStation = vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.TargetStation, CommSpec.CommandArgs[rawCommand])];

                        try { runCommand.Slot = Int32.Parse(vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.Slot, CommSpec.CommandArgs[rawCommand])]); }
                        catch (FormatException) { throw new NackResponse(NackCodes.MissingArguments); }

                        break;

                    case CommandTypes.DOOR:
                        if (vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.DoorStatus, CommSpec.CommandArgs[rawCommand])] == "0")
                        {
                            runCommand.State = false;
                        }
                        else if (vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.DoorStatus, CommSpec.CommandArgs[rawCommand])] == "1")
                        {
                            runCommand.State = true;
                        }
                        else
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
                        break;


                    case CommandTypes.SDOCK:
                        runCommand.PodID = vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.PodID, CommSpec.CommandArgs[rawCommand])];
                        break;

                    case CommandTypes.POWER:

                        if (vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.PowerStatus, CommSpec.CommandArgs[rawCommand])] == "0")
                        {
                            runCommand.State = false;
                        }
                        else if (vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.PowerStatus, CommSpec.CommandArgs[rawCommand])] == "1")
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
                            catch (NackResponse)
                            {
                                GetIndex(CommandArgTypes.PowerOn, CommSpec.CommandArgs[rawCommand]);
                                runCommand.State = true;
                            }
                        }
                        break;

                    case CommandTypes.POD:
                        runCommand.Capacity = int.Parse(vals[ComS.IndexValueStart - 1 + GetIndex(CommandArgTypes.Capacity, CommSpec.CommandArgs[rawCommand])]);
                        runCommand.PayloadType = vals[ComS.IndexValueStart - 1 + GetIndex(CommandArgTypes.Type, CommSpec.CommandArgs[rawCommand])];
                        break;

                    case CommandTypes.PAYLOAD:
                        runCommand.PodID = vals[ComS.IndexValueStart - 1 + GetIndex(CommandArgTypes.PodID, CommSpec.CommandArgs[rawCommand])];
                        runCommand.Slot = int.Parse(vals[ComS.IndexValueStart - 1 + GetIndex(CommandArgTypes.Slot, CommSpec.CommandArgs[rawCommand])]);
                        break;

                }
                OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Added Command {runCommand.Action.ToString()} to list (Target = {runCommand.Target})."));
                runCommands.Add(runCommand);
            }
            return runCommands;

            // TODO: Checksum
        }

        public string TranslateResponse(string transactionID, ResponseTypes RspType, string target, string message)
        {
            List<string> response = ["", "", "", ""];

            if (RspS.IndexTransaction >= 0 && RspS.IndexTransaction < response.Count)
                response[RspS.IndexTransaction] = transactionID;

            if (RspS.IndexMessage >= 0 && RspS.IndexMessage < response.Count)
                response[RspS.IndexMessage] = CommSpec.ResponseMap[RspType];

            if (RspS.IndexTarget >= 0 && RspS.IndexTarget < response.Count)
                response[RspS.IndexTarget] = target;

            if (RspS.IndexResponseStart >= 0 && RspS.IndexResponseStart < response.Count)
                response[RspS.IndexResponseStart] = message;

            int sum = 0;
            string responseString = string.Join(RspS.Delimiter, response.Where(s => !string.IsNullOrEmpty(s)));

            string? checksum = null;
            if (RspS.CheckSum)
            {
                foreach (char c in responseString)
                    sum += (int)c;
                checksum = (sum % 256).ToString("X2");
            }

            string returnString = $"{RspS.StartCharacter}{responseString}{RspS.EndCharacter}{checksum}";

            if (RspS.CRLF)
                returnString += "\r\n" ;

            return returnString;
        }

        public string TranslateErrorResponse(string transactionID, string target, ErrorCodes code)
        {
            return TranslateResponse(transactionID, ResponseTypes.ERROR, target, code.ToString());
        }

        public string TranslateNackResponse(string transactionID, string target, NackCodes code)
        {
            return TranslateResponse(transactionID, ResponseTypes.NACK, target, code.ToString());
        }

    }
}
