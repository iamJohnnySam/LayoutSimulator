using LayoutModels.CommSpecs;
using LayoutModels.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels
{
    public class Simulator
    {
        public event EventHandler<string>? Response;

        public Translator CommSpec { get; set; }

        public Simulator(CommandStructure commStruct, ICommSpec commSpec)
        {
            CommSpec = new Translator(commStruct, commSpec);
            CommSpec.Log += Log;
        }

        public void ProcessCommands(string command)
        {
            try
            {
                List<Job> commands = CommSpec.Translate(command);
                new Thread(() => ExecuteCommands(commands)).Start();
            }
            catch (NackResponse) 
            { 
                // TODO:
            }
        }

        private void ExecuteCommands(List<Job> commands)
        {
            foreach (var command in commands) 
            { 
                try
                {
                    switch(command.Action)
                    {
                        case CommandTypes.PICK:
                            break;
                        case CommandTypes.PLACE:
                            break;
                        case CommandTypes.DOOR:
                            break;
                        case CommandTypes.MAP:
                            break;
                        case CommandTypes.DOCK:
                            break;
                        case CommandTypes.UNDOCK:
                            break;
                        case CommandTypes.PROCESS:
                            break;
                        case CommandTypes.POWER:
                            break;
                        case CommandTypes.HOME:
                            break;
                    }
                }
                catch (NackResponse)
                {

                }
                catch (ErrorResponse)
                {

                }
            }
        }

        private void Log(object? sender, Support.LogMessage e)
        {
            // TODO: Remove console
            Console.WriteLine(e.message);
        }
    }
}
