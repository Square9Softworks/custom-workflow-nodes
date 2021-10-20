using System;
using System.Collections.Generic;
using System.IO;
using Renci.SshNet;
using Square9.CustomNode;

namespace SFTPImporter
{
    public class SFTPImport : CaptureImporter
    {
        public override List<string> Import()
        {
            System.Diagnostics.Debugger.Launch();
            //loadconfig
            var defaults = LoadConfig<Defaults>();

            //get settings or use defaults
            string host = Settings.GetStringSetting("Host");
            string username = defaults.User ?? Settings.GetStringSetting("User");
            string password = defaults.Password ?? Settings.GetStringSetting("Password");

            string remoteDirectory = Settings.GetStringSetting("RemoteDirectory");
            if(String.IsNullOrEmpty(defaults.Cache))
                throw new Exception($"Error importing from SFTP Default Cache Directory Not Set");

            List<string> importPaths = new List<string>();
            //connect to sftp server
            try
            {
                using (var sftp = new SftpClient(host, username, password))
                {
                    sftp.Connect();
                    var files = sftp.ListDirectory(remoteDirectory);

                    if (!Directory.Exists(defaults.Cache))
                        Directory.CreateDirectory(defaults.Cache);
                    //loops through available files
                    foreach (var file in files)
                    {
                        string remoteFileName = file.Name;
                        string path = Path.Combine(defaults.Cache + remoteFileName);
                        if ((!file.Name.StartsWith(".")) && ((file.LastWriteTime.Date == DateTime.Today)))

                        using (Stream file1 = File.OpenWrite(path))
                        {
                            //download file
                            sftp.DownloadFile(path, file1);
                            if (File.Exists(path))
                            {
                                //spawn new GC process from file path
                                importPaths.Add(path);
                            }                                
                        }
                    }

                }
                return importPaths;
            } catch(Exception ex)
            {
                //Throw back to the engine if workflow fails import
                throw new Exception($"Error importing from SFTP {host}: {ex.Message}");
            }
            finally
            {
                //clean up cache
                Directory.Delete(defaults.Cache, true);
            }
            
        }
    }

    //config.json object
    class Defaults
    {
        public string User { get; set; }
        public string Password { get; set; }
        public string Cache { get; set; }
    }
}
