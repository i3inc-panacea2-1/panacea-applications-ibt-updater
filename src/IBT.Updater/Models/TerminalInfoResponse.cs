using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace IBT.Updater.Models
{
    [DataContract]
    public class TerminalInfoResponse
    {
        [DataMember(Name = "terminalName")]
        public string TerminalName { get; set; }
    }
}
