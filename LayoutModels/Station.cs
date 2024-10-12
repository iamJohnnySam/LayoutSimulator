using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace LayoutModels
{
    internal class Station : ITarget
    {
        public event EventHandler<LogMessage>? Log;

        public Dictionary<int, Payload> slots = new();

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

        private DoorStates statusDoor = DoorStates.Close;
        public DoorStates StatusDoor {
            get { return statusDoor; }
            private set { 
                statusDoor = value;
                Log?.Invoke(this, new LogMessage($"Station {StationID} Door Open Status updated to {value})"));
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
        public bool PodDockable { get; private set; }

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

        public bool Mappable { get; set; }

        private bool statusMapped = false;
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

        public Station(string stationID, string payloadType, string inputState, string outputState, int capacity, string location, bool processable, int processTime, bool hasDoor, int doorTransitionTime, bool podDockable)
        {
            StationID = stationID;
            PayloadType = payloadType;
            InputState = inputState;
            OutputState = outputState;
            Capacity = capacity;
            Location = location;
            Processable = processable;
            ProcessTime = processTime;
            HasDoor = hasDoor;
            DoorTransitionTime = doorTransitionTime;
            if (!hasDoor)
                statusDoor = DoorStates.Open;

            PodDockable = podDockable;

            Mappable = podDockable;
            statusMapped = !podDockable;
        }

        private bool CheckAcessible()
        {
            if (StatusBeingAccessed) return false;
            else if (HasDoor && (statusDoor != DoorStates.Open)) return false;
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

            Log?.Invoke(this, new LogMessage($"Station {StationID} map was {slotMap.ToString}."));
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

        public DoorStates Door(string transactionID, bool requestedStatus)
        {
            // 0 -> Open Door
            // 1 -> Close Door

            if (Busy)
                throw new ErrorResponse(FaultCodes.Busy);

            if (!HasDoor)
                throw new ErrorResponse(FaultCodes.StationDoesNotHaveDoor);

            Busy = true;
            if (requestedStatus)
            {
                switch (StatusDoor)
                {
                    case DoorStates.Open:
                        Log?.Invoke(this, new LogMessage(transactionID, $"Station {StationID} door Closing."));
                        StatusDoor = DoorStates.Closing;
                        TimeKeeper.ProcessWait(DoorTransitionTime);
                        StatusDoor = DoorStates.Close;
                        Log?.Invoke(this, new LogMessage(transactionID, $"Station {StationID} door Closed."));
                        break;
                    case DoorStates.Close:
                        Log?.Invoke(this, new LogMessage(transactionID, $"Station {StationID} door already Closed."));
                        break;
                    case DoorStates.Opening:
                    case DoorStates.Closing:
                    case DoorStates.Mapping:
                        throw new ErrorResponse(FaultCodes.ProgramError);
                }
            }
            else
            {
                switch (StatusDoor)
                {
                    case DoorStates.Open:
                        Log?.Invoke(this, new LogMessage(transactionID, $"Station {StationID} door already Open."));
                        break;
                    case DoorStates.Close:
                        Log?.Invoke(this, new LogMessage(transactionID, $"Station {StationID} door Opening."));
                        StatusDoor = DoorStates.Opening;
                        TimeKeeper.ProcessWait(DoorTransitionTime);
                        StatusDoor = DoorStates.Open;
                        StatusDoor = DoorStates.Close;
                        break;
                    case DoorStates.Opening:
                    case DoorStates.Closing:
                    case DoorStates.Mapping:
                        throw new ErrorResponse(FaultCodes.ProgramError);
                }

            }
            Busy = false;
            return StatusDoor;
        }

        public void Process(string transactionID)
        {
            if (Busy)
                throw new ErrorResponse(FaultCodes.Busy);
            if (CheckAllSlotsEmpty())
                throw new ErrorResponse(FaultCodes.SlotsEmpty);

            Busy = true;
            Log?.Invoke(this, new LogMessage(transactionID, $"Station {StationID} Process Started."));
            TimeKeeper.ProcessWait(ProcessTime);

            foreach(KeyValuePair<int, Payload> slot in slots)
            {
                if (slot.Value.PayloadState != InputState)
                    Log?.Invoke(this, new LogMessage(transactionID, $"Payload {slot.Value.PayloadID} or Station {StationID} slot {slot.Key} has State {slot.Value.PayloadState} and does not match station Input state, {InputState}. Process continued..."));
                slot.Value.PayloadState = OutputState;
            }

            Log?.Invoke(this, new LogMessage(transactionID, $"Station {StationID} Process Complete."));
            Busy = false;
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
            StatusDoor = DoorStates.Mapping;

            TimeKeeper.ProcessWait(DoorTransitionTime * 2);
            List<MapCodes> slotMap = GetMap();

            StatusDoor = DoorStates.Open;
            Busy = false;

            Log?.Invoke(this, new LogMessage(transactionID, $"Station {StationID} was mapped."));
            return slotMap;
        }

        public string AcceptPayload(string transactionID, Payload payload, int slot)
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
            return payload.PayloadID;
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
