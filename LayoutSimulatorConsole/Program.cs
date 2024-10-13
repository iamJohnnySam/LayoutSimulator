using LayoutModels.CommSpecs;
using LayoutModels;

string? command = null;

Simulator simulator = new Simulator(new CommandStructure(false, null, null, ",", 0, 1, 2, null, 3), new UniversalCommSpec());



while (command == null)
{
    command = Console.ReadLine();
}


Console.ReadLine();
