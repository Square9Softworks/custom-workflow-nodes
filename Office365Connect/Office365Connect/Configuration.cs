using System.Collections.Generic;

namespace SharePointRelease
{
    public class Configuration
    {
        public Connection Connection { get; set; }
        public Credentials Credentials { get; set; }
        public Output Output { get; set; }
        public Dictionary<string, string> FieldMapping { get; set; }
    }

    public class Connection
    {
        public string URL { get; set; }
        public string TargetPath { get; set; }
        public InstanceType InstanceType { get; set; }
    }

    public class Credentials
    {
        public string Domain { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class Output
    {
        public string FileName { get; set; }
        public bool CreateFolder { get; set; }
        public string FolderName { get; set; }
    }

    public enum InstanceType
    {
        SharepointOnline,
        OneDrive,
        OnPrem
    }
}
