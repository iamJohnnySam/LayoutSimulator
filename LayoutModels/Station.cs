﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using LayoutModels.Support;
using static System.Collections.Specialized.BitVector32;

namespace LayoutModels
{
    public class Station : BaseStation, ITarget
    {
        public event EventHandler<LogMessage>? OnLogEvent;

        public Dictionary<int, Payload> slots = new();

        public string PayloadType { get; private set; }
        public string InputState { get; set; }
        public string OutputState { get; set; }
        public int Capacity { get; private set; }
        public bool Processable { get; private set; }
        public int ProcessTime { get; set; }
        public bool HasDoor { get; private set; }
        public int DoorTransitionTime { get; private set; }

        private DoorStates statusDoor = DoorStates.Close;
        public DoorStates StatusDoor {
            get { return statusDoor; }
            private set { 
                statusDoor = value;
                OnLogEvent?.Invoke(this, new LogMessage($"Station {StationID} Door Status updated to {value}"));
            }
        }

        private bool statusBeingAccessed = false;
        public bool StatusBeingAccessed {
            get { return statusBeingAccessed; }
            set { 
                statusBeingAccessed = value;
                OnLogEvent?.Invoke(this, new LogMessage($"Station {StationID} Being Accessed Status updated to {value}"));
            }
        }
        public bool PodDockable { get; private set; }

        private bool statusPodDocked = false;
        public bool StatusPodDocked {
            get { return statusPodDocked; }
            private set
            {
                statusPodDocked = value;
                OnLogEvent?.Invoke(this, new LogMessage($"Station {StationID} Pod Docked Status updated to {value}"));
            }
        }
        
        private string? podID = null;
        public string PodID {
            get { return PodID; }
            private set
            {
                podID = value;
                OnLogEvent?.Invoke(this, new LogMessage($"Station {StationID} Pod ID updated to {value}"));
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
                OnLogEvent?.Invoke(this, new LogMessage($"Station {StationID} Map Status updated to {value}"));
            }
        }

        public Station(string stationID, string payloadType, string inputState, string outputState, int capacity, List<string> locations, bool processable, int processTime, bool hasDoor, int doorTransitionTime, bool podDockable)
        {
            StationID = stationID;
            PayloadType = payloadType;
            InputState = inputState;
            OutputState = outputState;
            Capacity = capacity;
            Locations = locations;
            Processable = processable;
            ProcessTime = processTime;
            HasDoor = hasDoor;
            DoorTransitionTime = doorTransitionTime;
            if (!hasDoor)
                statusDoor = DoorStates.Open;

            PodDockable = podDockable;

            Mappable = podDockable;
            statusMapped = !podDockable;

            // TODO: Auto Open if Pod Docked

            OnLogEvent?.Invoke(this, new LogMessage($"Station {StationID} Created."));
        }

        public bool CheckPayloadCompatible(Payload payload)
        {
            if (payload.PayloadType != PayloadType)
                return false;
            return true;
        }

        public bool CheckSlotEmpty(int slot)
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

        public int GetNextEmptySlot()
        {
            for (int i = 0; i < Capacity; i++) {
                int slot = i + 1;
                if (slots.ContainsKey(slot))
                {
                    OnLogEvent?.Invoke(this, new LogMessage($"Station {StationID} Next empty slot ({slot}) was updated."));
                    return slot;
                }
            }
            return Capacity;
        }

        public int GetNextAvailableSlot()
        {
            for (int i = 0; i < Capacity; i++)
            {
                int slot = i + 1;
                if (!slots.ContainsKey(slot))
                {
                    OnLogEvent?.Invoke(this, new LogMessage($"Station {StationID} Next available slot ({slot}) was updated."));
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

            OnLogEvent?.Invoke(this, new LogMessage($"Station {StationID} map was {slotMap.ToString}."));
            return slotMap;
        }

        public void Dock(string transactionID, Pod pod)
        {
            if (statusPodDocked)
                throw new ErrorResponse(ErrorCodes.PodAlreadyAvailable);

            if (!CheckAllSlotsEmpty())
                throw new ErrorResponse(ErrorCodes.SlotsNotEmpty);


            PodID = pod.PodID;
            slots = pod.slots;
            statusPodDocked = true;
            statusMapped = false;
            OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Pod {pod.PodID} was docked to Station {StationID}."));
        }

        public Pod UnDock(string transactionID)
        {
            if (!statusPodDocked)
                throw new ErrorResponse(ErrorCodes.PodNotAvailable);

            Pod pod = new(PodID, Capacity, PayloadType);
            pod.slots = slots;
            statusPodDocked = false;
            statusMapped= false;
            OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Pod {pod.PodID} was undocked from Station {StationID}."));
            return pod;
        }

        public DoorStates Door(string transactionID, bool requestedStatus)
        {
            // 0 -> Open Door
            // 1 -> Close Door

            if (!statusPodDocked && PodDockable)
                throw new ErrorResponse(ErrorCodes.PodNotAvailable);

            Busy = true;
            if (requestedStatus)
            {
                switch (StatusDoor)
                {
                    case DoorStates.Open:
                        OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Station {StationID} door Closing."));
                        StatusDoor = DoorStates.Closing;
                        TimeKeeper.ProcessWait(DoorTransitionTime);
                        StatusDoor = DoorStates.Close;
                        OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Station {StationID} door Closed."));
                        break;
                    case DoorStates.Close:
                        OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Station {StationID} door already Closed."));
                        break;
                    case DoorStates.Opening:
                    case DoorStates.Closing:
                    case DoorStates.Mapping:
                        throw new ErrorResponse(ErrorCodes.ProgramError);
                }
            }
            else
            {
                switch (StatusDoor)
                {
                    case DoorStates.Open:
                        OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Station {StationID} door already Open."));
                        break;
                    case DoorStates.Close:
                        OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Station {StationID} door Opening."));
                        StatusDoor = DoorStates.Opening;
                        TimeKeeper.ProcessWait(DoorTransitionTime);
                        StatusDoor = DoorStates.Open;
                        StatusDoor = DoorStates.Close;
                        break;
                    case DoorStates.Opening:
                    case DoorStates.Closing:
                    case DoorStates.Mapping:
                        throw new ErrorResponse(ErrorCodes.ProgramError);
                }

            }
            Busy = false;
            return StatusDoor;
        }

        public void Process(string transactionID)
        {
            if (CheckAllSlotsEmpty())
                throw new ErrorResponse(ErrorCodes.SlotsEmpty);

            Busy = true;
            OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Station {StationID} Process Started."));
            TimeKeeper.ProcessWait(ProcessTime);

            foreach(KeyValuePair<int, Payload> slot in slots)
            {
                if (slot.Value.PayloadState != InputState)
                    OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Payload {slot.Value.PayloadID} or Station {StationID} slot {slot.Key} has State {slot.Value.PayloadState} and does not match station Input state, {InputState}. Process continued..."));
                slot.Value.PayloadState = OutputState;
            }

            OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Station {StationID} Process Complete."));
            Busy = false;
        }

        public List<MapCodes> Map(string transactionID)
        {
            if (PodDockable && !statusPodDocked)
                throw new ErrorResponse(ErrorCodes.PodNotAvailable);

            Busy = true;

            if (StatusDoor == DoorStates.Open)
            {
                StatusDoor = DoorStates.Mapping;
                TimeKeeper.ProcessWait(DoorTransitionTime * 2);
            }
            else
            {
                StatusDoor = DoorStates.Mapping;
                TimeKeeper.ProcessWait(DoorTransitionTime);
            }

            List<MapCodes> slotMap = GetMap();

            StatusDoor = DoorStates.Open;
            Busy = false;

            OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Station {StationID} was mapped."));
            return slotMap;
        }

        public string AcceptPayload(string transactionID, Payload payload, int slot)
        {
            slots.Add(slot, payload);

            OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Payload {payload.PayloadID} added to slot {slot} on Station {StationID}."));
            return payload.PayloadID;
        }

        public Payload ReleasePayload(string transactionID, int slot)
        {
            Payload payload = slots[slot];
            slots.Remove(slot);

            OnLogEvent?.Invoke(this, new LogMessage (transactionID, $"Payload {payload.PayloadID} removed from slot {slot} on Station {StationID}."));
            return payload;
        }
    }
}
