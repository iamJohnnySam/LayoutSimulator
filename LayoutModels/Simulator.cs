using LayoutModels.CommSpecs;
using LayoutModels.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using LayoutCommands;
using Grpc.Core;
using System.Windows.Input;

namespace LayoutModels
{
    public class Simulator : LayoutSimulator.LayoutSimulatorBase
    {
        public event EventHandler<LogMessage>? OnLogEvent;
        public event EventHandler<string>? OnResponseEvent;

#pragma warning disable IDE0028

        public Dictionary<string, Translator> Translators { get; set; } = new();
        public Dictionary<string, Pod> Pods { get; set; } = new ();
        public Dictionary<string, Station> Stations { get; set; } = new ();
        public Dictionary<string, Manipulator> Manipulators { get; set; } = new ();
        public Dictionary<string, Reader> Readers { get; set; } = new ();

#pragma warning restore IDE0028

        public SimulatorStates State { get; set; } = SimulatorStates.Uninitialized;
        // TODO: Simulator States


        private const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";


        public Simulator(string xmlPath)
        {
            XDocument simDoc = XDocument.Load(xmlPath);

            var stations = simDoc.Descendants("Station");
            foreach (var station in stations)
            {
                string identifier = station.Element("Identifier")?.Value ?? "P";
                string payloadType = station.Element("PayloadType")?.Value ?? "payload";
                string inputState = station.Element("InputState")?.Value ?? "unprocessed";
                string outputState = station.Element("OutputState")?.Value ?? "processed";
                int capacity = int.Parse(station.Element("Capacity")?.Value ?? "2");
                List<string> locations = (station.Element("Locations")?.Value ?? "location").Split(',').Select(loc => loc.Trim()).ToList();
                bool processable = station.Element("Processable")?.Value == "1";
                int processTime = int.Parse(station.Element("ProcessTime")?.Value ?? "5");
                bool hasDoor = station.Element("HasDoor")?.Value == "1";
                int doorTransitionTime = int.Parse(station.Element("DoorTransitionTime")?.Value ?? "5");
                bool podDockable = station.Element("PodDockable")?.Value == "1";
                List<string> acceptedCommands = (station.Element("AcceptedCommands")?.Value ?? "").Split(',').Select(loc => loc.Trim()).ToList();
                int count = int.Parse(station.Element("Count")?.Value ?? "1");
                int port = int.Parse(station.Element("ConnectionPort")?.Value ?? "7000");

                for (int i = 0; i < count; i++)
                {
                    int j = i+1;
                    string stationName = $"{identifier}{j++}";
                    while (Stations.ContainsKey(stationName))
                        stationName = $"{identifier}{j}";
                    Stations.Add(stationName, new Station(stationName, payloadType, inputState, outputState, capacity, locations, processable, processTime, hasDoor, doorTransitionTime, podDockable, acceptedCommands));
                    Stations[stationName].OnLogEvent += OnSimulatorLogEvent;
                }
            }

            var manipulators = simDoc.Descendants("Manipulator");
            foreach (var manipulator in manipulators)
            {
                string identifier = manipulator.Element("Identifier")?.Value ?? "R";
                List<string> endEffectorsTypes = (manipulator.Element("EndEffectors")?.Value ?? "payload").Split(',').Select(loc => loc.Trim()).ToList();
                List<string> locations = (manipulator.Element("Locations")?.Value ?? "location").Split(',').Select(loc => loc.Trim()).ToList();
                int motionTime = int.Parse(manipulator.Element("MotionTime")?.Value ?? "0");
                int extendTime = int.Parse(manipulator.Element("ExtendTime")?.Value ?? "0");
                int retractTime = int.Parse(manipulator.Element("RetractTime")?.Value ?? "0");
                int count = int.Parse(manipulator.Element("Count")?.Value ?? "0");
                int port = int.Parse(manipulator.Element("ConnectionPort")?.Value ?? "7000");

                Dictionary<int, Dictionary<string, Payload>> endEffectors = new();
                int endEffector = 1;
                foreach (string payload in endEffectorsTypes)
                    endEffectors.Add(endEffector++, new Dictionary<string, Payload>());
  
                for (int i = 0; i < count; i++)
                {
                    int j = i+1;
                    string manipulatornName = $"{identifier}{j++}";
                    while (Manipulators.ContainsKey(manipulatornName))
                        manipulatornName = $"{identifier}{j}";
                    Manipulators.Add(manipulatornName, new Manipulator(manipulatornName, endEffectors, endEffectorsTypes, locations, motionTime, extendTime, retractTime));
                    Manipulators[manipulatornName].OnLogEvent += OnSimulatorLogEvent;
                }
            }

            var readers = simDoc.Descendants("Reader");
            foreach (var reader in readers)
            {
                string identifier = reader.Element("Identifier")?.Value ?? "B";
                string stationID = reader.Element("StationIdentifier")?.Value ?? "P";
                string type = reader.Element("Type")?.Value ?? "PAYLOAD";
                int slot = int.Parse(reader.Element("Slot")?.Value ?? "1");
                int port = int.Parse(reader.Element("ConnectionPort")?.Value ?? "7000");

                int j = 1;
                string targetStation = $"{stationID}{j}";

                while (Stations.ContainsKey(targetStation))
                {
                    string readerName = $"{identifier}{j++}";
                    if (type == "PAYLOAD")
                        Readers.Add(readerName, new Reader(readerName, Stations[targetStation], slot));
                    else
                        Readers.Add(readerName, new Reader(readerName, Stations[targetStation]));

                    j++;
                    targetStation = $"{stationID}{j}";
                    
                }
                // TODO: Implement sockets
            }

        }


        private void CheckPodExist(string target)
        {
            if (!Pods.ContainsKey(target))
                throw new NackResponse(NackCodes.TargetNotExist);
        }

        private void CheckStationExist(string target)
        {
            if (!Stations.ContainsKey(target))
                throw new NackResponse(NackCodes.TargetNotExist);
        }

        private void CheckManipulatorExist(string target)
        {
            if (!Manipulators.ContainsKey(target))
                throw new NackResponse(NackCodes.TargetNotExist);
        }

        private void CheckReaderExist(string target)
        {
            if (!Readers.ContainsKey(target))
                throw new NackResponse(NackCodes.TargetNotExist);
        }

        private static string GetID(int length)
        {
            StringBuilder val = new (length);
            for (int i = 0; i < length; i++)
                val.Append(validChars[new Random().Next(validChars.Length)]);
            return val.ToString();
        }

        public void AddCommSpec(string commSpecName, CommandStructure commStruct, ResponseStructure ackStruct, ResponseStructure respStruct, ICommSpec commSpec)
        {
            Translators[commSpecName] = new Translator(commStruct, ackStruct, respStruct, commSpec);
            Translators[commSpecName].OnLogEvent += OnSupportLogEvent;
        }

        public void ExecuteCommands_NewThread(string? command, string commSpecName)
        {
            new Thread(() => ExecuteCommands(command, commSpecName)).Start();
        }

        private void ExecuteCommands(string? commandString, string commSpecName)
        {
            List<Job> commands;
            try 
            {
                if (commandString == null)
                    throw new NackResponse(NackCodes.CommandError);
                commands = Translators[commSpecName].TranslateCommand(commandString); 
            }
            catch (IndexOutOfRangeException) 
            {
                OnResponseEvent?.Invoke(this, Translators[commSpecName].TranslateNackResponse(new Job() { 
                    RawCommand = commandString }, 
                    NackCodes.MissingArguments));
                OnLogEvent?.Invoke(this, new LogMessage("0", $"{ResponseTypes.Nack},{NackCodes.MissingArguments}"));
                return;
            }
            catch(System.ArgumentOutOfRangeException)
            {
                OnResponseEvent?.Invoke(this, Translators[commSpecName].TranslateNackResponse(
                    new Job() { RawCommand = commandString }, 
                    NackCodes.MissingArguments));
                OnLogEvent?.Invoke(this, new LogMessage("0", $"{ResponseTypes.Nack},{NackCodes.MissingArguments}"));
                return;
            }
            catch (NackResponse e)
            {
                OnResponseEvent?.Invoke(this, Translators[commSpecName].TranslateNackResponse(
                    new Job() { RawCommand = commandString },
                    e.Code));
                OnLogEvent?.Invoke(this, new LogMessage("0", $"{ResponseTypes.Nack},{e.Code}"));
                return;
            }

            // ACK COMMAND
            try
            {
                foreach (var command in commands)
                {
                    CheckCommand(command, true);
                }
            }
            catch (NackResponse e)
            {
                OnResponseEvent?.Invoke(this, Translators[commSpecName].TranslateNackResponse(commands.Last(), e.Code));
                OnLogEvent?.Invoke(this, new LogMessage(commands.Last().TransactionID, $"{ResponseTypes.Nack},{e.Code}"));
                return;
            }
            OnResponseEvent?.Invoke(this, Translators[commSpecName].TranslateResponse(commands.Last(), ResponseTypes.Ack, ""));
            OnLogEvent?.Invoke(this, new LogMessage(commands.Last().TransactionID, $"{ResponseTypes.Ack}"));

            string response = string.Empty;
            // RUN COMMAND
            try
            {
                foreach (var command in commands)
                {
                    response = ExecuteCommand(command, response);
                }
            }
            catch (ErrorResponse e)
            {
                OnResponseEvent?.Invoke(this, Translators[commSpecName].TranslateErrorResponse(commands.Last(), e.Code));
                OnLogEvent?.Invoke(this, new LogMessage(commands.Last().TransactionID, $"{ResponseTypes.Error},{e.Code}"));
                return;
            }
            OnResponseEvent?.Invoke(this, Translators[commSpecName].TranslateResponse(commands.Last(), ResponseTypes.Success, response));
            OnLogEvent?.Invoke(this, new LogMessage(commands.Last().TransactionID, $"{ResponseTypes.Success},{response}"));
        }

        public override Task<CommandReply> ExecuteCommand_GRPC(Job request, ServerCallContext context)
        {
            string response = string.Empty;
            CommandReply gRPCResponse = new ();

            try
            {
                CheckCommand(request, false);
                response = ExecuteCommand(request, string.Empty);
            }
            catch (NackResponse e)
            {
                OnLogEvent?.Invoke(this, new LogMessage(request.TransactionID, $"{ResponseTypes.Nack},{e.Code}"));
                gRPCResponse.ResponseType = ResponseTypes.Nack;
                gRPCResponse.Response = e.Code.ToString();
                return Task.FromResult(gRPCResponse);
            }
            catch (ErrorResponse e)
            {
                OnLogEvent?.Invoke(this, new LogMessage(request.TransactionID, $"{ResponseTypes.Error},{e.Code}"));
                gRPCResponse.ResponseType = ResponseTypes.Error;
                gRPCResponse.Response = e.Code.ToString();
                return Task.FromResult(gRPCResponse);
            }

            gRPCResponse.ResponseType = ResponseTypes.Success;
            gRPCResponse.Response = response;

            return Task.FromResult(gRPCResponse);
        }

        private void CheckCommand(Job command, bool commandLock)
        {
            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"Checking {command.Action} for {command.Target}"));
            switch (command.Action)
            {
                case CommandTypes.Pick:
                    CheckManipulatorExist(command.Target);
                    CheckStationExist(command.TargetStation);

                    if (!Manipulators[command.Target].Power)
                        throw new NackResponse(NackCodes.PowerOff);
                    if (Manipulators[command.Target].Busy)
                        throw new NackResponse(NackCodes.Busy);
                    if (!Manipulators[command.Target].EndEffectors.ContainsKey(command.EndEffector))
                        throw new NackResponse(NackCodes.EndEffectorMissing);
                    break;


                case CommandTypes.Place:
                    CheckManipulatorExist(command.Target);
                    CheckStationExist(command.TargetStation);

                    if (!Manipulators[command.Target].Power)
                        throw new NackResponse(NackCodes.PowerOff);
                    if (Manipulators[command.Target].Busy)
                        throw new NackResponse(NackCodes.Busy);
                    if (!Manipulators[command.Target].EndEffectors.ContainsKey(command.EndEffector))
                        throw new NackResponse(NackCodes.EndEffectorMissing);
                    break;


                case CommandTypes.Door:
                    CheckStationExist(command.Target);

                    if (!Stations[command.Target].AcceptedCommands.Contains(command.RawAction) && commandLock)
                        throw new NackResponse(NackCodes.CommandError);
                    if (Stations[command.Target].Busy)
                        throw new NackResponse(NackCodes.Busy);
                    if (!Stations[command.Target].HasDoor)
                        throw new NackResponse(NackCodes.StationDoesNotHaveDoor);
                    break;

                case CommandTypes.DoorOpen:
                    CheckStationExist(command.Target);

                    if (!Stations[command.Target].AcceptedCommands.Contains(command.RawAction) && commandLock)
                        throw new NackResponse(NackCodes.CommandError);
                    if (Stations[command.Target].Busy)
                        throw new NackResponse(NackCodes.Busy);
                    if (!Stations[command.Target].HasDoor)
                        throw new NackResponse(NackCodes.StationDoesNotHaveDoor);
                    break;


                case CommandTypes.DoorClose:
                    CheckStationExist(command.Target);

                    if (!Stations[command.Target].AcceptedCommands.Contains(command.RawAction) && commandLock)
                        throw new NackResponse(NackCodes.CommandError);
                    if (Stations[command.Target].Busy)
                        throw new NackResponse(NackCodes.Busy);
                    if (!Stations[command.Target].HasDoor)
                        throw new NackResponse(NackCodes.StationDoesNotHaveDoor);
                    break;


                case CommandTypes.Map:
                    CheckStationExist(command.Target);

                    if (!Stations[command.Target].AcceptedCommands.Contains(command.RawAction) && commandLock)
                        throw new NackResponse(NackCodes.CommandError);
                    if (Stations[command.Target].Busy)
                        throw new NackResponse(NackCodes.Busy);
                    if (!Stations[command.Target].Mappable)
                        throw new NackResponse(NackCodes.NotMappable);
                    break;


                case CommandTypes.Dock:
                    if(command.PodID.Length == 0)
                        command.PodID = Pods.Keys.Last();

                    CheckStationExist(command.Target);
                    CheckPodExist(command.PodID);

                    if (!Stations[command.Target].AcceptedCommands.Contains(command.RawAction) && commandLock)
                        throw new NackResponse(NackCodes.CommandError);
                    if (Stations[command.Target].Busy)
                        throw new NackResponse(NackCodes.Busy);
                    if (!Stations[command.Target].PodDockable)
                        throw new NackResponse(NackCodes.NotDockable);
                    break;


                case CommandTypes.Undock:
                    CheckStationExist(command.Target);

                    if (!Stations[command.Target].AcceptedCommands.Contains(command.RawAction) && commandLock)
                        throw new NackResponse(NackCodes.CommandError);
                    if (Stations[command.Target].Busy)
                        throw new NackResponse(NackCodes.Busy);
                    if (!Stations[command.Target].PodDockable)
                        throw new NackResponse(NackCodes.NotDockable);
                    break;

                case CommandTypes.Process0:
                case CommandTypes.Process1:
                case CommandTypes.Process2:
                case CommandTypes.Process3:
                case CommandTypes.Process4:
                case CommandTypes.Process5:
                case CommandTypes.Process6:
                case CommandTypes.Process7:
                case CommandTypes.Process8:
                case CommandTypes.Process9:
                    CheckStationExist(command.Target);

                    if (!Stations[command.Target].AcceptedCommands.Contains(command.RawAction) && commandLock)
                        throw new NackResponse(NackCodes.CommandError);
                    if (Stations[command.Target].Busy)
                        throw new NackResponse(NackCodes.Busy);
                    break;


                case CommandTypes.Power:
                case CommandTypes.PowerOn:
                case CommandTypes.PowerOff:
                    CheckManipulatorExist(command.Target);
                    break;


                case CommandTypes.Home:
                    if (Manipulators.ContainsKey(command.Target))
                    {
                        if (!Manipulators[command.Target].Power)
                            throw new NackResponse(NackCodes.PowerOff);
                        if (Manipulators[command.Target].Busy)
                            throw new NackResponse(NackCodes.Busy);
                    }
                    else if (Stations.ContainsKey(command.Target))
                    {
                        if (Stations[command.Target].Busy)
                            throw new NackResponse(NackCodes.Busy);
                        if (!Stations[command.Target].HasDoor)
                            throw new NackResponse(NackCodes.StationDoesNotHaveDoor);
                    }
                    else
                    {
                        throw new NackResponse(NackCodes.CommandError);
                    }
                    break;


                case CommandTypes.ReadPod:
                case CommandTypes.ReadSlot:
                    CheckReaderExist(command.Target);
                    break;


                case CommandTypes.Pod:
                    break;


                case CommandTypes.Payload:
                    if (command.PodID.Length == 0) 
                        command.PodID = Pods.Keys.Last();

                    CheckPodExist(command.PodID);

                    if (command.Slot < 1)
                        throw new NackResponse(NackCodes.CommandError);

                    while (Pods[command.PodID].slots.ContainsKey(command.Slot))
                    {
                        command.Slot++;
                        if (command.Slot > Pods[command.PodID].Capacity)
                            throw new NackResponse(NackCodes.CommandError);
                    }
                    break;
            }
        }

        private string ExecuteCommand (Job command, string response)
        {
            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"Processing {command.Action} for {command.Target}"));
            switch (command.Action)
            {
                case CommandTypes.Pick:
                    Manipulators[command.Target].Pick(command.TransactionID, command.EndEffector, Stations[command.TargetStation], command.Slot);
                    break;

                case CommandTypes.Place:
                    Manipulators[command.Target].Place(command.TransactionID, command.EndEffector, Stations[command.TargetStation], command.Slot);
                    break;

                case CommandTypes.Door:
                    Stations[command.Target].Door(command.TransactionID, command.State);
                    break;

                case CommandTypes.DoorOpen:
                    Stations[command.Target].Door(command.TransactionID, false);
                    break;


                case CommandTypes.DoorClose:
                    Stations[command.Target].Door(command.TransactionID, true);
                    break;

                case CommandTypes.Map:
                    List<int> mapData = Stations[command.Target].OpenDoorAndMap(command.TransactionID).Cast<int>().ToList();
                    response = string.Join("", mapData);
                    break;

                case CommandTypes.Dock:
                    Stations[command.Target].Dock(command.TransactionID, Pods[command.PodID]);
                    Pods.Remove(command.PodID);
                    break;

                case CommandTypes.Undock:
                    Pod outgoingPod = Stations[command.Target].UnDock(command.TransactionID);
                    Pods.Add(outgoingPod.PodID, outgoingPod);
                    break;

                case CommandTypes.Process0:
                case CommandTypes.Process1:
                case CommandTypes.Process2:
                case CommandTypes.Process3:
                case CommandTypes.Process4:
                case CommandTypes.Process5:
                case CommandTypes.Process6:
                case CommandTypes.Process7:
                case CommandTypes.Process8:
                case CommandTypes.Process9:
                    Stations[command.Target].Process(command.TransactionID);
                    break;

                case CommandTypes.Power:
                    if (command.State)
                        Manipulators[command.Target].PowerOn(command.TransactionID);
                    else
                        Manipulators[command.Target].PowerOff(command.TransactionID);
                    break;

                case CommandTypes.PowerOn:
                    Manipulators[command.Target].PowerOn(command.TransactionID);
                    break;

                case CommandTypes.PowerOff:
                    Manipulators[command.Target].PowerOff(command.TransactionID);
                    break;

                case CommandTypes.Home:
                    if (Manipulators.TryGetValue(command.Target, out Manipulator? manipulator))
                        manipulator.Home(command.TransactionID);
                    else if (Stations.TryGetValue(command.Target, out Station? station))
                        station.Door(command.TransactionID, true);
                    else
                        throw new ErrorResponse(ErrorCodes.ProgramError);
                    break;


                case CommandTypes.ReadPod:
                case CommandTypes.ReadSlot:
                    response = Readers[command.Target].ReadID(command.TransactionID);
                    break;

                case CommandTypes.Pod:
                    string podID = GetID(5);
                    Pods.Add(podID, new Pod(podID, command.Capacity, command.PayloadType));
                    response = podID;
                    OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"Created Pod {podID}."));
                    break;

                case CommandTypes.Payload:
                    string payloadID = GetID(5);
                    Pods[command.PodID].slots[command.Slot] = new Payload(payloadID, Pods[command.PodID].PayloadType);
                    response = payloadID;
                    OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"Created Payload {payloadID} on Pod {command.PodID} at slot {command.Slot}."));
                    break;
            }
            return response;
        }

        private void OnSupportLogEvent(object? sender, Support.LogMessage e)
        {
            OnLogEvent?.Invoke(this, e);
        }

        private void OnSimulatorLogEvent(object? sender, LogMessage e)
        {
            OnLogEvent?.Invoke(this, e);
        }
    }
}
