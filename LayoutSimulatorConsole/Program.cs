using LayoutModels.CommSpecs;
using LayoutModels;
using Communicator;

string? command = null;

Simulator simulator = new Simulator(new CommandStructure(false, "<", ">", ",:", 0, 1, 2, string.Empty, 3, false), 
                                    new ResponseStructure("<", ">", ",", 0, 1, -1, 2, false, true), 
                                    new UniversalCommSpec(), 
                                    "simulation1.xml");


TCPServer server = new TCPServer("127.0.0.1", 8000);
server.Start();

server.OnMessageReceived += Server_OnMessageReceived;
simulator.OnLogEvent += Simulator_OnLogEvent;
simulator.OnResponseEvent += Simulator_OnErrorEvent;

void Server_OnMessageReceived(object? sender, string e)
{
    simulator.ProcessCommands(e);
}

void Simulator_OnErrorEvent(object? sender, string e)
{
    server.SendMessage(e);
}

void Simulator_OnLogEvent(object? sender, LayoutModels.Support.LogMessage e)
{
    Console.WriteLine($"{DateTime.Now} {e.transactionID}: {e.message}");
}


while (true)
{
    command = Console.ReadLine();
    simulator.ProcessCommands(command);
}

