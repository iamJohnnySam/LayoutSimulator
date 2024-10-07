using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels
{
    internal class Station(string stationID, string payloadType, string inputState, string outputState, int capacity, string location, bool processable, int processTime, bool hasDoor, int doorTransitionTime, bool podDockable) : ITarget
    {
        public event EventHandler<LogMessage>? Log;

        public Dictionary<int, Payload> slots = new();

        public string StationID { get; private set; } = stationID;
        public string PayloadType { get; private set; } = payloadType;
        public string InputState { get; set; } = inputState;
        public string OutputState { get; set; } = outputState;
        public int Capacity { get; private set; } = capacity;
        public string Location { get; private set; } = location;
        public bool Processable { get; private set; } = processable;
        public int ProcessTime { get; set; } = processTime;
        public bool HasDoor { get; private set; } = hasDoor;
        public int DoorTransitionTime { get; private set; } = doorTransitionTime;
        
        private bool statusDoorOpen = false;
        public bool StatusDoorOpen {
            get { return statusDoorOpen; }
            private set { 
                statusDoorOpen = value;
                Log?.Invoke(this, new LogMessage($"Station {StationID} Door Open Status updated to {value})"));
            }
        }

        private bool statusDoorClosed = true;
        public bool StatusDoorClosed {
            get { return statusDoorClosed; }
            private set
            {
                statusDoorClosed = value;
                Log?.Invoke(this, new LogMessage($"Station {StationID} Door Closed Status updated to {value}"));
            }
        }

        private bool statusDoorOpening = false;
        public bool StatusDoorOpening {
            get { return statusDoorOpening; }
            private set
            {
                statusDoorOpening = value;
                Log?.Invoke(this, new LogMessage($"Station {StationID} Door Opening Status updated to {value}"));
            }
        }

        private bool statusDoorClosing = false;
        public bool StatusDoorClosing {
            get { return statusDoorClosing; }
            private set
            {
                statusDoorClosing = value;
                Log?.Invoke(this, new LogMessage($"Station {StationID} Door Closing Status updated to {value}"));
            }
        }

        private bool statusBeingAccessed = false;
        public bool StatusBeingAccessed {
            get { return statusBeingAccessed; }
            private set { 
                statusBeingAccessed = value;
                Log?.Invoke(this, new LogMessage($"Station {StationID} Being Accessed Status updated to {value}"));
            }
        }
        public bool PodDockable { get; private set; } = podDockable;
        
        private bool statusPodDocked = false;
        public bool StatusPodDocked {
            get { return statusPodDocked; }
            private set
            {
                statusPodDocked = value;
                Log?.Invoke(this, new LogMessage($"Station {StationID} Pod Docked Status updated to {value}"));
            }
        }
        
        private string? podID = null;
        public string PodID {
            get { return PodID; }
            private set
            {
                podID = value;
                Log?.Invoke(this, new LogMessage($"Station {StationID} Pod ID updated to {value}"));
            }
        }

        private bool busy = false;
        public bool Busy {
            get { return busy; }
            private set
            {
                busy = value;
                Log?.Invoke(this, new LogMessage($"Station {StationID} Busy Status updated to {value}"));
            }
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

        private bool CheckAllSlotsEmpty()
        {
            if (slots.Count == 0) return true;
            return false;
        }

        private int GetNextEmptySlot()
        {
            for (int i = 0; i < Capacity; i++) {
                int slot = i + 1;
                if (slots.ContainsKey(slot))
                {
                    Log?.Invoke(this, new LogMessage($"Station {StationID} Next empty slot ({slot}) was updated."));
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
                    Log?.Invoke(this, new LogMessage($"Station {StationID} Next available slot ({slot}) was updated."));
                    return slot;
                }
            }
            return 1;
        }

        public void Dock(string transactionID, Pod pod)
        {
            if (!PodDockable)
                throw new ErrorResponse($"{transactionID}: Station {StationID} is not dockable.");

            if (statusPodDocked)
                throw new ErrorResponse($"{transactionID}: Station {StationID} dock is already occupied by pod {podID}.");

            if (!CheckAllSlotsEmpty())
                throw new ErrorResponse($"{transactionID}: Station {StationID} slots are not empty.");


            PodID = pod.PodID;
            slots = pod.slots;
            statusPodDocked = true;
            Log?.Invoke(this, new LogMessage(transactionID, $"Pod {pod.PodID} was docked to Station {StationID}."));

        }

        public Pod UnDock(string transactionID)
        {
            if (!PodDockable)
                throw new ErrorResponse($"{transactionID}: Station {StationID} is not dockable.");

            if (!statusPodDocked)
                throw new ErrorResponse($"{transactionID}: Station {StationID} does not have a pod.");

            Pod pod = new(PodID, Capacity, PayloadType);
            pod.slots = slots;
            statusPodDocked = false;
            Log?.Invoke(this, new LogMessage(transactionID, $"Pod {pod.PodID} was undocked from Station {StationID}."));
            return pod;
        }

        public void Door(string transactionID, bool requestedStatus)
        {
            // 0 -> Open
            // 1 -> Closed
        }

        public void Process(string transactionID)
        {

        }

        public void Map(string transactionID)
        {

        }

        public void AcceptPayload(string transactionID, Payload payload, int slot)
        {
            if (!CheckAcessible())
                throw new ErrorResponse($"{transactionID}: Station {StationID} is not accessible.");
    

            if (!CheckPayloadCompatible(payload))
                throw new ErrorResponse($"{transactionID}: Payload {payload.PayloadID} is not compatible with Station {StationID}.");

            if (slot == 0)
                slot = GetNextEmptySlot();

            if (!CheckSlotEmpty(slot))
                throw new ErrorResponse($"{transactionID}: Slot {slot} is not empty on Station {StationID}.");

            slots.Add(slot, payload);
            Log?.Invoke(this, new LogMessage(transactionID, $"Payload {payload.PayloadID} added to slot {slot} on Station {StationID}."));
        }

        public Payload ReleasePayload(string transactionID, int slot)
        {
            if (!CheckAcessible())
                throw new ErrorResponse($"{transactionID}: Station {StationID} is not accessible.");

            if (slot == 0)
                slot = GetNextAvailableSlot();

            if (CheckSlotEmpty(slot))
                throw new ErrorResponse($"{transactionID}: Slot {slot} is empty on Station {StationID}.");

            Payload payload = slots[slot];
            slots.Remove(slot);
            Log?.Invoke(this, new LogMessage (transactionID, $"Payload {payload.PayloadID} removed from slot {slot} on Station {StationID}."));
            return payload;
        }
    }
}
