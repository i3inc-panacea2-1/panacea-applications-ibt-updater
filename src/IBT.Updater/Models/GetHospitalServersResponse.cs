using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace IBT.Updater.Models
{
    

    [DataContract]
    public class CoreFile
    {
        

        [DataMember(Name = "filename")]
        public string FileName { get; set; }

        [DataMember(Name = "version")]
        public string Version { get; set; }


        [DataMember(Name = "hash")]
        public string Hash { get; set; }

        [DataMember(Name = "download_from")]
        public string DownloadPath { get; set; }

        public string LocalPath { get; set; }
    }

    [DataContract]
    public class GetAllVersionsResponse
    {
        [DataMember(Name = "coreVersion")]
        public CoreVersion CoreVersion { get; set; }

        [DataMember(Name = "plugins")]
        public List<Plugin> Plugins { get; set; }
    }

    [DataContract]
    public class CoreVersion
    {
        [DataMember(Name = "version")]
        public string Version { get; set; }
    }

    [DataContract]
    public class Plugin
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "file")]
        public string File { get; set; }

        [DataMember(Name = "version")]
        public string Version { get; set; }
    }
}
