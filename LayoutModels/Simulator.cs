using LayoutModels.CommSpecs;
using LayoutModels.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LayoutModels
{
    public class Simulator
    {
        public event EventHandler<string>? Response;

        public Translator CommSpec { get; set; }
        // public Dictionary<string, Station> Stations { get; set; }
        // public Dictionary<string, Manipulator> Manipulators { get; set; }

        public Simulator(CommandStructure commStruct, ICommSpec commSpec, string xmlPath)
        {
            CommSpec = new Translator(commStruct, commSpec);
            CommSpec.Log += Log;

            XmlDocument simDoc = new XmlDocument();
            simDoc.Load(System.IO.Path.GetFullPath(xmlPath));
            
            foreach(XmlNode node in simDoc.GetElementsByTagName("Station"))
            {
                for (int i = 0; i < (int)node.SelectSingleNode("Count"); i++)
                {

                }
            }
            foreach (XmlNode node in simDoc.GetElementsByTagName("Manipulator"))
            {
                Console.WriteLine(node.InnerText);
            }

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
