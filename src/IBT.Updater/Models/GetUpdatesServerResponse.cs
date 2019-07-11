using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace IBT.Updater.Models
{

   
    /*
      "__v": 1,
            "_id": "55951c00de3ae0b45e160348",
            "name": "test2",
            "timestamp": 1435835392769,
            "updates": [
                {
                    "patchScript": "files/terminalUpdates/55951fa249a3fbf567a7706f/wtf.cmd",
                    "description": "asdasdasdasd",
                    "name": ".NET update 4.5.0 to 4.5.1",
                    "_id": "55951fa249a3fbf567a7706f",
                    "is_deleted": false,
                    "requiresReboot": false,
                    "requiredFiles": [
                        "files/terminalUpdates/55951fa249a3fbf567a7706f/requiredFiles/omg.dll",
                        "files/terminalUpdates/55951fa249a3fbf567a7706f/requiredFiles/rofl.dll"
                    ],
                    "__v": 0
                },
                {
                    "patchScript": "files/terminalUpdates/559520363cc9ec5468bb5173/wtf.cmd",
                    "description": "asdasdasdasdasdasdasdasd",
                    "name": "test test test",
                    "_id": "559520363cc9ec5468bb5173",
                    "is_deleted": false,
                    "requiresReboot": false,
                    "requiredFiles": [],
                    "__v": 0
                }
            ]
     * */

    [DataContract]
    public class UpdatePackage
    {
        [DataMember(Name = "_id")]
        public string Id { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "timestamp")]
        public long Timestamp { get; set; }

        [DataMember(Name = "updates")]
        public List<Update> Updates { get; set; }

    }



    [DataContract]
    public class Update
    {

        [DataMember(Name = "_id")]
        public string Id { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "patchScript")]
        public string PatchScript { get; set; }

        [DataMember(Name = "requiresReboot")]
        public string RequiresReboot { get; set; }

        [DataMember(Name = "requiredFiles")]
        public List<string> RequiredFiles { get; set; }

        [DataMember(Name = "installed")]
        public bool Installed { get; set; }

        [DataMember(Name = "notifiedServer")]
        public bool NotifiedServer { get; set; }

        [DataMember(Name = "exitCode")]
        public int ExitCode { get; set; }

        public string PackageId { get; set; }
        
    }
}
