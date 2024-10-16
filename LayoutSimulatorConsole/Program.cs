﻿using LayoutModels.CommSpecs;
using LayoutModels;
using Communicator;
using LayoutCommands;
using Grpc.Core;

string? command = null;

TCPServer server = new TCPServer("127.0.0.1", 8000);
server.Start();


Simulator simulator = new Simulator(new CommandStructure(false, "<", ">", ",:", 0, 1, 2, string.Empty, 3, false), 
                                    new ResponseStructure("<", ">", ",", 0, 1, -1, 2, 3, false, true, "valcmd"),
                                    new ResponseStructure("<", ">", ",", 0, 2, -1, 1, 3, false, true, ""), 
                                    new UniversalCommSpec(), 
                                    "simulation1.xml");


server.OnMessageReceived += Server_OnMessageReceived;
simulator.OnLogEvent += Simulator_OnLogEvent;
simulator.OnResponseEvent += Simulator_OnErrorEvent;



Server grpcServer = new Server
{
    Services = { LayoutSimulator.BindService(simulator) },
    Ports = { new ServerPort("localhost", 50051, ServerCredentials.Insecure) }
};
grpcServer.Start();
Console.WriteLine($"gRPC server listening on port 50051");


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

await grpcServer.ShutdownAsync();
