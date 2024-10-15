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
using System.Xml;
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;

namespace LayoutModels
{
    public class Simulator
    {
        public event EventHandler<LogMessage>? OnLogEvent;
        public event EventHandler<string>? OnResponseEvent;

        public Translator CommSpec { get; set; }
        public Dictionary<string, Pod> Pods { get; set; } = new ();
        public Dictionary<string, Station> Stations { get; set; } = new ();
        public Dictionary<string, Manipulator> Manipulators { get; set; } = new ();
        public Dictionary<string, Reader> Readers { get; set; } = new ();
        public SimulatorStates State { get; set; } = SimulatorStates.Uninitialized;
        // TODO: Simulator States


        private const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";


        public Simulator(CommandStructure commStruct, ResponseStructure respStruct, ICommSpec commSpec, string xmlPath)
        {
            CommSpec = new Translator(commStruct, respStruct, commSpec);
            CommSpec.OnLogEvent += OnSupportLogEvent;

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
                int count = int.Parse(station.Element("Count")?.Value ?? "1");
                int port = int.Parse(station.Element("ConnectionPort")?.Value ?? "7000");

                for (int i = 0; i < count; i++)
                {
                    int j = i+1;
                    string stationName = $"{identifier}{j++}";
                    while (Stations.ContainsKey(stationName))
                        stationName = $"{identifier}{j}";
                    Stations.Add(stationName, new Station(stationName, payloadType, inputState, outputState, capacity, locations, processable, processTime, hasDoor, doorTransitionTime, podDockable));
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

        public void ProcessCommands(string command)
        {
            new Thread(() => ExecuteCommands(command)).Start();
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

        private string GetID(int length)
        {
            StringBuilder val = new StringBuilder(length);
            Random random = new Random();
            for (int i = 0; i < length; i++)
                val.Append(validChars[random.Next(validChars.Length)]);
            return val.ToString();
        }

        private void ExecuteCommands(string _command)
        {
            List<Job> commands = new();
            try { commands = CommSpec.TranslateCommand(_command); }
            catch (IndexOutOfRangeException) 
            { 
                OnResponseEvent?.Invoke(this, CommSpec.TranslateNackResponse("0", "", NackCodes.MissingArguments));
                OnLogEvent?.Invoke(this, new LogMessage("0", $"{ResponseTypes.NACK},{NackCodes.MissingArguments}"));
                return; 
            }
            catch(System.ArgumentOutOfRangeException)
            {
                OnResponseEvent?.Invoke(this, CommSpec.TranslateNackResponse("0", "", NackCodes.MissingArguments));
                OnLogEvent?.Invoke(this, new LogMessage("0", $"{ResponseTypes.NACK},{NackCodes.MissingArguments}"));
                return;
            }
            catch (NackResponse e)
            { 
                OnResponseEvent?.Invoke(this, CommSpec.TranslateNackResponse("0", "", e.Code));
                OnLogEvent?.Invoke(this, new LogMessage("0", $"{ResponseTypes.NACK},{e.Code}"));
                return; 
            }

            string response = string.Empty;

            foreach (var command in commands) 
            {
                string _podID = string.Empty;
                try
                {
                    switch(command.Action)
                    {
                        case CommandTypes.PICK:
                            CheckManipulatorExist(command.Target);
                            CheckStationExist(command.TargetStation);

                            if (!Manipulators[command.Target].Power)
                                throw new NackResponse(NackCodes.PowerOff);
                            if (Manipulators[command.Target].Busy)
                                throw new NackResponse(NackCodes.Busy);
                            if (!Manipulators[command.Target].EndEffectors.ContainsKey(command.EndEffector))
                                throw new NackResponse(NackCodes.EndEffectorMissing);
                            OnResponseEvent?.Invoke(this, CommSpec.TranslateResponse(command.TransactionID, ResponseTypes.ACK, command.Target, ""));
                            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"{ResponseTypes.ACK}"));

                            Manipulators[command.Target].Pick(command.TransactionID, command.EndEffector, Stations[command.TargetStation], command.Slot);
                            break;


                        case CommandTypes.PLACE:
                            CheckManipulatorExist(command.Target);
                            CheckStationExist(command.TargetStation);

                            if (!Manipulators[command.Target].Power)
                                throw new NackResponse(NackCodes.PowerOff);
                            if (Manipulators[command.Target].Busy)
                                throw new NackResponse(NackCodes.Busy);
                            if (!Manipulators[command.Target].EndEffectors.ContainsKey(command.EndEffector))
                                throw new NackResponse(NackCodes.EndEffectorMissing);
                            OnResponseEvent?.Invoke(this, CommSpec.TranslateResponse(command.TransactionID, ResponseTypes.ACK, command.Target, ""));
                            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"{ResponseTypes.ACK}"));

                            Manipulators[command.Target].Place(command.TransactionID, command.EndEffector, Stations[command.TargetStation], command.Slot);
                            break;


                        case CommandTypes.DOOR:
                            CheckStationExist(command.Target);

                            if (Stations[command.Target].Busy)
                                throw new NackResponse(NackCodes.Busy);
                            if (!Stations[command.Target].HasDoor)
                                throw new NackResponse(NackCodes.StationDoesNotHaveDoor);
                            OnResponseEvent?.Invoke(this, CommSpec.TranslateResponse(command.TransactionID, ResponseTypes.ACK, command.Target, ""));
                            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"{ResponseTypes.ACK}"));

                            Stations[command.Target].Door(command.TransactionID, command.State);
                            break;

                        case CommandTypes.DOOROPEN:
                            CheckStationExist(command.Target);

                            if (Stations[command.Target].Busy)
                                throw new NackResponse(NackCodes.Busy);
                            if (!Stations[command.Target].HasDoor)
                                throw new NackResponse(NackCodes.StationDoesNotHaveDoor);
                            OnResponseEvent?.Invoke(this, CommSpec.TranslateResponse(command.TransactionID, ResponseTypes.ACK, command.Target, ""));
                            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"{ResponseTypes.ACK}"));

                            Stations[command.Target].Door(command.TransactionID, false);
                            break;


                        case CommandTypes.DOORCLOSE:
                            CheckStationExist(command.Target);

                            if (Stations[command.Target].Busy)
                                throw new NackResponse(NackCodes.Busy);
                            if (!Stations[command.Target].HasDoor)
                                throw new NackResponse(NackCodes.StationDoesNotHaveDoor);
                            OnResponseEvent?.Invoke(this, CommSpec.TranslateResponse(command.TransactionID, ResponseTypes.ACK, command.Target, ""));
                            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"{ResponseTypes.ACK}"));

                            Stations[command.Target].Door(command.TransactionID, true);
                            break;


                        case CommandTypes.MAP:
                            CheckStationExist(command.Target);

                            if (Stations[command.Target].Busy)
                                throw new NackResponse(NackCodes.Busy);
                            if (!Stations[command.Target].Mappable)
                                throw new NackResponse(NackCodes.NotMappable);
                            OnResponseEvent?.Invoke(this, CommSpec.TranslateResponse(command.TransactionID, ResponseTypes.ACK, command.Target, ""));
                            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"{ResponseTypes.ACK}"));

                            List<int> mapData = Stations[command.Target].OpenDoorAndMap(command.TransactionID).Cast<int>().ToList();
                            response = string.Join("", mapData);
                            break;


                        case CommandTypes.REMAP:
                            CheckStationExist(command.Target);

                            if (Stations[command.Target].Busy)
                                throw new NackResponse(NackCodes.Busy);
                            if (!Stations[command.Target].Mappable)
                                throw new NackResponse(NackCodes.NotMappable);
                            OnResponseEvent?.Invoke(this, CommSpec.TranslateResponse(command.TransactionID, ResponseTypes.ACK, command.Target, ""));
                            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"{ResponseTypes.ACK}"));

                            List<int> mapData1 = Stations[command.Target].ReMap(command.TransactionID).Cast<int>().ToList();
                            response = string.Join("", mapData1);
                            break;


                        case CommandTypes.DOCK:
                            if (command.PodID.Length > 0) _podID = command.PodID;
                            else _podID = Pods.Keys.Last();

                            CheckStationExist(command.Target);
                            CheckPodExist(_podID);

                            if (Stations[command.Target].Busy)
                                throw new NackResponse(NackCodes.Busy);
                            if (!Stations[command.Target].PodDockable)
                                throw new NackResponse(NackCodes.NotDockable);
                            OnResponseEvent?.Invoke(this, CommSpec.TranslateResponse(command.TransactionID, ResponseTypes.ACK, command.Target, ""));
                            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"{ResponseTypes.ACK}"));

                            Stations[command.Target].Dock(command.TransactionID, Pods[_podID]);
                            Pods.Remove(command.PodID);
                            break;


                        case CommandTypes.UNDOCK:
                            CheckStationExist(command.Target);

                            if (Stations[command.Target].Busy)
                                throw new NackResponse(NackCodes.Busy);
                            if (!Stations[command.Target].PodDockable)
                                throw new NackResponse(NackCodes.NotDockable);
                            OnResponseEvent?.Invoke(this, CommSpec.TranslateResponse(command.TransactionID, ResponseTypes.ACK, command.Target, ""));
                            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"{ResponseTypes.ACK}"));

                            Pod outgoingPod = Stations[command.Target].UnDock(command.TransactionID);
                            Pods.Add(outgoingPod.PodID, outgoingPod);
                            break;

                        case CommandTypes.PROCESS0:
                        case CommandTypes.PROCESS1:
                        case CommandTypes.PROCESS2:
                        case CommandTypes.PROCESS3:
                        case CommandTypes.PROCESS4:
                        case CommandTypes.PROCESS5:
                        case CommandTypes.PROCESS6:
                        case CommandTypes.PROCESS7:
                        case CommandTypes.PROCESS8:
                        case CommandTypes.PROCESS9:
                            CheckStationExist(command.Target);

                            if (Stations[command.Target].Busy)
                                throw new NackResponse(NackCodes.Busy);
                            OnResponseEvent?.Invoke(this, CommSpec.TranslateResponse(command.TransactionID, ResponseTypes.ACK, command.Target, ""));
                            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"{ResponseTypes.ACK}"));

                            Stations[command.Target].Process(command.TransactionID);
                            break;


                        case CommandTypes.POWER:
                            CheckManipulatorExist(command.Target);
                            OnResponseEvent?.Invoke(this, CommSpec.TranslateResponse(command.TransactionID, ResponseTypes.ACK, command.Target, ""));
                            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"{ResponseTypes.ACK}"));

                            if (command.State)
                                Manipulators[command.Target].PowerOn(command.TransactionID);
                            else
                                Manipulators[command.Target].PowerOff(command.TransactionID);
                            break;


                        case CommandTypes.POWERON:
                            CheckManipulatorExist(command.Target);
                            OnResponseEvent?.Invoke(this, CommSpec.TranslateResponse(command.TransactionID, ResponseTypes.ACK, command.Target, ""));
                            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"{ResponseTypes.ACK}"));

                            Manipulators[command.Target].PowerOn(command.TransactionID);
                            break;


                        case CommandTypes.POWEROFF:
                            CheckManipulatorExist(command.Target);
                            OnResponseEvent?.Invoke(this, CommSpec.TranslateResponse(command.TransactionID, ResponseTypes.ACK, command.Target, ""));
                            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"{ResponseTypes.ACK}"));

                            Manipulators[command.Target].PowerOff(command.TransactionID);
                            break;


                        case CommandTypes.HOME:
                            CheckManipulatorExist(command.Target);
                            if (!Manipulators[command.Target].Power)
                                throw new NackResponse(NackCodes.PowerOff);
                            if (Manipulators[command.Target].Busy)
                                throw new NackResponse(NackCodes.Busy);
                            OnResponseEvent?.Invoke(this, CommSpec.TranslateResponse(command.TransactionID, ResponseTypes.ACK, command.Target, ""));
                            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"{ResponseTypes.ACK}"));

                            Manipulators[command.Target].Home(command.TransactionID);
                            break;


                        case CommandTypes.READPOD:
                        case CommandTypes.READSLOT:
                            CheckReaderExist(command.Target);
                            OnResponseEvent?.Invoke(this, CommSpec.TranslateResponse(command.TransactionID, ResponseTypes.ACK, command.Target, ""));
                            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"{ResponseTypes.ACK}"));

                            response = Readers[command.Target].ReadID(command.TransactionID);
                            break;


                        case CommandTypes.POD:
                            OnResponseEvent?.Invoke(this, CommSpec.TranslateResponse(command.TransactionID, ResponseTypes.ACK, command.Target, ""));
                            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"{ResponseTypes.ACK}"));

                            string podID = GetID(5);
                            Pods.Add(podID, new Pod(podID, command.Capacity, command.PayloadType));
                            response = podID;
                            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"Created Pod {podID}."));
                            break;


                        case CommandTypes.PAYLOAD:
                            if (command.PodID.Length > 0) _podID = command.PodID;
                            else _podID = Pods.Keys.Last();
                            CheckPodExist(_podID);
                            if (command.Slot < 1)
                                throw new NackResponse(NackCodes.CommandError);
                            int add_payload_slot = command.Slot;
                            while (Pods[_podID].slots.ContainsKey(add_payload_slot))
                            {
                                add_payload_slot++;
                                if (add_payload_slot > Pods[_podID].Capacity)
                                    throw new NackResponse(NackCodes.CommandError);
                            } 
                            OnResponseEvent?.Invoke(this, CommSpec.TranslateResponse(command.TransactionID, ResponseTypes.ACK, command.Target, ""));
                            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"{ResponseTypes.ACK}"));

                            string payloadID = GetID(5);
                            Pods[_podID].slots[add_payload_slot] = new Payload(payloadID, Pods[_podID].PayloadType);
                            response = payloadID;
                            OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"Created Payload {payloadID} on Pod {_podID} at slot {add_payload_slot}."));
                            break;
                    }
                }
                catch (NackResponse e)
                {
                    OnResponseEvent?.Invoke(this, CommSpec.TranslateNackResponse(command.TransactionID, command.Target, e.Code));
                    OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"{ResponseTypes.NACK},{e.Code}"));
                    return;
                }
                catch (ErrorResponse e)
                {
                    OnResponseEvent?.Invoke(this, CommSpec.TranslateErrorResponse(command.TransactionID, command.Target, e.Code));
                    OnLogEvent?.Invoke(this, new LogMessage(command.TransactionID, $"{ResponseTypes.ERROR},{e.Code}"));
                    return;
                }
            }
            OnResponseEvent?.Invoke(this, CommSpec.TranslateResponse(commands.Last().TransactionID, ResponseTypes.SUCCESS, commands.Last().Target, response));
            OnLogEvent?.Invoke(this, new LogMessage(commands.Last().TransactionID, $"{ResponseTypes.SUCCESS},{response}"));
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
