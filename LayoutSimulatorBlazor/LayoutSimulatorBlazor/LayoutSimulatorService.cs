using Communicator;
using LayoutCommands;
using LayoutModels;
using LayoutModels.CommSpecs;
using LayoutModels.Support;

namespace LayoutSimulatorBlazor
{
    public class LayoutSimulatorService
    {
        public Simulator Sim { get; private set; }
        public List<string> Logs { get; private set; } = new();

        TCPServer server = new("127.0.0.1", 8000);

        public LayoutSimulatorService(Simulator simulator)
        {
            Sim = simulator;
            Sim.AddCommSpec("commonCommSpec",
                new CommandStructure("<", ">", ",:", 0, 1, 2, 3, false, true, new Dictionary<CommandArgType, string>()),
                new ResponseStructure("<", ">", ",", 0, 1, -1, 2, 3, false, true, "valcmd"),
                new ResponseStructure("<", ">", ",", 0, 2, -1, 1, 3, false, true),
                new UniversalCommSpec());


            Sim.OnLogEvent += Sim_OnLogEvent;

            server.Start();
            server.OnMessageReceived += Server_OnMessageReceived;
            Sim.OnResponseEvent += Sim_OnResponseEvent;
        }

        private void Sim_OnResponseEvent(object? sender, string e)
        {
            server.SendMessage(e);
        }

        private void Sim_OnLogEvent(object? sender, LogMessage e)
        {
            Logs.Add($"{DateTime.Now} {e.transactionID}: {e.message}");
        }

        void Server_OnMessageReceived(object? sender, string e)
        {
            Sim.ExecuteCommands_NewThread(e, "commonCommSpec");
        }


        // You can add methods to interact with the simulator
        public void StartSimulation()
        {
            // Sim.Start();
        }

        public void StopSimulation()
        {
            // Sim.Stop();
        }
    }
}
