using LayoutModels.CommSpecs;
using LayoutModels.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;

namespace LayoutModels
{
    public class Translator(CommandStructure commStruct, ResponseStructure ackStruct, ResponseStructure respStruct, ICommSpec commSpec)
    {
        public event EventHandler<LogMessage>? OnLogEvent;

        Dictionary<string, List<string>> commands = new();
        public CommandStructure ComS { get; set; } = commStruct;
        public ResponseStructure RspS { get; set; } = respStruct;
        public ResponseStructure AckS { get; set; } = ackStruct;
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

            string rawCommand = commandString.Substring(pFrom, pTo - pFrom);

            List<string> vals = rawCommand.Split(ComS.Delimiter.ToCharArray()).ToList();

            string transactionID = vals[ComS.IndexTransaction];

            List<string> valsWithoutTxId = new List<string>(vals);
            valsWithoutTxId.RemoveAt(ComS.IndexTransaction);
            rawCommand = string.Join(ComS.Delimiter[0], valsWithoutTxId);

            string rawAction = vals[ComS.IndexCommand].ToUpper();

            OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Receieved Command {rawAction} in string {rawCommand}."));

            if (!CommSpec.CommandMap.ContainsKey(rawAction))
                throw new NackResponse(NackCodes.CommandError);

            if (!CommSpec.CommandArgs.ContainsKey(rawAction))
                throw new NackResponse(NackCodes.CommSpecError);

            List<CommandTypes> commands = CommSpec.CommandMap[rawAction];
            List<Job> runCommands = new List<Job>();

            if (commands.Count != 1 || !IgnoreTargetCommands.Contains(commands[0]))
                if (vals.Count != (CommSpec.CommandArgs[rawAction].Count + ComS.IndexValueStart))
                    throw new NackResponse(NackCodes.CommandError);

            OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Commands identified {commands.Count} command(s)."));

            foreach (CommandTypes command in commands)
            {
                Job runCommand = new();
                runCommand.TransactionID = transactionID;
                runCommand.RawAction = rawAction;
                runCommand.RawCommand = rawCommand;

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
                        try { runCommand.EndEffector = Int32.Parse(vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.EndEffector, CommSpec.CommandArgs[rawAction])]); }
                        catch (FormatException) { throw new NackResponse(NackCodes.MissingArguments); }

                        runCommand.TargetStation = vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.TargetStation, CommSpec.CommandArgs[rawAction])];

                        try { runCommand.Slot = Int32.Parse(vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.Slot, CommSpec.CommandArgs[rawAction])]); }
                        catch (FormatException) { throw new NackResponse(NackCodes.MissingArguments); }

                        break;

                    case CommandTypes.PLACE:
                        try { runCommand.EndEffector = Int32.Parse(vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.EndEffector, CommSpec.CommandArgs[rawAction])]); }
                        catch (FormatException) { throw new NackResponse(NackCodes.MissingArguments); }

                        runCommand.TargetStation = vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.TargetStation, CommSpec.CommandArgs[rawAction])];

                        try { runCommand.Slot = Int32.Parse(vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.Slot, CommSpec.CommandArgs[rawAction])]); }
                        catch (FormatException) { throw new NackResponse(NackCodes.MissingArguments); }

                        break;

                    case CommandTypes.DOOR:
                        if (vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.DoorStatus, CommSpec.CommandArgs[rawAction])] == "0")
                        {
                            runCommand.State = false;
                        }
                        else if (vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.DoorStatus, CommSpec.CommandArgs[rawAction])] == "1")
                        {
                            runCommand.State = true;
                        }
                        else
                        {
                            try
                            {
                                GetIndex(CommandArgTypes.DoorOpen, CommSpec.CommandArgs[rawAction]);
                                runCommand.State = false;
                            }
                            catch
                            {
                                GetIndex(CommandArgTypes.DoorClose, CommSpec.CommandArgs[rawAction]);
                                runCommand.State = true;
                            }
                        }
                        break;


                    case CommandTypes.SDOCK:
                        runCommand.PodID = vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.PodID, CommSpec.CommandArgs[rawAction])];
                        break;

                    case CommandTypes.POWER:

                        if (vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.PowerStatus, CommSpec.CommandArgs[rawAction])] == "0")
                        {
                            runCommand.State = false;
                        }
                        else if (vals[ComS.IndexValueStart + GetIndex(CommandArgTypes.PowerStatus, CommSpec.CommandArgs[rawAction])] == "1")
                        {
                            runCommand.State = true;
                        }
                        else
                        {
                            try
                            {
                                GetIndex(CommandArgTypes.PowerOff, CommSpec.CommandArgs[rawAction]);
                                runCommand.State = false;
                            }
                            catch (NackResponse)
                            {
                                GetIndex(CommandArgTypes.PowerOn, CommSpec.CommandArgs[rawAction]);
                                runCommand.State = true;
                            }
                        }
                        break;

                    case CommandTypes.POD:
                        runCommand.Capacity = int.Parse(vals[ComS.IndexValueStart - 1 + GetIndex(CommandArgTypes.Capacity, CommSpec.CommandArgs[rawAction])]);
                        runCommand.PayloadType = vals[ComS.IndexValueStart - 1 + GetIndex(CommandArgTypes.Type, CommSpec.CommandArgs[rawAction])];
                        break;

                    case CommandTypes.PAYLOAD:
                        runCommand.PodID = vals[ComS.IndexValueStart - 1 + GetIndex(CommandArgTypes.PodID, CommSpec.CommandArgs[rawAction])];
                        runCommand.Slot = int.Parse(vals[ComS.IndexValueStart - 1 + GetIndex(CommandArgTypes.Slot, CommSpec.CommandArgs[rawAction])]);
                        break;

                }
                OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Added Command {runCommand.Action.ToString()} to list (Target = {runCommand.Target})."));
                runCommands.Add(runCommand);
            }
            return runCommands;

            // TODO: Checksum
        }

        public string TranslateResponse(Job command, ResponseTypes RspType, string message)
        {
            ResponseStructure UseS = AckS;

            switch (RspType)
            {
                case ResponseTypes.ACK:
                case ResponseTypes.NACK:
                    UseS = AckS; break;
                case ResponseTypes.ERROR:
                case ResponseTypes.SUCCESS:
                    UseS = RspS; break;
            }

            List<string> response = ["", "", "", "", ""];

            if (UseS.IndexTransaction >= 0 && UseS.IndexTransaction < response.Count)
                response[UseS.IndexTransaction] = command.TransactionID;

            if (UseS.IndexMessage >= 0 && UseS.IndexMessage < response.Count)
                response[UseS.IndexMessage] = CommSpec.ResponseMap[RspType];

            if (UseS.IndexTarget >= 0 && UseS.IndexTarget < response.Count)
                response[UseS.IndexTarget] = command.Target;

            if (UseS.OriginalCommandIndex >= 0 && UseS.OriginalCommandIndex < response.Count)
                response[UseS.OriginalCommandIndex] = command.RawCommand;

            if (UseS.IndexResponseStart >= 0 && UseS.IndexResponseStart < response.Count)
                response[UseS.IndexResponseStart] = message;

            int sum = 0;
            string responseString = string.Join(UseS.Delimiter, response.Where(s => !string.IsNullOrEmpty(s)));

            string? checksum = null;
            if (UseS.CheckSum)
            {
                foreach (char c in responseString)
                    sum += (int)c;
                checksum = (sum % 256).ToString("X2");
            }

            string returnString = $"{UseS.StartCharacter}{responseString}{UseS.EndCharacter}{checksum}";

            if (UseS.CRLF)
                returnString += "\r\n" ;

            return returnString;
        }

        public string TranslateErrorResponse(Job command, ErrorCodes code)
        {
            return TranslateResponse(command, ResponseTypes.ERROR, code.ToString());
        }

        public string TranslateNackResponse(Job command, NackCodes code)
        {
            return TranslateResponse(command, ResponseTypes.NACK, code.ToString());
        }

    }
}
