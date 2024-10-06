using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels
{
    internal class Station: ITarget
    {
        public event EventHandler<string>? Log;

        public Dictionary<int, Payload> slots = new Dictionary<int, Payload>();

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

        private bool busy = false;
        public bool Busy {
            get { return busy; }
            private set
            {
                busy = value;
                Log?.Invoke(this, $"Station {StationID} Busy Status updated to {value}");
            }
        }

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

        private bool CheckAcessible()
        {
            if (StatusBeingAccessed) return false;
            else if (HasDoor && !StatusDoorOpen) return false;
            else return true;
        }

        private bool CheckPayloadCompatible(Payload payload)
        {
            if (payload.PayloadType != PayloadType)
                return false;
            return true;
        }

        private bool CheckSlotEmpty(int slot)
        {
            if (slots.ContainsKey(slot))
                return false;
            return true;
        }

        private int GetNextEmptySlot()
        {
            for (int i = 0; i < Capacity; i++) {
                int slot = i + 1;
                if (slots.ContainsKey(slot))
                {
                    Log?.Invoke(this, $"Station {StationID} Next empty slot ({slot}) was updated.");
                    return slot;
                }
            }
            return Capacity;
        }

        private int GetNextAvailableSlot()
        {
            for (int i = 0; i < Capacity; i++)
            {
                int slot = i + 1;
                if (!slots.ContainsKey(slot))
                {
                    Log?.Invoke(this, $"Station {StationID} Next available slot ({slot}) was updated.");
                    return slot;
                }
            }
            return 1;
        }

        public void Dock(Pod pod)
        {
            // TODO: Implement
        }

        public Pod UnDock()
        {
            // TODO: Implement
        }

        public void AcceptPayload(Payload payload, int slot)
        {
            if (!CheckAcessible())
                throw new ErrorResponse($"Station {StationID} is not accessible.");
    

            if (!CheckPayloadCompatible(payload))
                throw new ErrorResponse($"Payload {payload.PayloadID} is not compatible with Station {StationID}.");

            if (slot == 0)
                slot = GetNextEmptySlot();

            if (!CheckSlotEmpty(slot))
                throw new ErrorResponse($"Slot {slot} is not empty on Station {StationID}.");

            slots.Add(slot, payload);
            Log?.Invoke(this, $"Payload {payload.PayloadID} added to slot {slot} on Station {StationID}.");
        }

        public Payload ReleasePayload(int slot)
        {
            if (!CheckAcessible())
                throw new ErrorResponse($"Station {StationID} is not accessible.");

            if (slot == 0)
                slot = GetNextAvailableSlot();

            if (CheckSlotEmpty(slot))
                throw new ErrorResponse($"Slot {slot} is empty on Station {StationID}.");

            Payload payload = slots[slot];
            slots.Remove(slot);
            Log?.Invoke(this, $"Payload {payload.PayloadID} removed from slot {slot} on Station {StationID}.");
            return payload;
        }
    }
}
