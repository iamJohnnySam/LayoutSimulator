using LayoutModels.CommSpecs;
using LayoutModels;
using Communicator;
using LayoutCommands;
using Grpc.Core;

string? command = null;

TCPServer server = new ("127.0.0.1", 8000);
server.Start();

Simulator simulator = new ("simulation1.xml");

server.OnMessageReceived += Server_OnMessageReceived;
simulator.OnLogEvent += Simulator_OnLogEvent;
simulator.OnResponseEvent += Simulator_OnErrorEvent;

simulator.AddCommSpec("commonCommSpec",
    new CommandStructure("<", ">", ",:", 0, 1, 2, 3, false, true, new Dictionary<CommandArgType, string>()),
    new ResponseStructure("<", ">", ",", 0, 1, -1, 2, 3, false, true, "valcmd"),
    new ResponseStructure("<", ">", ",", 0, 2, -1, 1, 3, false, true),
    new UniversalCommSpec());
Console.WriteLine($"Command Specification, commonCommSpec was added on port 8000");
// Add more comm specs and ports if direct connection to any stations are required...

simulator.ConnectPassThrough("R1", "127.0.0.1", 12800,
    new CommandStructure("<", ">", ",", 0, 1, 2, 3, true, true, new Dictionary<CommandArgType, string>() 
    { 
        { CommandArgType.EndEffector, "H" } 
    }),
    new ResponseStructure("<", ">", ",", 0, 1, -1, -1, 2, true, true),
    new RobotCommSpec());

Server grpcServer = new()
{
    Services = { LayoutSimulator.BindService(simulator) },
    Ports = { new ServerPort("localhost", 50051, ServerCredentials.Insecure) }
};
// grpcServer.Start();
Console.WriteLine($"gRPC server listening on port 50051");


void Server_OnMessageReceived(object? sender, string e)
{
    simulator.ExecuteCommands_NewThread(e, "commonCommSpec");
}

void Simulator_OnErrorEvent(object? sender, string e)
{
    server.SendMessage(e);
}

void Simulator_OnLogEvent(object? sender, LayoutModels.Support.LogMessage e)
{
    Console.WriteLine($"{DateTime.Now} {e.transactionID}: {e.message}");
}

Console.WriteLine($"Listening to Commands through text input on Command Line (Command Specificatio = commonCommSpec)...");
while (true)
{
    command = Console.ReadLine();
    simulator.ExecuteCommands_NewThread(command, "commonCommSpec");
}

// Unreachable code. Uncomment when not using Command Line input
// await grpcServer.ShutdownAsync();
