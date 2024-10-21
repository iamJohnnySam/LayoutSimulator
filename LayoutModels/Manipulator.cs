using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using LayoutCommands;
using LayoutModels.Support;
using static System.Collections.Specialized.BitVector32;

namespace LayoutModels
{
    public class Manipulator : BaseStation, ITarget
    {
        public event EventHandler<LogMessage>? OnLogEvent;

        public Dictionary<int, Dictionary<string, Payload>> EndEffectors { get; private set; }
        public List<string> EndEffectorTypes { get; set; }
        public float MotionTime { get; private set; }
        public float ExtendTime { get; private set; }
        public float RetractTime { get; private set; }
        public ManipulatorArmStates ArmState { get; private set; } = ManipulatorArmStates.retracted;
        public string CurrentLocation { get; private set; } = "home";

        // Todo: convert to state models

        private bool power = false;
        public bool Power { 
            get { return power; }
            private set
            { 
                power = value;
                OnLogEvent?.Invoke(this, new LogMessage($"Manipulator {StationID} Power status changed to {value}"));
            }
        }


        public Manipulator(string manipulatorID, Dictionary<int, Dictionary<string, Payload>> endEffectors, List<string> endEffectorsTypes, List<string> locations, float motionTime, float extendTime, float retractTime)
        {
            StationID = manipulatorID;
            EndEffectors = endEffectors;
            EndEffectorTypes = endEffectorsTypes;
            Locations = locations;
            MotionTime = motionTime;
            ExtendTime = extendTime;
            RetractTime = retractTime;
        }

        private void GoToStation(string stationID)
        {
            if (CurrentLocation != stationID)
            {
                ProcessWait(MotionTime);
                CurrentLocation = stationID;
            }
        }

        private void Extend()
        {
            ProcessWait(ExtendTime);
            ArmState = ManipulatorArmStates.extended;
        }

        private void Retract()
        {
            ProcessWait(RetractTime);
            ArmState = ManipulatorArmStates.retracted;
        }

        public void Pick(Job job, int endEffector, Station station, int slot)
        {
            if (EndEffectors[endEffector].ContainsKey("payload"))
                throw new ErrorResponse(ErrorCodes.PayloadAlreadyAvailable, $"Manipulator {StationID} End Effector {endEffector} did not contain payload.");

            if (!Locations.Intersect(station.Locations).Any())
                throw new ErrorResponse(ErrorCodes.StationNotReachable, $"Manipulator {StationID} could not access any locations.");

            if (EndEffectorTypes[endEffector - 1] != station.PayloadType)
                throw new ErrorResponse(ErrorCodes.PayloadTypeMismatch, $"Manipulator {StationID} End Effector did not match the payload type for this station.");

            if (ArmState != ManipulatorArmStates.retracted)
                throw new ErrorResponse(ErrorCodes.UnknownArmState, $"Manipulator {StationID} arm was not retracted. Home the Manipulator.");

            if (slot == 0)
                slot = station.GetNextAvailableSlot();

            if (slot > station.Capacity)
                throw new ErrorResponse(ErrorCodes.SlotIndexMissing, $"Manipulator {StationID} did not recognize slot {slot}.");

            if (station.CheckSlotEmpty(slot))
                throw new ErrorResponse(ErrorCodes.PayloadNotAvailable, $"Manipulator {StationID} slot {slot} access on was empty.");

            Busy = true;
            string response = ProcessCommand(job);

            GoToStation(station.StationID);

            station.StartStationAccess();

            Extend();
            EndEffectors[endEffector]["payload"] = station.ReleasePayload(job.TransactionID, slot);
            Retract();

            station.StopStationAccess();
            Busy = false;

        }

        public void Place(Job job, int endEffector, Station station, int slot)
        {
            if (!EndEffectors[endEffector].ContainsKey("payload"))
                throw new ErrorResponse(ErrorCodes.PayloadNotAvailable, $"Manipulator {StationID} End Effector {endEffector} did not contain payload.");

            if (!Locations.Intersect(station.Locations).Any())
                throw new ErrorResponse(ErrorCodes.StationNotReachable, $"Manipulator {StationID} could not access any locations.");

            if (ArmState != ManipulatorArmStates.retracted)
                throw new ErrorResponse(ErrorCodes.UnknownArmState, $"Manipulator {StationID} arm was not retracted. Home the Manipulator.");

            if (!station.CheckPayloadCompatible(EndEffectors[endEffector]["payload"]))
                throw new ErrorResponse(ErrorCodes.PayloadTypeMismatch, $"Manipulator {StationID} End Effector did not match the payload type for this station.");

            if (slot == 0)
                slot = station.GetNextEmptySlot();

            if (slot > station.Capacity)
                throw new ErrorResponse(ErrorCodes.SlotIndexMissing, $"Manipulator {StationID} did not recognize slot {slot}.");

            if (!station.CheckSlotEmpty(slot))
                throw new ErrorResponse(ErrorCodes.PayloadAlreadyAvailable, $"Manipulator {StationID} slot {slot} access on was empty.");

            Busy = true;
            string response = ProcessCommand(job);

            GoToStation(station.StationID);

            station.StartStationAccess();

            Extend();
            station.AcceptPayload(job.TransactionID, EndEffectors[endEffector]["payload"], slot);
            EndEffectors[endEffector].Remove("payload");
            Retract();

            station.StopStationAccess();
            Busy = false;
        }

        public void Home(Job job)
        {
            Busy = true;
            string response = ProcessCommand(job);

            OnLogEvent?.Invoke(this, new LogMessage(job.TransactionID, $"Manipulator {StationID} Homing"));

            if (ArmState != ManipulatorArmStates.retracted)
                Retract();

            GoToStation("home");
            Busy = false;
            OnLogEvent?.Invoke(this, new LogMessage(job.TransactionID, $"Manipulator {StationID} at Home"));
        }

        public void PowerOff(Job job)
        {
            string response = ProcessCommand(job);
            Power = false;
            if (Busy)
                throw new ErrorResponse(ErrorCodes.PowerOffWhileBusy, $"Manipulator {StationID} was busy.");
            OnLogEvent?.Invoke(this, new LogMessage(job.TransactionID, $"Manipulator {StationID} Off."));
        }

        public void PowerOn(Job job)
        {
            string response = ProcessCommand(job);
            if (Busy)
                throw new ErrorResponse(ErrorCodes.ProgramError, $"Manipulator {StationID} was busy.");

            Power = true;
            OnLogEvent?.Invoke(this, new LogMessage(job.TransactionID, $"Manipulator {StationID} On"));
        }


    }
}
