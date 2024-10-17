using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using LayoutModels.Support;
using static System.Collections.Specialized.BitVector32;

namespace LayoutModels
{
    public class Manipulator : BaseStation, ITarget
    {
        public event EventHandler<LogMessage>? OnLogEvent;

        public Dictionary<int, Dictionary<string, Payload>> EndEffectors { get; private set; }
        public List<string> EndEffectorTypes { get; set; }
        public int MotionTime { get; private set; }
        public int ExtendTime { get; private set; }
        public int RetractTime { get; private set; }
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


        public Manipulator(string manipulatorID, Dictionary<int, Dictionary<string, Payload>> endEffectors, List<string> endEffectorsTypes, List<string> locations, int motionTime, int extendTime, int retractTime)
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
            if (CurrentLocation != StationID)
            {
                TimeKeeper.ProcessWait(MotionTime);
                CurrentLocation = StationID;
            }
        }

        private void Extend()
        {
            TimeKeeper.ProcessWait(ExtendTime);
            ArmState = ManipulatorArmStates.extended;
        }

        private void Retract()
        {
            TimeKeeper.ProcessWait(RetractTime);
            ArmState = ManipulatorArmStates.retracted;
        }

        public void Pick(string transactionID, int endEffector, Station station, int slot)
        {
            if (EndEffectors[endEffector].ContainsKey("payload"))
                throw new ErrorResponse(ErrorCodes.PayloadAlreadyAvailable);

            if (!Locations.Intersect(station.Locations).Any())
                throw new ErrorResponse(ErrorCodes.StationNotReachable);

            if (EndEffectorTypes[endEffector] != station.PayloadType)
                throw new ErrorResponse(ErrorCodes.PayloadTypeMismatch);

            if (ArmState != ManipulatorArmStates.retracted)
                throw new ErrorResponse(ErrorCodes.UnknownArmState);

            if (slot == 0)
                slot = station.GetNextAvailableSlot();

            if (slot > station.Capacity)
                throw new ErrorResponse(ErrorCodes.SlotIndexMissing);

            if (station.CheckSlotEmpty(slot))
                throw new ErrorResponse(ErrorCodes.PayloadNotAvailable);

            Busy = true;
            GoToStation(station.StationID);

            station.StartStationAccess();

            Extend();
            EndEffectors[endEffector]["payload"] = station.ReleasePayload(transactionID, slot);
            Retract();

            station.StopStationAccess();
            Busy = false;

        }

        public void Place(string transactionID, int endEffector, Station station, int slot)
        {
            if (!EndEffectors[endEffector].ContainsKey("payload"))
                throw new ErrorResponse(ErrorCodes.PayloadNotAvailable);

            if (!Locations.Intersect(station.Locations).Any())
                throw new ErrorResponse(ErrorCodes.StationNotReachable);

            if (ArmState != ManipulatorArmStates.retracted)
                throw new ErrorResponse(ErrorCodes.UnknownArmState);

            if (!station.CheckPayloadCompatible(EndEffectors[endEffector]["payload"]))
                throw new ErrorResponse(ErrorCodes.PayloadTypeMismatch);

            if (slot == 0)
                slot = station.GetNextEmptySlot();

            if (slot > station.Capacity)
                throw new ErrorResponse(ErrorCodes.SlotIndexMissing);

            if (!station.CheckSlotEmpty(slot))
                throw new ErrorResponse(ErrorCodes.PayloadAlreadyAvailable);

            Busy = true;
            GoToStation(station.StationID);

            station.StartStationAccess();

            Extend();
            station.AcceptPayload(transactionID, EndEffectors[endEffector]["payload"], slot);
            EndEffectors[endEffector].Remove("payload");
            Retract();

            station.StopStationAccess();
            Busy = false;
        }

        public void Home(string transactionID)
        {
            Busy = true;

            OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Manipulator {StationID} Homing"));

            if (ArmState != ManipulatorArmStates.retracted)
                Retract();

            GoToStation("home");
            Busy = false;
            OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Manipulator {StationID} at Home"));
        }

        public void PowerOff(string transactionID)
        {
            Power = false;
            if (Busy)
                throw new ErrorResponse(ErrorCodes.PowerOffWhileBusy);
            OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Manipulator {StationID} Off"));
        }

        public void PowerOn(string transactionID)
        {
            if (Busy)
                throw new ErrorResponse(ErrorCodes.ProgramError);

            Power = true;
            OnLogEvent?.Invoke(this, new LogMessage(transactionID, $"Manipulator {StationID} On"));
        }


    }
}
