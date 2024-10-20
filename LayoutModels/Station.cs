﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using LayoutCommands;
using LayoutModels.Support;
using static System.Collections.Specialized.BitVector32;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LayoutModels
{
    public enum StationState
    {
        UnDocked,
        Closed,
        Opening,
        Mapping,
        Open,
        BeingAccessed,
        Closing,
        Processing,
    }


    public class Station : BaseStation, ITarget
    {
        public event EventHandler<LogMessage>? OnLogEvent;

        public Dictionary<int, Payload> slots = new();

        private StationState StationState { get; set; }
        public StationState State { 
            get { return StationState; } 
            private set {
                StationState = value;
                OnLogEvent?.Invoke(this, new LogMessage($"Station {StationID} State was updated to {value}"));
            } 
        }

        public string PayloadType { get; private set; }
        public string InputState { get; set; }
        public string OutputState { get; set; }
        public int Capacity { get; private set; }
        public bool Processable { get; private set; }
        public float ProcessTime { get; set; }
        public bool HasDoor { get; private set; }
        public float DoorTransitionTime { get; private set; }
        public bool PodDockable { get; private set; }
        
        private string? podID = null;
        public string PodID {
            get 
            { 
                if (PodDockable && (State != StationState.UnDocked))
                {
                    return PodID;
                }
                else
                    throw new ErrorResponse(ErrorCodes.PodNotAvailable, $"Station {StationID} did not have a pod docked.");

            }
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

        public List<string> AcceptedCommands { get; set; }

        public Station(string stationID, string payloadType, string inputState, string outputState, int capacity, List<string> locations, bool processable, float processTime, bool hasDoor, float doorTransitionTime, bool podDockable, List<string> acceptedCommands)
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
            PodDockable = podDockable;
            Mappable = podDockable;
            statusMapped = !podDockable;
            AcceptedCommands = acceptedCommands;

            if (podDockable)
                State = StationState.UnDocked;
            else if (hasDoor)
                State = StationState.Closed;
            else
                State = StationState.Open;

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

            for (int i = 0; i < Capacity; i++)
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

            OnLogEvent?.Invoke(this, new LogMessage($"Station {StationID} map was {slotMap}."));
            return slotMap;
        }

        public void Dock(Job job, Pod pod)
        {
            if (State != StationState.UnDocked)
                throw new ErrorResponse(ErrorCodes.PodAlreadyAvailable, $"Station {StationID} already has pod.");

            if (!CheckAllSlotsEmpty())
                throw new ErrorResponse(ErrorCodes.SlotsNotEmpty, $"Station {StationID} slots are not empty.");


            PodID = pod.PodID;
            slots = pod.slots;
            statusMapped = false;
            if (HasDoor)
                State = StationState.Closed;
            else
                State = StationState.Open;
            OnLogEvent?.Invoke(this, new LogMessage(job.TransactionID, $"Pod {pod.PodID} was docked to Station {StationID}."));
        }

        public Pod UnDock(Job job)
        {
            if (State != StationState.Closed)
                throw new ErrorResponse(ErrorCodes.IncorrectState, $"Station {StationID} door is not closed.");

            Pod pod = new(PodID, Capacity, PayloadType);
            pod.slots = slots;
            statusMapped= false;
            State = StationState.UnDocked;
            OnLogEvent?.Invoke(this, new LogMessage(job.TransactionID, $"Pod {pod.PodID} was undocked from Station {StationID}."));
            return pod;
        }

        public void Door(Job job, bool requestedStatus)
        {
            // 0 -> Open Door
            // 1 -> Close Door

            Busy = true;
            if (requestedStatus && State == StationState.Open)
            {
                OnLogEvent?.Invoke(this, new LogMessage(job.TransactionID, $"Station {StationID} door Closing."));
                State = StationState.Closing;
                ProcessWait(DoorTransitionTime);
                State = StationState.Closed;
                OnLogEvent?.Invoke(this, new LogMessage(job.TransactionID, $"Station {StationID} door Closed."));
            }
            else if (!requestedStatus && State == StationState.Closed) {
                OnLogEvent?.Invoke(this, new LogMessage(job.TransactionID, $"Station {StationID} door Opening."));
                State = StationState.Opening;
                ProcessWait(DoorTransitionTime);
                State = StationState.Open;
                OnLogEvent?.Invoke(this, new LogMessage(job.TransactionID, $"Station {StationID} door Open."));
            }
            else
            {
                OnLogEvent?.Invoke(this, new LogMessage(job.TransactionID, $"Station {StationID} was in state {State}."));
                throw new ErrorResponse(ErrorCodes.IncorrectState, $"Station {StationID} door operation failed.");
            }
            Busy = false;
        }

        public void Process(Job job)
        {
            if (CheckAllSlotsEmpty())
                throw new ErrorResponse(ErrorCodes.SlotsEmpty, $"Station {StationID} does not have any payloads to process.");

            Busy = true;
            OnLogEvent?.Invoke(this, new LogMessage(job.TransactionID, $"Station {StationID} Process Started."));
            ProcessWait(ProcessTime);

            foreach(KeyValuePair<int, Payload> slot in slots)
            {
                if (slot.Value.PayloadState != InputState)
                    OnLogEvent?.Invoke(this, new LogMessage(job.TransactionID, $"Payload {slot.Value.PayloadID} or Station {StationID} slot {slot.Key} has State {slot.Value.PayloadState} and does not match station Input state, {InputState}. Process continued..."));
                slot.Value.PayloadState = OutputState;
            }

            OnLogEvent?.Invoke(this, new LogMessage(job.TransactionID, $"Station {StationID} Process Complete."));
            Busy = false;
        }

        public List<MapCodes> OpenDoorAndMap(Job job)
        {
            if (State == StationState.UnDocked)
                throw new ErrorResponse(ErrorCodes.PodNotAvailable, $"Station {StationID} did not have a docked pod.");

            if (State != StationState.Closed)
                throw new ErrorResponse(ErrorCodes.IncorrectState, $"Station {StationID} door was not closed.");

            Busy = true;
            OnLogEvent?.Invoke(this, new LogMessage(job.TransactionID, $"Station {StationID} door Mapping."));
            State = StationState.Mapping;
            ProcessWait(DoorTransitionTime);
            List<MapCodes> slotMap = GetMap();
            State = StationState.Open;
            OnLogEvent?.Invoke(this, new LogMessage(job.TransactionID, $"Station {StationID} was mapped."));
            Busy = false;
            return slotMap;
        }

        public void StartStationAccess()
        {
            if (Busy)
                throw new ErrorResponse(ErrorCodes.NotAccessible, $"Station {StationID} busy.");
            if (State != StationState.Open)
                throw new ErrorResponse(ErrorCodes.NotAccessible, $"Station {StationID} door is not open.");
            State = StationState.BeingAccessed;
        }
        
        public void StopStationAccess()
        {
            if (Busy)
                throw new ErrorResponse(ErrorCodes.NotAccessible, $"Station {StationID} busy.");
            if (State != StationState.BeingAccessed)
                throw new ErrorResponse(ErrorCodes.ProgramError, $"Station {StationID} being accessed by robot.");
            State = StationState.Open;
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
