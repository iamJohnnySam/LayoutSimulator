using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutModels.CommSpecs
{
    public struct CommandStructure (bool dedicatedPort, string startChar, string endChar, string delimiter, int indexTransaction, int indexCommand, int indexTarget, string fixedTarget, int indexValueStart, bool checkSum)
    {
        public bool DedicatedPort { get; set; } = dedicatedPort;
        public string StartCharacter { get; set; } = startChar;
        public string EndCharacter { get; set;} = endChar;
        public string Delimiter { get; set;} = delimiter;
        public int IndexTransaction { get; set; } = indexTransaction;
        public int IndexCommand { get; set; } = indexCommand;
        public int IndexTarget { get; set; } = indexTarget;
        public string FixedTarget { get; set; } = fixedTarget;
        public int IndexValueStart { get; set;} = indexValueStart;
        public bool CheckSum { get; set; } = checkSum;
    }

    public struct ResponseStructure(string startChar, string endChar, string delimiter, int indexTransaction, int indexMessage, int indexTarget, int originalCommandIndex, int indexResponseStart, bool checkSum, bool cRLF)
    {
        public string StartCharacter { get; set; } = startChar;
        public string EndCharacter { get; set; } = endChar;
        public string Delimiter { get; set; } = delimiter;
        public int IndexTransaction { get; set; } = indexTransaction;
        public int IndexMessage { get; set; } = indexMessage;
        public int IndexTarget { get; set; } = indexTarget;
        public int OriginalCommandIndex { get; set; } = originalCommandIndex;
        public int IndexResponseStart { get; set; } = indexResponseStart;
        public bool CheckSum { get; set; } = checkSum;
        public bool CRLF { get; set; } = cRLF;
    }
}
