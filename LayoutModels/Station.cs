using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels
{
    internal class Station
    {

        public Station(string stationID, string payloadType, string inputState, string outputState, int capacity, string location, bool processable, int processTime, bool hasDoor, int doorTransitionTime, bool podDockable)
        {
            // Station Details
            StationID = stationID;
            PayloadType = payloadType;

            // Payload Handling
            InputState = inputState;
            OutputState = outputState;
            Capacity = capacity;

            // Processing
            Processable = processable;
            ProcessTime = processTime;

            // Accessibility
            Location = location;
            HasDoor = hasDoor;
            DoorTransitionTime = doorTransitionTime;

            // Pod
            PodDockable = podDockable;
        }

        public string StationID { get; set; }
        public string PayloadType { get; set; }
        public string InputState { get; set; }
        public string OutputState { get; set; }
        public int Capacity { get; private set; }
        public string Location { get; private set; }
        public bool Processable { get; private set; }
        public int ProcessTime { get; set; }
        public bool HasDoor { get; private set; }
        public int DoorTransitionTime { get; private set; }
        public bool StateDoorOpen { get; set; }
        public bool StateDoorClosed { get; set; }
        public bool DoorOpening { get; set; }
        public bool PodDockable { get; private set; }

    }
}
