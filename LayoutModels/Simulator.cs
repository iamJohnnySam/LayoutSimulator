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
using System.Numerics;

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

        private SimulatorState simState = SimulatorState.Stopped;
        public SimulatorState State {
            get 
            {
                return simState;
            }
            set
            {
                simState = value;
                OnLogEvent?.Invoke(this, new LogMessage($"Simulator State has changed to {value}."));
            } } 
        // TODO: Simulator States


        private const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";


        public Simulator(string xmlPath)
        {
            XDocument simDoc = XDocument.Load(xmlPath);
            CreateStations (simDoc.Descendants("Station"));
            CreateManipulators (simDoc.Descendants("Manipulator"));
            CreateReaders (simDoc.Descendants("Reader"));
        }

        private void CreateStations(IEnumerable<XElement> stations)
        {
            foreach (var station in stations)
            {
                string identifier = station.Element("Identifier")?.Value ?? "P";
                string payloadType = station.Element("PayloadType")?.Value ?? "payload";
                string inputState = station.Element("InputState")?.Value ?? "unprocessed";
                string outputState = station.Element("OutputState")?.Value ?? "processed";
                int capacity = int.Parse(station.Element("Capacity")?.Value ?? "2");
                List<string> locations = (station.Element("Locations")?.Value ?? "location").Split(',').Select(loc => loc.Trim()).ToList();
                bool processable = station.Element("Processable")?.Value == "1";
                float processTime = float.Parse(station.Element("ProcessTime")?.Value ?? "5");
                bool hasDoor = station.Element("HasDoor")?.Value == "1";
                float doorTransitionTime = float.Parse(station.Element("DoorTransitionTime")?.Value ?? "5");
                bool podDockable = station.Element("PodDockable")?.Value == "1";
                List<string> acceptedCommands = (station.Element("AcceptedCommands")?.Value ?? "").Split(',').Select(loc => loc.Trim()).ToList();
                int count = int.Parse(station.Element("Count")?.Value ?? "1");

                for (int i = 0; i < count; i++)
                {
                    int j = i + 1;
                    string stationName = $"{identifier}{j++}";
                    while (Stations.ContainsKey(stationName))
                        stationName = $"{identifier}{j}";
                    Stations.Add(stationName, new Station(stationName, payloadType, inputState, outputState, capacity, locations, processable, processTime, hasDoor, doorTransitionTime, podDockable, acceptedCommands));
                    Stations[stationName].OnLogEvent += OnSimulatorLogEvent;
                }
            }
        }
        private void CreateManipulators(IEnumerable<XElement> manipulators)
        {
            foreach (var manipulator in manipulators)
            {
                string identifier = manipulator.Element("Identifier")?.Value ?? "R";
                List<string> endEffectorsTypes = (manipulator.Element("EndEffectors")?.Value ?? "payload").Split(',').Select(loc => loc.Trim()).ToList();
                List<string> locations = (manipulator.Element("Locations")?.Value ?? "location").Split(',').Select(loc => loc.Trim()).ToList();
                float motionTime = float.Parse(manipulator.Element("MotionTime")?.Value ?? "0");
                float extendTime = float.Parse(manipulator.Element("ExtendTime")?.Value ?? "0");
                float retractTime = float.Parse(manipulator.Element("RetractTime")?.Value ?? "0");
                int count = int.Parse(manipulator.Element("Count")?.Value ?? "0");

#pragma warning disable IDE0028

                Dictionary<int, Dictionary<string, Payload>> endEffectors = new();
                int endEffector = 1;
                foreach (string payload in endEffectorsTypes)
                    endEffectors.Add(endEffector++, new Dictionary<string, Payload>());

#pragma warning restore IDE0028

                for (int i = 0; i < count; i++)
                {
                    int j = i + 1;
                    string manipulatornName = $"{identifier}{j++}";
                    while (Manipulators.ContainsKey(manipulatornName))
                        manipulatornName = $"{identifier}{j}";
                    Manipulators.Add(manipulatornName, new Manipulator(manipulatornName, endEffectors, endEffectorsTypes, locations, motionTime, extendTime, retractTime));
                    Manipulators[manipulatornName].OnLogEvent += OnSimulatorLogEvent;
                }
            }
        }
        private void CreateReaders(IEnumerable<XElement> readers)
        {
            foreach (var reader in readers)
            {
                string identifier = reader.Element("Identifier")?.Value ?? "B";
                string stationID = reader.Element("StationIdentifier")?.Value ?? "P";
                string type = reader.Element("Type")?.Value ?? "PAYLOAD";
                int slot = int.Parse(reader.Element("Slot")?.Value ?? "1");

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
            }
        }


        private void CheckPodExist(string target)
        {
            if (!Pods.ContainsKey(target))
                throw new NackResponse(NackCodes.TargetNotExist, $"Simulator could not find pod {target}.");
        }
        private void CheckStationExist(string target)
        {
            if (!Stations.ContainsKey(target))
                throw new NackResponse(NackCodes.TargetNotExist, $"Simulator could not find station {target}.");
        }
        private void CheckManipulatorExist(string target)
        {
            if (!Manipulators.ContainsKey(target))
                throw new NackResponse(NackCodes.TargetNotExist, $"Simulator could not find manipulator {target}.");
        }
        private void CheckReaderExist(string target)
        {
            if (!Readers.ContainsKey(target))
                throw new NackResponse(NackCodes.TargetNotExist, $"Simulator could not find reader {target}.");
        }

        public void ConnectPassThrough(string stationName, string ip, int port, CommandStructure commStruct, ResponseStructure respStruct, ICommSpec commSpec)
        {
            if (Stations.Keys.Contains(stationName))
            {
                Stations[stationName].EnablePassthrough(ip, port, commStruct, respStruct, commSpec);
            }
            else if (Manipulators.Keys.Contains(stationName))
            {
                Manipulators[stationName].EnablePassthrough(ip, port, commStruct, respStruct, commSpec);
            }
            else
            {
                Readers[stationName].EnablePassthrough(ip, port, commStruct, respStruct, commSpec);
            }

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
                    throw new NackResponse(NackCodes.CommandError, $"Simulator got a blank command.");
                commands = Translators[commSpecName].TranslateCommandFromString(commandString); 
            }
            catch (IndexOutOfRangeException) 
            {
                OnResponseEvent?.Invoke(this, Translators[commSpecName].TranslateNackResponse(new Job() { 
                    RawCommand = commandString }, 
                    NackCodes.MissingArguments, $"Simulator caught Translator accessing unavailable index."));
                OnLogEvent?.Invoke(this, new LogMessage("0", $"{ResponseType.Nack},{NackCodes.MissingArguments}"));
                return;
            }
            catch(System.ArgumentOutOfRangeException)
            {
                OnResponseEvent?.Invoke(this, Translators[commSpecName].TranslateNackResponse(
                    new Job() { RawCommand = commandString }, 
                    NackCodes.MissingArguments, $"Simulator caught Translator accessing unavailable Argument."));
                OnLogEvent?.Invoke(this, new LogMessage("0", $"{ResponseType.Nack},{NackCodes.MissingArguments}"));
                return;
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                OnResponseEvent?.Invoke(this, Translators[commSpecName].TranslateNackResponse(
                    new Job() { RawCommand = commandString },
                    NackCodes.MissingArguments, $"Simulator caught Translator accessing unavailable Dictionary Key."));
                OnLogEvent?.Invoke(this, new LogMessage("0", $"{ResponseType.Nack},{NackCodes.MissingArguments}"));
                return;
            }
            catch (NackResponse e)
            {
                OnResponseEvent?.Invoke(this, Translators[commSpecName].TranslateNackResponse(
                    new Job() { RawCommand = commandString },
                    e.Code, e.ErrorMessage));
                OnLogEvent?.Invoke(this, new LogMessage("0", $"{ResponseType.Nack},{e.Code}"));
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
                OnResponseEvent?.Invoke(this, Translators[commSpecName].TranslateNackResponse(commands.Last(), e.Code, e.ErrorMessage));
                OnLogEvent?.Invoke(this, new LogMessage(commands.Last().TransactionID, $"{ResponseType.Nack},{e.Code}"));
                return;
            }
            OnResponseEvent?.Invoke(this, Translators[commSpecName].TranslateResponseToString(commands.Last(), ResponseType.Ack, ""));
            OnLogEvent?.Invoke(this, new LogMessage(commands.Last().TransactionID, $"{ResponseType.Ack}"));

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
                OnResponseEvent?.Invoke(this, Translators[commSpecName].TranslateErrorResponse(commands.Last(), e.Code, e.ErrorMessage));
                OnLogEvent?.Invoke(this, new LogMessage(commands.Last().TransactionID, $"{ResponseType.Error},{e.Code}"));
                return;
            }
            OnResponseEvent?.Invoke(this, Translators[commSpecName].TranslateResponseToString(commands.Last(), ResponseType.Success, response));
            OnLogEvent?.Invoke(this, new LogMessage(commands.Last().TransactionID, $"{ResponseType.Success},{response}"));
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
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                OnLogEvent?.Invoke(this, new LogMessage(request.TransactionID, $"{ResponseType.Nack},Key Not Found"));
                gRPCResponse.ResponseType = ResponseType.Nack;
                gRPCResponse.Response = $"{NackCodes.MissingArguments}: Key not found";
                return Task.FromResult(gRPCResponse);
            }
            catch (NackResponse e)
            {
                OnLogEvent?.Invoke(this, new LogMessage(request.TransactionID, $"{ResponseType.Nack},{e.Code}"));
                gRPCResponse.ResponseType = ResponseType.Nack;
                gRPCResponse.Response = $"{e.Code}: {e.ErrorMessage}";
                return Task.FromResult(gRPCResponse);
            }
            catch (ErrorResponse e)
            {
                OnLogEvent?.Invoke(this, new LogMessage(request.TransactionID, $"{ResponseType.Error},{e.Code}"));
                gRPCResponse.ResponseType = ResponseType.Error;
                gRPCResponse.Response = $"{e.Code}: {e.ErrorMessage}";
                return Task.FromResult(gRPCResponse);
            }

            gRPCResponse.ResponseType = ResponseType.Success;
            gRPCResponse.Response = response;

            return Task.FromResult(gRPCResponse);
        }
        private static bool ConvertStringtoBool(string str)
        {
            if (Int32.Parse(str) == 0)
                return false;
            return true;
        }


        private void CheckCommand(Job command, bool commandLock)
        {
            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"Checking {command.Action} for {command.Target}"));

            if (State != SimulatorState.ListeningCommands && command.Action != CommandType.StartSim)
                throw new NackResponse(NackCodes.SimulatorNotStarted, $"Simulator not started.");

            switch (command.Action)
            {
                case CommandType.Pick:
                    CheckManipulatorExist(command.Target);
                    CheckStationExist(command.Arguments[(int) CommandArgType.TargetStation]);

                    if (!Manipulators[command.Target].Power)
                        throw new NackResponse(NackCodes.PowerOff, $"Simulator caught Manipulator {command.Target} power off.");
                    if (Manipulators[command.Target].Busy)
                        throw new NackResponse(NackCodes.Busy, $"Simulator caught Manipulator {command.Target} busy.");
                    if (!Manipulators[command.Target].EndEffectors.ContainsKey(Int32.Parse(command.Arguments[(int)CommandArgType.EndEffector])))
                        throw new NackResponse(NackCodes.EndEffectorMissing, $"Simulator caught Manipulator {command.Target} does not have End Effector.");
                    break;


                case CommandType.Place:
                    CheckManipulatorExist(command.Target);
                    CheckStationExist(command.Arguments[(int)CommandArgType.TargetStation]);

                    if (!Manipulators[command.Target].Power)
                        throw new NackResponse(NackCodes.PowerOff, $"Simulator caught Manipulator {command.Target} power off.");
                    if (Manipulators[command.Target].Busy)
                        throw new NackResponse(NackCodes.Busy, $"Simulator caught Manipulator {command.Target} busy.");
                    if (!Manipulators[command.Target].EndEffectors.ContainsKey(Int32.Parse(command.Arguments[(int)CommandArgType.EndEffector])))
                        throw new NackResponse(NackCodes.EndEffectorMissing, $"Simulator caught Manipulator {command.Target} does not have End Effector.");
                    break;


                case CommandType.Door:
                    CheckStationExist(command.Target);

                    if (!Stations[command.Target].AcceptedCommands.Contains(command.RawAction) && commandLock)
                        throw new NackResponse(NackCodes.CommandError, $"Simulator could not find {command.RawAction} in accepted list of commands for this station.");
                    if (Stations[command.Target].Busy)
                        throw new NackResponse(NackCodes.Busy, $"Simulator caught Station {command.Target} busy.");
                    if (!Stations[command.Target].HasDoor)
                        throw new NackResponse(NackCodes.StationDoesNotHaveDoor, $"Simulator caught Station {command.Target} does not have a door.");
                    break;

                case CommandType.DoorOpen:
                    CheckStationExist(command.Target);

                    if (!Stations[command.Target].AcceptedCommands.Contains(command.RawAction) && commandLock)
                        throw new NackResponse(NackCodes.CommandError, $"Simulator could not find {command.RawAction} in accepted list of commands for this station.");
                    if (Stations[command.Target].Busy)
                        throw new NackResponse(NackCodes.Busy, $"Simulator caught Station {command.Target} busy.");
                    if (!Stations[command.Target].HasDoor)
                        throw new NackResponse(NackCodes.StationDoesNotHaveDoor, $"Simulator caught Station {command.Target} does not have a door.");
                    break;


                case CommandType.DoorClose:
                    CheckStationExist(command.Target);

                    if (!Stations[command.Target].AcceptedCommands.Contains(command.RawAction) && commandLock)
                        throw new NackResponse(NackCodes.CommandError, $"Simulator could not find {command.RawAction} in accepted list of commands for this station.");
                    if (Stations[command.Target].Busy)
                        throw new NackResponse(NackCodes.Busy, $"Simulator caught Station {command.Target} busy.");
                    if (!Stations[command.Target].HasDoor)
                        throw new NackResponse(NackCodes.StationDoesNotHaveDoor, $"Simulator caught Station {command.Target} does not have a door.");
                    break;


                case CommandType.Map:
                    CheckStationExist(command.Target);

                    if (!Stations[command.Target].AcceptedCommands.Contains(command.RawAction) && commandLock)
                        throw new NackResponse(NackCodes.CommandError, $"Simulator could not find {command.RawAction} in accepted list of commands for this station.");
                    if (Stations[command.Target].Busy)
                        throw new NackResponse(NackCodes.Busy, $"Simulator caught Station {command.Target} busy.");
                    if (!Stations[command.Target].Mappable)
                        throw new NackResponse(NackCodes.NotMappable, $"Simulator caught Station {command.Target} not mappable.");
                    break;


                case CommandType.Dock:
                    if (!command.Arguments.ContainsKey((int)CommandArgType.PodId) || (command.Arguments[(int)CommandArgType.PodId] == ""))
                    {
                        command.Arguments[(int)CommandArgType.PodId] = Pods.Keys.Last();
                        OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"Chnaged pod ID to {Pods.Keys.Last()}"));
                    }

                    CheckStationExist(command.Target);
                    CheckPodExist(command.Arguments[(int)CommandArgType.PodId]);

                    if (!Stations[command.Target].AcceptedCommands.Contains(command.RawAction) && commandLock)
                        throw new NackResponse(NackCodes.CommandError, $"Simulator could not find {command.RawAction} in accepted list of commands for this station.");
                    if (Stations[command.Target].Busy)
                        throw new NackResponse(NackCodes.Busy, $"Simulator caught Station {command.Target} busy.");
                    if (!Stations[command.Target].PodDockable)
                        throw new NackResponse(NackCodes.NotDockable, $"Simulator caught Station {command.Target} not dockable.");
                    break;


                case CommandType.Undock:
                    CheckStationExist(command.Target);

                    if (!Stations[command.Target].AcceptedCommands.Contains(command.RawAction) && commandLock)
                        throw new NackResponse(NackCodes.CommandError, $"Simulator could not find {command.RawAction} in accepted list of commands for this station.");
                    if (Stations[command.Target].Busy)
                        throw new NackResponse(NackCodes.Busy, $"Simulator caught Station {command.Target} busy.");
                    if (!Stations[command.Target].PodDockable)
                        throw new NackResponse(NackCodes.NotDockable, $"Simulator caught Station {command.Target} not dockable.");
                    break;

                case CommandType.Process0:
                case CommandType.Process1:
                case CommandType.Process2:
                case CommandType.Process3:
                case CommandType.Process4:
                case CommandType.Process5:
                case CommandType.Process6:
                case CommandType.Process7:
                case CommandType.Process8:
                case CommandType.Process9:
                    CheckStationExist(command.Target);

                    if (!Stations[command.Target].AcceptedCommands.Contains(command.RawAction) && commandLock)
                        throw new NackResponse(NackCodes.CommandError, $"Simulator could not find {command.RawAction} in accepted list of commands for this station.");
                    if (Stations[command.Target].Busy)
                        throw new NackResponse(NackCodes.Busy, $"Simulator caught Station {command.Target} busy.");
                    break;


                case CommandType.Power:
                case CommandType.PowerOn:
                case CommandType.PowerOff:
                    CheckManipulatorExist(command.Target);
                    break;


                case CommandType.Home:
                    if (Manipulators.TryGetValue(command.Target, out Manipulator? manipulator))
                    {
                        if (!manipulator.Power)
                            throw new NackResponse(NackCodes.PowerOff, $"Simulator caught Manipulator {command.Target} power off.");
                        if (manipulator.Busy)
                            throw new NackResponse(NackCodes.Busy, $"Simulator caught Manipulator {command.Target} busy.");
                    }
                    else if (Stations.TryGetValue(command.Target, out Station? station))
                    {
                        if (station.Busy)
                            throw new NackResponse(NackCodes.Busy, $"Simulator caught Station {command.Target} busy.");
                        if (!station.HasDoor)
                            throw new NackResponse(NackCodes.StationDoesNotHaveDoor, $"Simulator caught Station {command.Target} does not have door.");
                    }
                    else
                    {
                        throw new NackResponse(NackCodes.CommandError, $"Simulator could not find station {command.Target}");
                    }
                    break;


                case CommandType.ReadPod:
                case CommandType.ReadSlot:
                    CheckReaderExist(command.Target);
                    break;


                case CommandType.Pod:
                    break;


                case CommandType.Payload:
                    if (!command.Arguments.ContainsKey((int)CommandArgType.PodId) || (command.Arguments[(int)CommandArgType.PodId] == ""))
                    {
                        command.Arguments[(int)CommandArgType.PodId] = Pods.Keys.Last();
                        OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"Chnaged pod ID to {Pods.Keys.Last()}"));
                    }

                    CheckPodExist(command.Arguments[(int)CommandArgType.PodId]);

                    int slot = Int32.Parse(command.Arguments[(int)CommandArgType.Slot]);

                    if (slot < 1)
                        throw new NackResponse(NackCodes.CommandError, $"Simulator cannot create payload in slot less than 1.");

                    while (Pods[command.Arguments[(int)CommandArgType.PodId]].slots.ContainsKey(slot))
                    {
                        slot++;
                        if (slot > Pods[command.Arguments[(int)CommandArgType.PodId]].Capacity)
                            throw new NackResponse(NackCodes.CommandError, $"Simulator cannot create payload in slot greater than station capacity.");
                        
                        command.Arguments[(int)CommandArgType.Slot] = slot.ToString();
                    }
                    break;
            }
        }
        private string ExecuteCommand (Job command, string response)
        {
            while (State == SimulatorState.Paused)
            {
                Thread.Sleep(100);
            }

            if (State == SimulatorState.Paused)
                throw new ErrorResponse(ErrorCodes.SimulatorStopped, $"Simulator was stopped.");

            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"Processing {command.Action} for {command.Target}"));
            switch (command.Action)
            {
                case CommandType.Pick:
                    Manipulators[command.Target].Pick(command, Int32.Parse(command.Arguments[(int)CommandArgType.EndEffector]), Stations[command.Arguments[(int)CommandArgType.TargetStation]], Int32.Parse(command.Arguments[(int)CommandArgType.Slot]));
                    break;

                case CommandType.Place:
                    Manipulators[command.Target].Place(command, Int32.Parse(command.Arguments[(int)CommandArgType.EndEffector]), Stations[command.Arguments[(int)CommandArgType.TargetStation]], Int32.Parse(command.Arguments[(int)CommandArgType.Slot]));
                    break;

                case CommandType.Door:
                    Stations[command.Target].Door(command, ConvertStringtoBool(command.Arguments[(int)CommandArgType.DoorStatus]));
                    break;

                case CommandType.DoorOpen:
                    Stations[command.Target].Door(command, false);
                    break;


                case CommandType.DoorClose:
                    Stations[command.Target].Door(command, true);
                    break;

                case CommandType.Map:
                    List<int> mapData = Stations[command.Target].OpenDoorAndMap(command).Cast<int>().ToList();
                    response = string.Join("", mapData);
                    break;

                case CommandType.Dock:
                    Stations[command.Target].Dock(command, Pods[command.Arguments[(int)CommandArgType.PodId]]);
                    Pods.Remove(command.Arguments[(int)CommandArgType.PodId]);
                    break;

                case CommandType.Undock:
                    Pod outgoingPod = Stations[command.Target].UnDock(command);
                    Pods.Add(outgoingPod.PodID, outgoingPod);
                    break;

                case CommandType.Process0:
                case CommandType.Process1:
                case CommandType.Process2:
                case CommandType.Process3:
                case CommandType.Process4:
                case CommandType.Process5:
                case CommandType.Process6:
                case CommandType.Process7:
                case CommandType.Process8:
                case CommandType.Process9:
                    Stations[command.Target].Process(command);
                    break;

                case CommandType.Power:
                    if (ConvertStringtoBool(command.Arguments[(int)CommandArgType.PowerStatus]))
                        Manipulators[command.Target].PowerOn(command);
                    else
                        Manipulators[command.Target].PowerOff(command);
                    break;

                case CommandType.PowerOn:
                    Manipulators[command.Target].PowerOn(command);
                    break;

                case CommandType.PowerOff:
                    Manipulators[command.Target].PowerOff(command);
                    break;

                case CommandType.Home:
                    if (Manipulators.TryGetValue(command.Target, out Manipulator? manipulator))
                        manipulator.Home(command);
                    else if (Stations.TryGetValue(command.Target, out Station? station))
                        station.Door(command, true);
                    else
                        throw new ErrorResponse(ErrorCodes.ProgramError, $"Simulator could not identify target {command.Target} to home.");
                    break;


                case CommandType.ReadPod:
                case CommandType.ReadSlot:
                    response = Readers[command.Target].ReadID(command.TransactionID);
                    break;

                case CommandType.Pod:
                    string podID = GetID(5);
                    Pods.Add(podID, new Pod(podID, Int32.Parse(command.Arguments[(int)CommandArgType.Capacity]), command.Arguments[(int)CommandArgType.Type]));
                    response = podID;
                    OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"Created Pod {podID} for {command.Arguments[(int)CommandArgType.Type]} with {command.Arguments[(int)CommandArgType.Capacity]} slots."));
                    break;

                case CommandType.Payload:
                    string payloadID = GetID(5);
                    Pods[command.Arguments[(int)CommandArgType.PodId]].slots[Int32.Parse(command.Arguments[(int)CommandArgType.Slot])] = new Payload(payloadID, Pods[command.Arguments[(int)CommandArgType.PodId]].PayloadType, command.Arguments[(int)CommandArgType.PodId], Int32.Parse(command.Arguments[(int)CommandArgType.Slot]));
                    response = payloadID;
                    OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"Created Payload {payloadID} on Pod {command.Arguments[(int)CommandArgType.PodId]} at slot {Int32.Parse(command.Arguments[(int)CommandArgType.Slot])}."));
                    break;

                case CommandType.StartSim:
                case CommandType.ResumeSim:
                    State = SimulatorState.ListeningCommands;
                    break;

                case CommandType.StopSim:
                    State = SimulatorState.Stopped;
                    break;

                case CommandType.PauseSim:
                    State = SimulatorState.Paused;
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
