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
using LayoutCommands;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Security.Cryptography;
using Google.Protobuf.WellKnownTypes;

namespace LayoutModels
{
    public static class CommandRequirement
    {
        public static Dictionary<CommandType, List<CommandArgType>> CommandDict = new()
        {
            { CommandType.Pick, new List<CommandArgType>() { CommandArgType.EndEffector, CommandArgType.TargetStation, CommandArgType.Slot } },
            { CommandType.Place, new List<CommandArgType>() { CommandArgType.EndEffector, CommandArgType.TargetStation, CommandArgType.Slot } },
            { CommandType.Door, new List<CommandArgType> () { CommandArgType.DoorStatus } },
            { CommandType.DoorOpen, new List<CommandArgType>() { } },
            { CommandType.DoorClose, new List<CommandArgType>() { } },
            { CommandType.Map, new List<CommandArgType>() { } },
            { CommandType.Dock, new List<CommandArgType>() { } },
            { CommandType.Sdock, new List<CommandArgType>() { CommandArgType.PodId } },
            { CommandType.Undock, new List<CommandArgType>() { } },
            { CommandType.Process0, new List<CommandArgType>() { } },
            { CommandType.Process1, new List<CommandArgType>() { } },
            { CommandType.Process2, new List<CommandArgType>() { } },
            { CommandType.Process3, new List<CommandArgType>() { } },
            { CommandType.Process4, new List<CommandArgType>() { } },
            { CommandType.Process5, new List<CommandArgType>() { } },
            { CommandType.Process6, new List<CommandArgType>() { } },
            { CommandType.Process7, new List<CommandArgType>() { } },
            { CommandType.Process8, new List<CommandArgType>() { } },
            { CommandType.Process9, new List<CommandArgType>() { } },
            { CommandType.Power, new List<CommandArgType>() { CommandArgType.PowerStatus } },
            { CommandType.PowerOn, new List<CommandArgType>() { } },
            { CommandType.PowerOff, new List<CommandArgType>() { } },
            { CommandType.Home, new List<CommandArgType>() { } },
            { CommandType.ReadSlot, new List<CommandArgType>() { } },
            { CommandType.ReadPod, new List<CommandArgType>() { } },
            { CommandType.Pod, new List<CommandArgType>() { CommandArgType.Capacity, CommandArgType.Type } },
            { CommandType.Payload, new List<CommandArgType>() { CommandArgType.PodId, CommandArgType.Slot } }
        };
    }

    public class Translator
    {
        public event EventHandler<LogMessage>? OnLogEvent;

        public CommandStructure ComS { get; set; }
        public ResponseStructure RspS { get; set; }
        public ResponseStructure AckS { get; set; }
        public ICommSpec CommSpec { get; set; }
        public List<string> AcceptedTargets { get; set; }
        public bool AcceptAnyTarget { get; set; }

        private readonly List<CommandType> IgnoreTargetCommands = [ 
            CommandType.Pod, 
            CommandType.Payload,
            CommandType.StartSim,
            CommandType.StopSim,
            CommandType.PauseSim,
            CommandType.ResumeSim,
            ];

        public Translator(CommandStructure commStruct, ResponseStructure ackStruct, ResponseStructure respStruct, ICommSpec commSpec)
        {
            ComS = commStruct;
            AckS = ackStruct;
            RspS = respStruct;
            CommSpec = commSpec;
            AcceptedTargets = [];
            AcceptAnyTarget = true;
        }

        public Translator(CommandStructure commStruct, ResponseStructure ackStruct, ResponseStructure respStruct, ICommSpec commSpec, List<string> acceptedTargets)
        {
            ComS = commStruct;
            AckS = ackStruct;
            RspS = respStruct;
            CommSpec = commSpec;
            AcceptedTargets = acceptedTargets;
            AcceptAnyTarget = false;
        }

        public Translator(CommandStructure commStruct, ResponseStructure respStruct, ICommSpec commSpec)
        {
            ComS = commStruct;
            RspS = respStruct;
            AckS = new ();

            CommSpec = commSpec;
            AcceptedTargets = [];
            AcceptAnyTarget = true;
        }


        private int GetIndex(CommandArgType value, List<CommandArgType> args)
        {
            int index = args.IndexOf(value);
            if (index == -1)
            {
                OnLogEvent?.Invoke(this, new LogMessage($"Number of arguments in command sent did not match number of arguments expected in Comm. Spec."));
                throw new NackResponse(NackCodes.MissingArguments, $"Translator could not get the index of {value} in the arguments.");
            }
            return index;
        }

        private void DecodeCommands(List<CommandType> commands, string transactionID, string rawAction, string rawCommand, List<Job> runCommands, List<string> recievedValues)
        {
            foreach (CommandType command in commands)
            {
                Job runCommand = new()
                {
                    TransactionID = transactionID,
                    RawAction = rawAction,
                    RawCommand = rawCommand,
                };

                if (!IgnoreTargetCommands.Contains(command))
                {
                    if (!ComS.DedicatedPort)
                        runCommand.Target = recievedValues[ComS.IndexTarget];
                    else
                        runCommand.Target = ComS.FixedTarget ?? "P1";
                }

                // TODO: Reverse Target Mapping

                if (!AcceptAnyTarget && !AcceptedTargets.Any(s => runCommand.Target.StartsWith(s)))
                {
                    OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Target for receieved Command {rawAction} is not in the allowed target list."));
                    throw new NackResponse(NackCodes.CommandError, $"Translator could not find {rawAction} in the allowed target list for {runCommand.Target}.");
                }

                OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Translating command {command}."));
                runCommand.Action = command;

                AddArgumentsToCommand(runCommand, command, recievedValues, rawAction);

                OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Added Command {runCommand.Action} to list (Target = {runCommand.Target})."));
                runCommands.Add(runCommand);
            }
        }

        private void AddArgumentsToCommand(Job job, CommandType command, List<string> receievedValues, string rawAction)
        {
            if (CommandRequirement.CommandDict.TryGetValue(command, out var argList))
            {
                foreach (CommandArgType arg in argList)
                {
                    string prefix = string.Empty;
                    if (ComS.Prefixs.ContainsKey(arg))
                        prefix = ComS.Prefixs[arg];

                    try
                    {
                        int reduce = 0;
                        if (IgnoreTargetCommands.Contains(command))
                            reduce = 1;
                        int index = GetIndex(arg, CommSpec.CommandArgs[rawAction]) - reduce;
                        if (prefix == string.Empty)
                            job.Arguments[(int) arg] = receievedValues[ComS.IndexValueStart + index];
                        else
                            job.Arguments[(int)arg] = receievedValues[ComS.IndexValueStart + index].Replace(prefix, string.Empty);
                    }
                    catch (FormatException) { throw new NackResponse(NackCodes.MissingArguments, $"Translator could not get the argument {command} for {rawAction}."); }
                }
            }
            else
            {
                // Pass
            }
        }
        private static void AddArgumentToList(List<string> response, int index, string value)
        {
            if (index >= 0 && index < response.Count)
                response[index] = value;
        }
        private string ConvertListToString (List<string> response, string delimiter)
        {
            return string.Join(delimiter, response.Where(s => !string.IsNullOrEmpty(s)));
        }
        private string GetCommandStringFromEnum(CommandType commandType)
        {
            foreach (KeyValuePair<string, List<CommandType>> entry in CommSpec.CommandMap)
            {
                if (entry.Value.Contains(commandType))
                {
                    return entry.Key;
                }
            }
            return string.Empty;
        }
        private static string CalculateChecksum(string responseString)
        {
            int sum = 0;

            foreach (char c in responseString)
                sum += (int)c;

            return (sum % 256).ToString("X2");
        }


        public List<Job> TranslateCommandFromString(string commandString)
        {
            int pFrom = 0;
            if (ComS.StartCharacter != null)
                pFrom = commandString.IndexOf(ComS.StartCharacter) + ComS.StartCharacter.Length;

            int pTo = commandString.Length;
            if (ComS.EndCharacter != null)
                pTo = commandString.LastIndexOf(ComS.EndCharacter);

            string rawReceievedCommand = commandString.Substring(pFrom, pTo - pFrom);

            List<string> vals = [.. rawReceievedCommand.Split(ComS.Delimiter.ToCharArray())];

            string transactionID = vals[ComS.IndexTransaction];

            List<string> valsWithoutTxId = new(vals);
            valsWithoutTxId.RemoveAt(ComS.IndexTransaction);
            string rawCommand = string.Join(ComS.Delimiter[0], valsWithoutTxId);

            string rawAction = vals[ComS.IndexCommand].ToUpper();

            OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Receieved Command {rawAction} in string {rawCommand}."));

            if (!CommSpec.CommandMap.ContainsKey(rawAction))
                throw new NackResponse(NackCodes.CommandError, $"Translator could not find action in Comm Spec Commands.");

            if (!CommSpec.CommandArgs.ContainsKey(rawAction))
                throw new NackResponse(NackCodes.CommSpecError, $"Translator could not find action in Comm Spec Arguments.");

            List<CommandType> commands = CommSpec.CommandMap[rawAction];
            List<Job> runCommands = [];

            if (commands.Count != 1 || !IgnoreTargetCommands.Contains(commands[0]))
                if (vals.Count != (CommSpec.CommandArgs[rawAction].Count + ComS.IndexValueStart))
                    throw new NackResponse(NackCodes.CommandError, $"Translator could not match receieved commands against expected Comm Spec commands.");

            OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Commands identified {commands.Count} command(s)."));

            DecodeCommands(commands, transactionID, rawAction, rawCommand, runCommands, vals);

            return runCommands;

            // TODO: Checksum
        }
        public string TranslateCommandToString(Job command)
        {
            List<string> response = ["", "", "", "", "", "", "", "", "", "", ""];

            string actionString = GetCommandStringFromEnum(command.Action);

            AddArgumentToList(response, ComS.IndexTransaction, command.TransactionID);
            AddArgumentToList(response, ComS.IndexCommand, actionString);

            if (CommSpec.StationMapping.ContainsKey(command.Target))
                AddArgumentToList(response, ComS.IndexTarget, CommSpec.StationMapping[command.Target]);
            else
                AddArgumentToList(response, ComS.IndexTarget, command.Target);

            List<CommandArgType> commandArgs;
            try
            {
                commandArgs = CommSpec.CommandArgs[actionString];
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                throw new NackResponse(NackCodes.CommSpecError, $"Translator caught {actionString} is not in Comm Spec.");
            }
            for (int i = 0; i < commandArgs.Count; i++)
            {
                string prefix = string.Empty;
                if (ComS.Prefixs.ContainsKey(commandArgs[i]))
                    prefix = ComS.Prefixs[commandArgs[i]];

                if (commandArgs[i] == CommandArgType.TargetStation && CommSpec.StationMapping.ContainsKey(command.Target))
                {
                    AddArgumentToList(response, ComS.IndexValueStart + i, $"{prefix}{CommSpec.StationMapping[command.Arguments[(int)commandArgs[i]]]}");
                }
                else
                {
                    AddArgumentToList(response, ComS.IndexValueStart + i, $"{prefix}{command.Arguments[(int)commandArgs[i]]}");
                }
            }

            string responseString = ConvertListToString(response, ComS.Delimiter);

            string checksum = string.Empty;
            if (ComS.CheckSum)
            {
                checksum = CalculateChecksum(responseString);
            }

            string returnString = $"{ComS.StartCharacter}{responseString}{ComS.EndCharacter}{checksum}";

            if (ComS.CRLF)
                returnString += "\r\n";

            return returnString;
        }
        public (string, ResponseType, string) TranslateResponseToMessage(string commandString)
        {
            Console.WriteLine(commandString);
            int pFrom = 0;
            if (RspS.StartCharacter != null)
                pFrom = commandString.IndexOf(RspS.StartCharacter) + RspS.StartCharacter.Length;

            int pTo = commandString.Length;
            if (RspS.EndCharacter != null)
                pTo = commandString.LastIndexOf(RspS.EndCharacter);

            string rawCommand = commandString.Substring(pFrom, pTo - pFrom);

            List<string> vals = [.. rawCommand.Split(RspS.Delimiter.ToCharArray())];

            string transactionID = vals[RspS.IndexTransaction];
            string responseTypeString = vals[RspS.IndexMessage];
            ResponseType responseType = CommSpec.ResponseMap.FirstOrDefault(x => x.Value == responseTypeString).Key;

            OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Receieved Command {responseTypeString} in string {rawCommand}."));

            string reply = string.Join(RspS.Delimiter, vals.Skip(RspS.IndexResponseStart));

            return (transactionID, responseType, reply);
        }
        public string TranslateResponseToString(Job command, ResponseType RspType, string message)
        {
            ResponseStructure UseS;

            switch (RspType)
            {
                case ResponseType.Ack:
                case ResponseType.Nack:
                    UseS = AckS;
                    break;
                case ResponseType.Error:
                case ResponseType.Success:
                default:
                    UseS = RspS; 
                    break;
            }

            if (RspType == ResponseType.Ack)
            {
                message = AckS.InjectAckResponse;
            }

            List<string> response = ["", "", "", "", "", "", "", "", "", "", ""];

            AddArgumentToList(response, UseS.IndexTransaction, command.TransactionID);
            AddArgumentToList(response, UseS.IndexMessage, CommSpec.ResponseMap[RspType]);
            AddArgumentToList(response, UseS.IndexTarget, command.Target);
            AddArgumentToList(response, UseS.OriginalCommandIndex, command.RawCommand);
            AddArgumentToList(response, UseS.IndexResponseStart, message);
            string responseString = ConvertListToString(response, UseS.Delimiter);

            string checksum = string.Empty;
            if (UseS.CheckSum)
            {
                checksum = CalculateChecksum(responseString);
            }

            string returnString = $"{UseS.StartCharacter}{responseString}{UseS.EndCharacter}{checksum}";

            if (UseS.CRLF)
                returnString += "\r\n" ;

            return returnString;
        }
        public string TranslateErrorResponse(Job command, ErrorCodes code, string reply)
        {
            return TranslateResponseToString(command, ResponseType.Error, $"{code}: {reply}");
        }
        public string TranslateNackResponse(Job command, NackCodes code, string reply)
        {
            return TranslateResponseToString(command, ResponseType.Nack, $"{code}: {reply}");
        }

    }
}
