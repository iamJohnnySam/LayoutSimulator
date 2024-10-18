using LayoutModels.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LayoutCommands;

namespace LayoutModels.CommSpecs
{
    public interface ICommSpec
    {
        Dictionary<String, List<CommandTypes>> CommandMap { get; set; }
        Dictionary<String, List<CommandArgTypes>> CommandArgs { get; set; }
        public Dictionary<ResponseTypes, String> ResponseMap { get; set; }
    }
}
