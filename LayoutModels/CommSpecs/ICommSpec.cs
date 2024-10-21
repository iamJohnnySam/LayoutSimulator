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
        Dictionary<string, List<CommandType>> CommandMap { get; set; }
        Dictionary<string, List<CommandArgType>> CommandArgs { get; set; }
        Dictionary<ResponseType, String> ResponseMap { get; set; }
        Dictionary<string, string> StationMapping { get; set; }
    }
}
