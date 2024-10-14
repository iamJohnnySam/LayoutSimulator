using LayoutModels.CommSpecs;
using LayoutModels.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;

namespace LayoutModels
{
    public class Simulator
    {
        public event EventHandler<string>? Response;

        public Translator CommSpec { get; set; }
        public Dictionary<string, Pod> Pods { get; set; } = new Dictionary<string, Pod>();
        public Dictionary<string, Station> Stations { get; set; } = new Dictionary<string, Station>();
        public Dictionary<string, Manipulator> Manipulators { get; set; } = new Dictionary<string, Manipulator>();
        public Dictionary<string, Reader> Readers { get; set; } = new Dictionary<string, Reader>();
        public SimulatorStates State { get; set; } = SimulatorStates.Uninitialized;

        private const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public Simulator(CommandStructure commStruct, ICommSpec commSpec, string xmlPath)
        {
            CommSpec = new Translator(commStruct, commSpec);
            CommSpec.Log += Log;

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
                    Stations[stationName].Log += Simulator_Log;
                    Console.WriteLine(stationName);
                }
            }

            var manipulators = simDoc.Descendants("Manipulator");
            foreach (var manipulator in manipulators)
            {
                string identifier = manipulator.Element("Identifier")?.Value ?? "R";
                List<string> endEffectors_types = (manipulator.Element("EndEffectors")?.Value ?? "payload").Split(',').Select(loc => loc.Trim()).ToList();
                List<string> locations = (manipulator.Element("Locations")?.Value ?? "location").Split(',').Select(loc => loc.Trim()).ToList();
                int motionTime = int.Parse(manipulator.Element("MotionTime")?.Value ?? "0");
                int extendTime = int.Parse(manipulator.Element("ExtendTime")?.Value ?? "0");
                int retractTime = int.Parse(manipulator.Element("RetractTime")?.Value ?? "0");
                int count = int.Parse(manipulator.Element("Count")?.Value ?? "0");
                int port = int.Parse(manipulator.Element("ConnectionPort")?.Value ?? "7000");

                Dictionary<int, Dictionary<string, Payload>> endEffectors = new();
                int endEffector = 1;
                foreach (string payload in endEffectors_types)
                    endEffectors.Add(endEffector++, new Dictionary<string, Payload>());
  
                for (int i = 0; i < count; i++)
                {
                    int j = i+1;
                    string manipulatornName = $"{identifier}{j++}";
                    while (Manipulators.ContainsKey(manipulatornName))
                        manipulatornName = $"{identifier}{j}";
                    Manipulators.Add(manipulatornName, new Manipulator(manipulatornName, endEffectors, locations, motionTime, extendTime, retractTime));
                    Manipulators[manipulatornName].Log += Simulator_Log;
                    Console.WriteLine(manipulatornName);
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
                        Readers.Add(readerName, new Reader(Stations[targetStation], slot));
                    else
                        Readers.Add(readerName, new Reader(Stations[targetStation]));

                    j++;
                    targetStation = $"{stationID}{j}";
                    
                }
                // TODO: Implement sockets
            }

        }

        public void ProcessCommands(string command)
        {
            try
            {
                List<Job> commands = CommSpec.Translate(command);
                new Thread(() => ExecuteCommands(commands)).Start();
            }
            catch (NackResponse e)
            {
                Console.WriteLine(e.ToString());
                // TODO:
            }
            catch (IndexOutOfRangeException e) 
            {
                Console.WriteLine(e.ToString());
                // TODO:
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

        private string GetID(int length)
        {
            StringBuilder val = new StringBuilder(length);
            Random random = new Random();
            for (int i = 0; i < length; i++)
                val.Append(validChars[random.Next(validChars.Length)]);
            return val.ToString();
        }

        private void ExecuteCommands(List<Job> commands)
        {
            foreach (var command in commands) 
            { 
                try
                {
                    switch(command.Action)
                    {
                        case CommandTypes.PICK:
                            CheckManipulatorExist(command.Target);
                            CheckStationExist(command.TargetStation);
                            Manipulators[command.Target].Pick(command.TransactionID, command.EndEffector, Stations[command.TargetStation], command.Slot);
                            break;
                        case CommandTypes.PLACE:
                            CheckManipulatorExist(command.Target);
                            CheckStationExist(command.TargetStation);
                            Manipulators[command.Target].Place(command.TransactionID, command.EndEffector, Stations[command.TargetStation], command.Slot);
                            break;
                        case CommandTypes.DOOR:
                            CheckStationExist(command.Target);
                            Stations[command.Target].Door(command.TransactionID, command.State);
                            break;
                        case CommandTypes.DOOROPEN:
                            CheckStationExist(command.Target);
                            Stations[command.Target].Door(command.TransactionID, false);
                            break;
                        case CommandTypes.DOORCLOSE:
                            CheckStationExist(command.Target);
                            Stations[command.Target].Door(command.TransactionID, true);
                            break;
                        case CommandTypes.MAP:
                            CheckStationExist(command.Target);
                            Stations[command.Target].Map(command.TransactionID);
                            break;
                        case CommandTypes.DOCK:
                            CheckStationExist(command.Target);
                            CheckPodExist(command.PodID);
                            Stations[command.Target].Dock(command.TransactionID, Pods[command.PodID]);
                            Pods.Remove(command.PodID);
                            break;
                        case CommandTypes.UNDOCK:
                            CheckStationExist(command.Target);
                            Pod outgoingPod = Stations[command.Target].UnDock(command.TransactionID);
                            Pods.Add(outgoingPod.PodID, outgoingPod);
                            break;
                        case CommandTypes.PROCESS:
                            CheckStationExist(command.Target);
                            Stations[command.Target].Process(command.TransactionID);
                            break;
                        case CommandTypes.POWER:
                            CheckManipulatorExist(command.Target);
                            if (command.State)
                                Manipulators[command.Target].PowerOn(command.TransactionID);
                            else
                                Manipulators[command.Target].PowerOff(command.TransactionID);
                            break;
                        case CommandTypes.POWERON:
                            Manipulators[command.Target].PowerOn(command.TransactionID);
                            break;
                        case CommandTypes.POWEROFF:
                            Manipulators[command.Target].PowerOff(command.TransactionID);
                            break;
                        case CommandTypes.HOME:
                            CheckManipulatorExist(command.Target);
                            break;
                        case CommandTypes.READ:
                            break;
                        case CommandTypes.POD:
                            string podID = GetID(5);
                            Pods.Add(podID, new Pod(podID, command.Capacity, command.PayloadType));
                            Console.WriteLine(podID);
                            // TODO:
                            break;
                        case CommandTypes.PAYLOAD:
                            CheckPodExist(command.PodID);
                            string payloadID = GetID(5);
                            Pods[command.PodID].slots[command.Slot] = new Payload(payloadID, Pods[command.PodID].PayloadType);
                            Console.WriteLine(payloadID);
                            // TODO:
                            break;
                    }
                }
                catch (NackResponse e)
                {
                    Console.WriteLine(e.ToString());
                    // TODO:
                }
                catch (ErrorResponse e)
                {
                    Console.WriteLine(e.ToString());
                    // TODO:
                }
            }
        }

        private void Log(object? sender, Support.LogMessage e)
        {
            // TODO: Remove console
            Console.WriteLine(e.message);
        }

        private void Simulator_Log(object? sender, LogMessage e)
        {
            // TODO: Remove console
            Console.WriteLine(e.message);
        }
    }
}
