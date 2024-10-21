using LayoutCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels.CommSpecs
{
    public struct CommandStructure
    {
        public bool DedicatedPort { get; set; } 
        public string StartCharacter { get; set; }
        public string EndCharacter { get; set;}
        public string Delimiter { get; set;} 
        public int IndexTransaction { get; set; } 
        public int IndexCommand { get; set; } 
        public int IndexTarget { get; set; } 
        public string FixedTarget { get; set; } 
        public int IndexValueStart { get; set;} 
        public bool CheckSum { get; set; }
        public bool CRLF { get; set; }
        public Dictionary<CommandArgType, string> Prefixs { get; set; }

        public CommandStructure(string startChar, string endChar, string delimiter, int indexTransaction, int indexCommand, int indexTarget, int indexValueStart, bool checkSum, bool cRLF, Dictionary<CommandArgType, string> prefixs)
        {
            DedicatedPort = false;
            StartCharacter = startChar;
            EndCharacter = endChar;
            Delimiter = delimiter;
            IndexTransaction = indexTransaction;
            IndexCommand = indexCommand;
            IndexTarget = indexTarget;
            FixedTarget = string.Empty;
            IndexValueStart = indexValueStart;
            CheckSum = checkSum;
            CRLF = cRLF;
            Prefixs = prefixs;
        }

        public CommandStructure(string startChar, string endChar, string delimiter, int indexTransaction, int indexCommand, string fixedTarget, int indexValueStart, bool checkSum, bool cRLF, Dictionary<CommandArgType, string> prefixs)
        {
            DedicatedPort = true;
            StartCharacter = startChar;
            EndCharacter = endChar;
            Delimiter = delimiter;
            IndexTransaction = indexTransaction;
            IndexCommand = indexCommand;
            IndexTarget = -1;
            FixedTarget = fixedTarget;
            IndexValueStart = indexValueStart;
            CheckSum = checkSum;
            CRLF = cRLF;
            Prefixs = prefixs;
        }
    }

    public struct ResponseStructure
    {
        public string StartCharacter { get; set; }
        public string EndCharacter { get; set; }
        public string Delimiter { get; set; }
        public int IndexTransaction { get; set; }
        public int IndexMessage { get; set; }
        public int IndexTarget { get; set; }
        public int OriginalCommandIndex { get; set; }
        public int IndexResponseStart { get; set; }
        public bool CheckSum { get; set; }
        public bool CRLF { get; set; }
        public string InjectAckResponse { get; set; }

        public ResponseStructure(string startChar, string endChar, string delimiter, int indexTransaction, int indexMessage, int indexTarget, int originalCommandIndex, int indexResponseStart, bool checkSum, bool cRLF, string injectAckResponse)
        {
            StartCharacter = startChar;
            EndCharacter = endChar;
            Delimiter = delimiter;
            IndexTransaction = indexTransaction;
            IndexMessage = indexMessage;
            IndexTarget = indexTarget;
            OriginalCommandIndex = originalCommandIndex;
            IndexResponseStart = indexResponseStart;
            CheckSum = checkSum;
            CRLF = cRLF;
            InjectAckResponse = injectAckResponse;
        }

        public ResponseStructure(string startChar, string endChar, string delimiter, int indexTransaction, int indexMessage, int indexTarget, int originalCommandIndex, int indexResponseStart, bool checkSum, bool cRLF)
        {
            StartCharacter = startChar;
            EndCharacter = endChar;
            Delimiter = delimiter;
            IndexTransaction = indexTransaction;
            IndexMessage = indexMessage;
            IndexTarget = indexTarget;
            OriginalCommandIndex = originalCommandIndex;
            IndexResponseStart = indexResponseStart;
            CheckSum = checkSum;
            CRLF = cRLF;
            InjectAckResponse = string.Empty;
        }
    }
}
