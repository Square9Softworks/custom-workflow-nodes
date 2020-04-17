using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text.RegularExpressions;

using Square9.CustomNode;

namespace SharePointRelease
{
    public class SharePointRelease : CustomNode
    {
        private string ProcessIDText;
        private Boolean EnforceUniqueName = true;
        private Boolean AllowOverwrite = false;

        public Configuration Configuration = new Configuration();

        public override void Run()
        {
            if (Process.ProcessType != ProcessType.GlobalCapture)
            {
                LogHistory("\"Office 365 Connect\" may only be used for GlobalCapture processes.");
                Process.SetStatus(ProcessStatus.Errored);
                return;
            }

            try
            {
                LoadConfiguration();
            }
            catch (Exception ex)
            {
                throw new Exception("Error loading node configuration: " + ex.Message);
            }

            // Always check to ensure the process document is merged before release.
            Process.Document.MergePages();

            SPRelease();
        }

        private void LoadConfiguration()
        {
            Configuration = new Configuration()
            {
                Connection = new Connection()
                {
                    URL = Settings.GetStringSetting("URL"),
                    TargetPath = Settings.GetStringSetting("TargetPath"),
                    InstanceType = InstanceType.SharepointOnline
                },
                Credentials = new Credentials()
                {
                    Domain = Settings.GetStringSetting("Domain"),
                    Username = Settings.GetStringSetting("Username"),
                    Password = Settings.GetStringSetting("Password")
                },
                Output = new Output()
                {
                    FileName = Settings.GetStringSetting("FileName"),
                    CreateFolder = Settings.GetBooleanSetting("CreateFolder"),
                    FolderName = Settings.GetStringSetting("FolderName")
                },
                FieldMapping = new Dictionary<string, string>()
            };

            if (Settings.GetBooleanSetting("OnPremInstance"))
            {
                Configuration.Connection.InstanceType = InstanceType.OnPrem;
            }
            else if (Settings.GetBooleanSetting("OneDriveInstance"))
            {
                Configuration.Connection.InstanceType = InstanceType.OneDrive;
            }
            else
            {
                Configuration.Connection.InstanceType = InstanceType.SharepointOnline;
            }

            var processFields = Settings.GetListSetting("ProcessFields");
            var sharepointNames = Settings.GetListSetting("SharepointNames");

            if (processFields.Count == sharepointNames.Count)
            {
                for (var i = 0; i < processFields.Count; i++)
                {
                    Configuration.FieldMapping.Add(processFields[i], sharepointNames[i]);
                }
            }
            else
            {
                throw new Exception("There must be a Sharepoint Field Name for each Process Field ID provided.");
            }
        }

        private void SPRelease()
        {
            ProcessIDText = "\tProcess: " + Process.Id;

            Console.WriteLine(DateTime.Now.ToString() + ProcessIDText + "\tStarting O365 Release.");

            ICredentials userAuth = Authentication.GetUserAuth(Configuration.Credentials.Username, Configuration.Credentials.Password, 
                Configuration.Connection.InstanceType, Configuration.Credentials.Domain);

            string filePath = Process.Properties.GetSingleValue("FilePath");
            if (!System.IO.File.Exists(filePath))
            {
                String errorMsg = DateTime.Now.ToString() + "\t" + ProcessIDText + "\tUnable to file for upload.";
                Console.WriteLine(errorMsg);
                throw new Exception(errorMsg);
            }

            ClientContext clientContext = new ClientContext(Configuration.Connection.URL);
            clientContext.Credentials = userAuth;

            String title = Configuration.Connection.TargetPath.Trim('/').Split('/')[0];
            String destination = "";

            Folder targetFolder = null;
            Stopwatch stopwatch = new Stopwatch();
            try
            {
                stopwatch.Start();

                string variablePath = "";
                if (!string.IsNullOrEmpty(Configuration.Output.FolderName))
                {
                    variablePath = "/" + Configuration.Output.FolderName.Trim('/');
                }

                destination = "/" + Configuration.Connection.TargetPath.Trim('/') + variablePath + "/";

                targetFolder = EnsureAndGetTargetFolder(clientContext, destination.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).ToList(), Configuration.Output.CreateFolder);

                stopwatch.Stop();

                Console.WriteLine(DateTime.Now.ToString() + ProcessIDText + "\tFolder synchronization - " + stopwatch.ElapsedMilliseconds + "ms");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                String errorMsg = DateTime.Now.ToString() + ProcessIDText + "\tError targeting folder: " + ex.Message;
                Console.WriteLine(errorMsg);
                throw new Exception(errorMsg);
            }

            string fileName = "";
            if (string.IsNullOrEmpty(Configuration.Output.FileName))
            {
                fileName = Process.Properties.GetSingleValue("OriginalFileName");
            }
            else
            {
                fileName = Configuration.Output.FileName;
            }

            int num;
            if (Configuration.Connection.InstanceType == InstanceType.SharepointOnline && (new FileInfo(filePath).Length > 10485760))
            {
                num = this.UploadDocumentByChunks(ref clientContext, filePath, targetFolder, fileName);
            }
            else
            {
                num = this.UploadDocument(ref clientContext, filePath, targetFolder, fileName);
            }

            var byTitle = clientContext.Web.Lists.GetByTitle(title);
            ListItem itemById = byTitle.GetItemById(num);
            var fields = Configuration.FieldMapping;
            foreach (var mappedField in fields)
            {
                try
                {
                    var sharepointFieldValue = Process.Properties.GetSingleValue(mappedField.Key);
                    if (!string.IsNullOrEmpty(sharepointFieldValue))
                    {
                        var sharepointFieldName = mappedField.Value.Replace(" ", "_x0020_");
                        itemById[sharepointFieldName] = sharepointFieldValue;
                    }
                }
                catch (Exception ex2)
                {
                    string value2 = string.Concat(new object[]
                    {
                        DateTime.Now.ToString(),
                        this.ProcessIDText,
                        "\tWarning - Cannot map capture field ",
                        mappedField.Key,
                        " to Sharepoint: ",
                        ex2.Message
                    });
                    Console.WriteLine(value2);
                }
            }
            try
            {
                itemById.Update();
                clientContext.ExecuteQuery();
            }
            catch (Exception ex3)
            {
                string text4 = string.Concat(new object[]
                {
                    DateTime.Now.ToString(),
                    this.ProcessIDText,
                    "\tError updating fields - document with ID ",
                    num,
                    " saved without metadata: ",
                    ex3.Message
                });
                Console.WriteLine(text4);
                throw new Exception(text4);
            }
            clientContext.Dispose();
            Console.WriteLine(string.Concat(new object[]
            {
                DateTime.Now.ToString(),
                this.ProcessIDText,
                "\tEnding Sharepoint release success. Sharepoint document ID: ",
                num
            }));
        }

        private int UploadDocument(ref ClientContext Ctx, string FileSourceURL, Folder destination, string fileName = "")
        {
            int id;
            using (FileStream fileStream = new FileStream(FileSourceURL, FileMode.Open))
            {
                if (String.IsNullOrEmpty(fileName))  //Provide a unique file name
                {
                    fileName = Guid.NewGuid().ToString();
                }
                else
                {
                    Regex pattern = new Regex("[\\/:,*?\"<>|#{}%~&]");  //Remove illegal characters
                    fileName = pattern.Replace(fileName, "_");
                }

                string extension = Path.GetExtension(FileSourceURL);

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                try
                {
                    if (extension.ToUpper() == ".TMP" || extension.ToUpper() == ".DS_STORE")
                    {
                        throw new Exception("Attempting to upload a file with an illegal extension.");
                    }

                    //if (this.currentMapping.Config.InstanceType.ToUpper() == "ONEDRIVE")
                    //{
                    //    //OneDrive requires the literal path for the SaveBinaryDirect calls.
                    //    var uri = new Uri(this.currentMapping.Config.WebURL);
                    //    destination = uri.PathAndQuery + destination;
                    //}

                    string str = "";
                    string serverRelativeUrl = "";
                    int num = 0;

                    try
                    {
                        serverRelativeUrl = fileName + str + extension;
                        bool stepCount = true;
                        if (!EnforceUniqueName)
                            stepCount = false;

                        while (stepCount)
                        {
                            Microsoft.SharePoint.Client.File currentFile = destination.Files.GetByUrl(serverRelativeUrl);
                            Ctx.Load(currentFile);
                            Ctx.ExecuteQuery();

                            num++;
                            str = "_" + num;
                            serverRelativeUrl = fileName + str + extension;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message != "File Not Found.")
                        {
                            String errorMsg = DateTime.Now.ToString() + ProcessIDText + "\tError targeting folder: " + ex.Message;
                            Console.WriteLine(errorMsg);
                            throw new Exception(errorMsg);
                        }
                    }

                    FileCreationInformation fci = new FileCreationInformation();
                    fci.ContentStream = fileStream;
                    fci.Overwrite = AllowOverwrite;
                    fci.Url = serverRelativeUrl;

                    Microsoft.SharePoint.Client.File newFile = destination.Files.Add(fci);

                    Ctx.Load(newFile);
                    Ctx.ExecuteQuery();

                    ListItem listItemAllFields = newFile.ListItemAllFields;
                    Ctx.Load<ListItem>(listItemAllFields, new Expression<Func<ListItem, object>>[0]);
                    Ctx.ExecuteQuery();
                    stopwatch.Stop();

                    Console.WriteLine(DateTime.Now.ToString() + ProcessIDText + "\tFile posted - " + stopwatch.ElapsedMilliseconds + "ms");
                    id = listItemAllFields.Id;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    String errorText = DateTime.Now.ToString() + ProcessIDText + "\tError in upload - " + stopwatch.ElapsedMilliseconds + "ms: " + ex.Message;
                    Console.WriteLine(errorText);
                    throw new Exception(errorText);
                }
            }
            return id;
        }

        private int UploadDocumentByChunks(ref ClientContext Ctx, string FileSourceURL, Folder destination, string fileName = "")
        {
            int? id = null;
            // Each sliced upload requires a unique ID.
            Guid uploadId = Guid.NewGuid();

            if (String.IsNullOrEmpty(fileName))  //Provide a unique file name
            {
                fileName = Guid.NewGuid().ToString();
            }
            else
            {
                Regex pattern = new Regex("[\\/:,*?\"<>|#{}%~&]");  //Remove illegal characters
                fileName = pattern.Replace(fileName, "_");
            }

            string extension = Path.GetExtension(FileSourceURL);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                if (extension.ToUpper() == ".TMP" || extension.ToUpper() == ".DS_STORE")
                {
                    throw new Exception("Attempting to upload a file with an illegal extension.");
                }
                string str = "";
                string serverRelativeUrl = "";
                int num = 0;

                try
                {
                    serverRelativeUrl = fileName + str + extension;
                    bool stepCount = true;
                    if (!EnforceUniqueName)
                        stepCount = false;

                    while (stepCount)
                    {
                        Microsoft.SharePoint.Client.File currentFile = destination.Files.GetByUrl(serverRelativeUrl);
                        Ctx.Load(currentFile);
                        Ctx.ExecuteQuery();

                        num++;
                        str = "_" + num;
                        serverRelativeUrl = fileName + str + extension;
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message != "File Not Found.")
                    {
                        String errorMsg = DateTime.Now.ToString() + ProcessIDText + "\tError targeting folder: " + ex.Message;
                        Console.WriteLine(errorMsg);
                        throw new Exception(errorMsg);
                    }
                }

                //Now to do the upload
                // File object.
                Microsoft.SharePoint.Client.File uploadFile;
                int blockSize = 8388608;
                long fileSize = new FileInfo(FileSourceURL).Length;

                ClientResult<long> bytesUploaded = null;
                try
                {
                    using (FileStream fs = System.IO.File.Open(FileSourceURL, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (BinaryReader br = new BinaryReader(fs))
                        {
                            byte[] buffer = new byte[blockSize];
                            Byte[] lastBuffer = null;
                            long fileoffset = 0;
                            long totalBytesRead = 0;
                            int bytesRead;
                            bool first = true;
                            bool last = false;

                            while ((bytesRead = br.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                totalBytesRead = totalBytesRead + bytesRead;

                                // You've reached the end of the file.
                                if (totalBytesRead == fileSize)
                                {
                                    last = true;
                                    // Copy to a new buffer that has the correct size.
                                    lastBuffer = new byte[bytesRead];
                                    Array.Copy(buffer, 0, lastBuffer, 0, bytesRead);
                                }

                                if (first)
                                {
                                    using (MemoryStream contentStream = new MemoryStream())
                                    {
                                        // Add an empty file.
                                        FileCreationInformation fileInfo = new FileCreationInformation();
                                        fileInfo.ContentStream = contentStream;
                                        fileInfo.Url = serverRelativeUrl;
                                        fileInfo.Overwrite = AllowOverwrite;
                                        uploadFile = destination.Files.Add(fileInfo);

                                        // Start upload by uploading the first slice. 
                                        using (MemoryStream s = new MemoryStream(buffer))
                                        {
                                            // Call the start upload method on the first slice.
                                            bytesUploaded = uploadFile.StartUpload(uploadId, s);
                                            Ctx.ExecuteQuery();
                                            // fileoffset is the pointer where the next slice will be added.
                                            fileoffset = bytesUploaded.Value;
                                        }

                                        // You can only start the upload once.
                                        first = false;
                                    }
                                }
                                else
                                {
                                    // Get a reference to your file.
                                    uploadFile = Ctx.Web.GetFileByServerRelativeUrl(destination.ServerRelativeUrl + Path.AltDirectorySeparatorChar + serverRelativeUrl);// + System.IO.Path.AltDirectorySeparatorChar + uniqueFileName);

                                    if (last)
                                    {
                                        // Is this the last slice of data?
                                        using (MemoryStream s = new MemoryStream(lastBuffer))
                                        {
                                            ListItem listItemAllFields = uploadFile.ListItemAllFields;
                                            Ctx.Load<ListItem>(listItemAllFields, new Expression<Func<ListItem, object>>[0]);
                                            Ctx.ExecuteQuery();

                                            // End sliced upload by calling FinishUpload.
                                            uploadFile = uploadFile.FinishUpload(uploadId, fileoffset, s);
                                            Ctx.ExecuteQuery();

                                            stopwatch.Stop();
                                            Console.WriteLine(DateTime.Now.ToString() + ProcessIDText + "\tFile posted - " + stopwatch.ElapsedMilliseconds + "ms");
                                            id = listItemAllFields.Id;
                                        }
                                    }
                                    else
                                    {
                                        using (MemoryStream s = new MemoryStream(buffer))
                                        {
                                            // Continue sliced upload.
                                            bytesUploaded = uploadFile.ContinueUpload(uploadId, fileoffset, s);
                                            Ctx.ExecuteQuery();
                                            // Update fileoffset for the next slice.
                                            fileoffset = bytesUploaded.Value;
                                        }
                                    }
                                }
                            }// while ((bytesRead = br.Read(buffer, 0, buffer.Length)) > 0)
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }


            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                String errorText = DateTime.Now.ToString() + ProcessIDText + "\tError in upload - " + stopwatch.ElapsedMilliseconds + "ms: " + ex.Message;
                Console.WriteLine(errorText);
                throw new Exception(errorText);
            }

            if (id.HasValue)
            {
                return (int)id;
            }
            throw new Exception();
        }

        private static Folder EnsureAndGetTargetFolder(ClientContext ctx, List<string> folderPath, Boolean CreatePath)
        {
            List list = ctx.Web.Lists.GetByTitle(folderPath[0]);
            Folder returnFolder = list.RootFolder;
            if (folderPath != null && folderPath.Count > 0)
            {
                Web web = ctx.Web;
                Folder currentFolder = list.RootFolder;
                ctx.Load(web, t => t.Url);
                ctx.Load(currentFolder);
                ctx.ExecuteQuery();
                for (Int32 i = 1; i < folderPath.Count; i++)
                {
                    Regex pattern = new Regex("[\\/:,*?\"<>|#{}%~&]");
                    folderPath[i] = pattern.Replace(folderPath[i], "_");

                    FolderCollection folders = currentFolder.Folders;
                    ctx.Load(folders);
                    ctx.ExecuteQuery();

                    bool folderFound = false;
                    foreach (Folder existingFolder in folders)
                    {
                        if (existingFolder.Name.Equals(folderPath[i], StringComparison.InvariantCultureIgnoreCase))
                        {
                            folderFound = true;
                            currentFolder = existingFolder;
                            break;
                        }
                    }

                    if (!folderFound && CreatePath)
                    {
                        ListItemCreationInformation itemCreationInfo = new ListItemCreationInformation();
                        itemCreationInfo.UnderlyingObjectType = FileSystemObjectType.Folder;
                        itemCreationInfo.LeafName = folderPath[i];
                        itemCreationInfo.FolderUrl = currentFolder.ServerRelativeUrl;
                        ListItem folderItemCreated = list.AddItem(itemCreationInfo);
                        folderItemCreated.Update();
                        ctx.Load(folderItemCreated, f => f.Folder);
                        ctx.ExecuteQuery();
                        currentFolder = folderItemCreated.Folder;
                    }
                    else if (!folderFound)
                    {
                        throw new Exception("Folder path not found in the target location.");
                    }
                }


                returnFolder = currentFolder;
            }
            return returnFolder;
        }
    }

    public static class ListExtentions
    {
        public static void CreateFolder(this List list, string name)
        {
            ListItemCreationInformation parameters = new ListItemCreationInformation
            {
                UnderlyingObjectType = FileSystemObjectType.Folder,
                LeafName = name
            };
            ListItem listItem = list.AddItem(parameters);
            listItem["Title"] = name;
            listItem.Update();
        }
    }
}