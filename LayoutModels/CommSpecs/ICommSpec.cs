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
        Dictionary<String, List<CommandType>> CommandMap { get; set; }
        Dictionary<String, List<CommandArgType>> CommandArgs { get; set; }
        public Dictionary<ResponseTypes, String> ResponseMap { get; set; }
    }
}
