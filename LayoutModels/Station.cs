using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels
{
    internal class Station
    {
        public event EventHandler<string>? Log;

        public Dictionary<string, Payload> slots = new Dictionary<string, Payload>();

        public string StationID { get; private set; }
        public string PayloadType { get; private set; }
        public string InputState { get; set; }
        public string OutputState { get; set; }
        public int Capacity { get; private set; }
        public string Location { get; private set; }
        public bool Processable { get; private set; }
        public int ProcessTime { get; set; }
        public bool HasDoor { get; private set; }
        public int DoorTransitionTime { get; private set; }
        
        private bool statusDoorOpen = false;
        public bool StatusDoorOpen {
            get { return statusDoorOpen; }
            private set { 
                statusDoorOpen = value;
                Log?.Invoke(this, $"Station {StationID} Door Open Status updated to {value}");
            }
        }

        private bool statusDoorClosed = true;
        public bool StatusDoorClosed {
            get { return statusDoorClosed; }
            private set
            {
                statusDoorClosed = value;
                Log?.Invoke(this, $"Station {StationID} Door Closed Status updated to {value}");
            }
        }

        private bool statusDoorOpening = false;
        public bool StatusDoorOpening {
            get { return statusDoorOpening; }
            private set
            {
                statusDoorOpening = value;
                Log?.Invoke(this, $"Station {StationID} Door Opening Status updated to {value}");
            }
        }

        private bool statusDoorClosing = false;
        public bool StatusDoorClosing {
            get { return statusDoorClosing; }
            private set
            {
                statusDoorClosing = value;
                Log?.Invoke(this, $"Station {StationID} Door Closing Status updated to {value}");
            }
        }

        private bool statusBeingAccessed = false;
        public bool StatusBeingAccessed {
            get { return statusBeingAccessed; }
            private set { 
                statusBeingAccessed = value;
                Log?.Invoke(this, $"Station {StationID} Being Accessed Status updated to {value}");
            }
        }
        public bool PodDockable { get; private set; }
        
        private bool statusPodDocked = false;
        public bool StatusPodDocked {
            get { return statusPodDocked; }
            private set
            {
                statusPodDocked = value;
                Log?.Invoke(this, $"Station {StationID} Pod Docked Status updated to {value}");
            }
        }
        
        private string? podID = null;
        public string PodID {
            get { return PodID; }
            private set
            {
                podID = value;
                Log?.Invoke(this, $"Station {StationID} Pod ID updated to {value}");
            }
        }
        public bool Busy { get; private set; } = false;

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
            statusDoorClosed = hasDoor;
            statusDoorOpen = !hasDoor;
            DoorTransitionTime = doorTransitionTime;

            // Pod
            PodDockable = podDockable;

        }

        public void AcceptPayload(Payload payload, int slot)
        {
            // todo
        }

    }
}
