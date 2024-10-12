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
    internal class Manipulator : BaseStation, ITarget
    {
        public event EventHandler<LogMessage>? Log;

        public Dictionary<int, Dictionary<string, Payload>> EndEffectors { get; private set; }
        public int MotionTime { get; private set; }
        public int ExtendTime { get; private set; }
        public int RetractTime { get; private set; }
        public ManipulatorArmStates ArmState { get; private set; } = ManipulatorArmStates.retracted;
        public string CurrentLocation { get; private set; } = "home";

        private bool power = false;
        public bool Power { 
            get { return power; }
            private set
            { 
                power = value;
                Log?.Invoke(this, new LogMessage($"Manipulator {StationID} Power status changed to {value}"));
            }
        }


        public Manipulator(string manipulatorID, Dictionary<int, Dictionary<string, Payload>> endEffectors, List<string> locations, int motionTime, int extendTime, int retractTime)
        {
            StationID = manipulatorID;
            EndEffectors = endEffectors;
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

        public void Pick(string transactionID, int endEffector, Station station, int slot)
        {
            if (Busy)
                throw new ErrorResponse(FaultCodes.Busy);

            if (!Power)
                throw new ErrorResponse(FaultCodes.PowerOff);

            if (!EndEffectors.ContainsKey(endEffector))
                throw new ErrorResponse(FaultCodes.EndEffectorMissing);

            if (EndEffectors[endEffector].ContainsKey("payload"))
                throw new ErrorResponse(FaultCodes.PayloadAlreadyAvailable);

            // TODO: Payload Mismatch

            if (!Locations.Intersect(station.Locations).Any())
                throw new ErrorResponse(FaultCodes.StationNotReachable);

            if (ArmState != ManipulatorArmStates.retracted)
                throw new ErrorResponse(FaultCodes.UnknownArmState);

            Busy = true;
            GoToStation(station.StationID);

            station.StatusBeingAccessed = true;

            TimeKeeper.ProcessWait(ExtendTime);
            ArmState = ManipulatorArmStates.extended;

            EndEffectors[endEffector]["payload"] = station.ReleasePayload(transactionID, slot);

            TimeKeeper.ProcessWait(RetractTime);
            ArmState = ManipulatorArmStates.retracted;

            station.StatusBeingAccessed = false;
            Busy = false;

        }

        public void Place(string transactionID, int endEffector, Station station, int slot)
        {
            if (Busy)
                throw new ErrorResponse(FaultCodes.Busy);

            if (!Power)
                throw new ErrorResponse(FaultCodes.PowerOff);

            if (!EndEffectors.ContainsKey(endEffector))
                throw new ErrorResponse(FaultCodes.EndEffectorMissing);

            if (!EndEffectors[endEffector].ContainsKey("payload"))
                throw new ErrorResponse(FaultCodes.PayloadNotAvailable);

            if (!Locations.Intersect(station.Locations).Any())
                throw new ErrorResponse(FaultCodes.StationNotReachable);

            if (ArmState != ManipulatorArmStates.retracted)
                throw new ErrorResponse(FaultCodes.UnknownArmState);

            Busy = true;
            GoToStation(station.StationID);

            station.StatusBeingAccessed = true;

            TimeKeeper.ProcessWait(ExtendTime);
            ArmState = ManipulatorArmStates.extended;

            station.AcceptPayload(transactionID, EndEffectors[endEffector]["payload"], slot);
            EndEffectors[endEffector].Remove("payload");

            TimeKeeper.ProcessWait(RetractTime);
            ArmState = ManipulatorArmStates.retracted;

            station.StatusBeingAccessed = false;
            Busy = false;
        }

        public void Home(string transactionID)
        {
            if (Busy)
                throw new ErrorResponse(FaultCodes.Busy);

            if (!Power)
                throw new ErrorResponse(FaultCodes.PowerOff);

            if (ArmState != ManipulatorArmStates.retracted)
                throw new ErrorResponse(FaultCodes.UnknownArmState);

            Log?.Invoke(this, new LogMessage(transactionID, $"Manipulator {StationID} Homing"));
            Busy = true;
            GoToStation("home");
            Busy = false;
            Log?.Invoke(this, new LogMessage(transactionID, $"Manipulator {StationID} at Home"));
        }

        public void PowerOff(string transactionID)
        {
            Power = false;
            if (Busy)
                throw new ErrorResponse(FaultCodes.PowerOffWhileBusy);
            Log?.Invoke(this, new LogMessage(transactionID, $"Manipulator {StationID} Off"));
        }

        public void PowerOn(string transactionID)
        {
            if (Busy)
                throw new ErrorResponse(FaultCodes.ProgramError);

            Power = true;
            Log?.Invoke(this, new LogMessage(transactionID, $"Manipulator {StationID} On"));
        }


    }
}
