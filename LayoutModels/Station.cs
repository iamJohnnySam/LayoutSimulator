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

        public bool Mappable { get; set; } = podDockable;

        private bool statusMapped = !podDockable;
        public bool StatusMapped
        {
            get { return statusMapped; }
            private set
            {
                statusMapped = value;
                Log?.Invoke(this, new LogMessage($"Station {StationID} Map Status updated to {value}"));
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

        private List<MapCodes> GetMap()
        {
            List<MapCodes> slotMap = new();

            for (int i = 0; i < slots.Count; i++)
            {
                int slot = i + 1;
                if (!slots.ContainsKey(slot))
                    slotMap.Add(MapCodes.Empty);
                else if (slots[slot].PayloadErrorStaus)
                    if (slot > 1)
                    {
                        if (slotMap[slot - 1] == MapCodes.Double)
                        {
                            slotMap[slot - 1] = MapCodes.Cross;
                            slotMap.Add(MapCodes.Cross);
                        }
                        else
                            slotMap.Add(MapCodes.Double);
                    }
                    else
                        slotMap.Add(MapCodes.Double);
                else
                    slotMap.Add(MapCodes.Available);
            }
            return slotMap;
        }

        public void Dock(string transactionID, Pod pod)
        {
            if (Busy)
                throw new ErrorResponse(FaultCodes.Busy);

            if (!PodDockable)
                throw new ErrorResponse(FaultCodes.NotDockable);

            if (statusPodDocked)
                throw new ErrorResponse(FaultCodes.PodAlreadyAvailable);

            if (!CheckAllSlotsEmpty())
                throw new ErrorResponse(FaultCodes.SlotsNotEmpty);


            PodID = pod.PodID;
            slots = pod.slots;
            statusPodDocked = true;
            statusMapped = false;
            Log?.Invoke(this, new LogMessage(transactionID, $"Pod {pod.PodID} was docked to Station {StationID}."));

        }

        public Pod UnDock(string transactionID)
        {
            if (Busy)
                throw new ErrorResponse(FaultCodes.Busy);

            if (!PodDockable)
                throw new ErrorResponse(FaultCodes.NotDockable);

            if (!statusPodDocked)
                throw new ErrorResponse(FaultCodes.PodNotAvailable);

            Pod pod = new(PodID, Capacity, PayloadType);
            pod.slots = slots;
            statusPodDocked = false;
            statusMapped= false;
            Log?.Invoke(this, new LogMessage(transactionID, $"Pod {pod.PodID} was undocked from Station {StationID}."));
            return pod;
        }

        public void Door(string transactionID, bool requestedStatus)
        {
            // 0 -> Open
            // 1 -> Closed

            if (Busy)
                throw new ErrorResponse(FaultCodes.Busy);
        }

        public void Process(string transactionID)
        {
            if (Busy)
                throw new ErrorResponse(FaultCodes.Busy);
        }

        public List<MapCodes> Map(string transactionID)
        {
            if (Busy)
                throw new ErrorResponse(FaultCodes.Busy);

            if (!Mappable)
                throw new ErrorResponse(FaultCodes.NotMappable);

            if (PodDockable && !statusPodDocked)
                throw new ErrorResponse(FaultCodes.PodNotAvailable);

            Busy = true;
            StatusDoorOpen = false;

            TimeKeeper.ProcessWait(DoorTransitionTime * 2);
            List<MapCodes> slotMap = GetMap();

            StatusDoorOpen = true;
            Busy = false;

            return slotMap;
        }

        public void AcceptPayload(string transactionID, Payload payload, int slot)
        {
            if (!CheckAcessible())
                throw new ErrorResponse(FaultCodes.NotAccessible);

            if (!CheckPayloadCompatible(payload))
                throw new ErrorResponse(FaultCodes.PayloadTypeMismatch);

            if (slot == 0)
                slot = GetNextEmptySlot();

            if (!CheckSlotEmpty(slot))
                throw new ErrorResponse(FaultCodes.PayloadAlreadyAvailable);

            if (Busy)
                throw new ErrorResponse(FaultCodes.Busy);

            slots.Add(slot, payload);
            Log?.Invoke(this, new LogMessage(transactionID, $"Payload {payload.PayloadID} added to slot {slot} on Station {StationID}."));
        }

        public Payload ReleasePayload(string transactionID, int slot)
        {
            if (!CheckAcessible())
                throw new ErrorResponse(FaultCodes.NotAccessible);

            if (slot == 0)
                slot = GetNextAvailableSlot();

            if (CheckSlotEmpty(slot))
                throw new ErrorResponse(FaultCodes.PayloadNotAvailable);

            if (Busy)
                throw new ErrorResponse(FaultCodes.Busy);

            Payload payload = slots[slot];
            slots.Remove(slot);
            Log?.Invoke(this, new LogMessage (transactionID, $"Payload {payload.PayloadID} removed from slot {slot} on Station {StationID}."));
            return payload;
        }
    }
}
