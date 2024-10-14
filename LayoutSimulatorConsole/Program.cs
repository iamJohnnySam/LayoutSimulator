using LayoutModels.CommSpecs;
using LayoutModels;
using Communicator;

string? command = null;

Simulator simulator = new Simulator(new CommandStructure(false, null, null, ",", 0, 1, 2, null, 3), new UniversalCommSpec(), "simulation1.xml");



TCPServer server = new TCPServer("127.0.0.1", 8000);
server.Start();
server.OnMessageReceived += Server_OnMessageReceived;

void Server_OnMessageReceived(object? sender, string e)
{
    simulator.ProcessCommands(e);
}


while (true)
{
    command = Console.ReadLine();
    simulator.ProcessCommands(command);
}

