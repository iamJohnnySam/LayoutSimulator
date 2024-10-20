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
            { CommandType.Sdock, new List<CommandArgType>() { CommandArgType.PodID } },
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
            { CommandType.Payload, new List<CommandArgType>() { CommandArgType.PodID, CommandArgType.Slot } }
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
            AcceptedTargets = new ();
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


        private int GetIndex(CommandArgType value, List<CommandArgType> args)
        {
            int index = args.IndexOf(value);
            if (index == -1)
            {
                OnLogEvent?.Invoke(this, new LogMessage($"Number of arguments in command sent did not match number of arguments expected in Comm. Spec."));
                throw new NackResponse(NackCodes.MissingArguments);
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

                if (!AcceptAnyTarget && !AcceptedTargets.Any(s => runCommand.Target.StartsWith(s)))
                {
                    OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Target for receieved Command {rawAction} is not in the allowed target list."));
                    throw new NackResponse(NackCodes.CommandError);
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
                foreach (var arg in argList)
                {
                    try
                    {
                        int index = GetIndex(arg, CommSpec.CommandArgs[rawAction]);

                        switch (arg)
                        {
                            case CommandArgType.EndEffector:
                                job.EndEffector = Int32.Parse(receievedValues[ComS.IndexValueStart + index]);
                                break;

                            case CommandArgType.TargetStation:
                                job.TargetStation = receievedValues[ComS.IndexValueStart + index];
                                break;

                            case CommandArgType.Slot:
                                job.Slot = Int32.Parse(receievedValues[ComS.IndexValueStart + index]);
                                break;

                            case CommandArgType.PodID:
                                job.PodID = receievedValues[ComS.IndexValueStart + index];
                                break;

                            case CommandArgType.DoorStatus:
                            case CommandArgType.PowerStatus:
                                if (receievedValues[ComS.IndexValueStart + index] == "1")
                                    job.State = true;
                                else
                                    job.State = false;
                                break;

                            case CommandArgType.Capacity:
                                job.Capacity = Int32.Parse(receievedValues[ComS.IndexValueStart + index]);
                                break;

                            case CommandArgType.Type:
                                job.PayloadType = receievedValues[ComS.IndexValueStart + index];
                                break;

                            case CommandArgType.Ignore:
                                // Do nothing
                                break;
                        }
                    }
                    catch (FormatException) { throw new NackResponse(NackCodes.MissingArguments); }
                }
            }
            else
            {
                // Pass
            }
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

            List<CommandType> commands = CommSpec.CommandMap[rawAction];
            List<Job> runCommands = new ();

            if (commands.Count != 1 || !IgnoreTargetCommands.Contains(commands[0]))
                if (vals.Count != (CommSpec.CommandArgs[rawAction].Count + ComS.IndexValueStart))
                    throw new NackResponse(NackCodes.CommandError);

            OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Commands identified {commands.Count} command(s)."));

            DecodeCommands(commands, transactionID, rawAction, rawCommand, runCommands, vals);

            return runCommands;

            // TODO: Checksum
        }

        public string TranslateResponse(Job command, ResponseTypes RspType, string message)
        {
            ResponseStructure UseS = AckS;

            switch (RspType)
            {
                case ResponseTypes.Ack:
                case ResponseTypes.Nack:
                    UseS = AckS; break;
                case ResponseTypes.Error:
                case ResponseTypes.Success:
                    UseS = RspS; break;
            }

            if (RspType == ResponseTypes.Ack)
            {
                message = AckS.InjectAckResponse;
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
            return TranslateResponse(command, ResponseTypes.Error, code.ToString());
        }

        public string TranslateNackResponse(Job command, NackCodes code)
        {
            return TranslateResponse(command, ResponseTypes.Nack, code.ToString());
        }

    }
}
