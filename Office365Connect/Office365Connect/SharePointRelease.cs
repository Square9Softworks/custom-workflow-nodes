using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace SharePointRelease
{
    public class SharePointRelease
    {
        private string processFieldValues;

        private string mappingFile = "";

        private string processId;

        private PropertyMapping currentMapping = null;

        private Boolean enforceUniqueName = true;
        private Boolean allowOverwrite = false;

        public Dictionary<string, string> RunCallAssembly(Dictionary<string, string> Input)
        {
            this.processFieldValues = "";
            this.mappingFile = "";
            try
            {
                foreach (KeyValuePair<string, string> current in Input)
                {
                    bool flag = current.Value != null && current.Value.Contains("SPXML_");
                    if (flag)
                    {
                        this.mappingFile = Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), current.Value.Substring(6) + ".xml");
                        bool flag2 = !System.IO.File.Exists(this.mappingFile);
                        if (flag2)
                        {
                            string text = DateTime.Now.ToString() + "Mapping file not found at " + this.mappingFile;
                            Console.WriteLine(text);
                            throw new Exception(text);
                        }
                        XmlSerializer xmlSerializer = new XmlSerializer(typeof(PropertyMapping));
                        using (StreamReader streamReader = new StreamReader(this.mappingFile))
                        {
                            this.currentMapping = (PropertyMapping)xmlSerializer.Deserialize(streamReader);
                            streamReader.Close();
                        }
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(current.Value) && current.Value.ToLower() == "bypassuniqueconstraint")
                            enforceUniqueName = false;
                        else if (!String.IsNullOrEmpty(current.Value) && current.Value.ToLower() == "allowoverwrite")
                            allowOverwrite = true;

                        this.processFieldValues = string.Concat(new string[]
                        {
                            this.processFieldValues,
                            current.Key,
                            ":",
                            current.Value,
                            "\t"
                        });
                    }
                }

                if (String.IsNullOrEmpty(this.mappingFile))
                {
                    string text2 = DateTime.Now.ToString() + "\tRelease attempted, no SPXML_ property found.\t" + this.processFieldValues;
                    Console.WriteLine(text2);
                    throw new Exception(text2);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Property mapping error: " + ex.Message);
            }
            this.SPRelease(Input);
            return new Dictionary<string, string>();
        }

        private void SPRelease(Dictionary<string, string> Input)
        {
            try
            {
                bool flag = !string.IsNullOrEmpty(this.currentMapping.Config.ProcessIdField);
                if (flag)
                {
                    this.processId = "\tProcess:" + Input[this.currentMapping.Config.ProcessIdField];
                }
                else
                {
                    this.processId = "";
                }
            }
            catch
            {
                Console.WriteLine(DateTime.Now.ToString() + "\tWarning - No process ID specified for Sharepoint logging.  Consider adding a field for storing process id to your workflow.");
                this.processId = "";
            }

            Console.WriteLine(DateTime.Now.ToString() + processId + "\tStarting O365 Release.");
            string password = Encrypt.DecryptString(this.currentMapping.Config.Password, "Infinet S9");
            ICredentials userAuth = Authentication.GetUserAuth(this.currentMapping.Config.Username, password, this.currentMapping.Config.InstanceType, this.currentMapping.Config.Domain);
            string text = Input["-1"];

            if (!System.IO.File.Exists(text))
            {
                String errorMsg = DateTime.Now.ToString() + "\t" + processId + "\tUnable to file for upload.";
                Console.WriteLine(errorMsg);
                throw new Exception(errorMsg);
            }

            ClientContext clientContext = new ClientContext(this.currentMapping.Config.WebURL);
            clientContext.Credentials = userAuth;

            String title = currentMapping.Config.DestinationURL.Trim('/').Split('/')[0];
            String destination = "";


            Folder targetFolder = null;
            Stopwatch stopwatch = new Stopwatch();
            try
            {
                stopwatch.Start();

                String variablePath = "";
                if (currentMapping.Config.FolderField != "")
                {
                    variablePath = "/" + Input[currentMapping.Config.FolderField].Trim('/');
                }

                destination = "/" + currentMapping.Config.DestinationURL.Trim('/') + variablePath + "/";

                targetFolder = EnsureAndGetTargetFolder(clientContext, destination.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).ToList(), currentMapping.Config.FolderCreation);

                stopwatch.Stop();

                Console.WriteLine(DateTime.Now.ToString() + processId + "\tFolder synchronization - " + stopwatch.ElapsedMilliseconds + "ms");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                String errorMsg = DateTime.Now.ToString() + processId + "\tError targeting folder: " + ex.Message;
                Console.WriteLine(errorMsg);
                throw new Exception(errorMsg);
            }

            String fileName;
            if (String.IsNullOrEmpty(currentMapping.Config.FileNameField))
            {
                fileName = "";
            }
            else
            {
                fileName = Input[currentMapping.Config.FileNameField];
            }

            int num;
            if (this.currentMapping.Config.InstanceType.Equals("SHAREPOINTONLINE") && (new FileInfo(text).Length > 10485760))
            {
                num = this.UploadDocumentByChunks(ref clientContext, text, targetFolder, fileName);
            }
            else
            {
                num = this.UploadDocument(ref clientContext, text, targetFolder, fileName);
            }


            List byTitle;
            byTitle = clientContext.Web.Lists.GetByTitle(title);
            ListItem itemById = byTitle.GetItemById(num);
            PropertyMappingField[] fields = this.currentMapping.Fields;
            for (int i = 0; i < fields.Length; i++)
            {
                PropertyMappingField propertyMappingField = fields[i];
                try
                {
                    if (!string.IsNullOrEmpty(Input[propertyMappingField.ID.ToString()]))
                    {
                        propertyMappingField.Value = propertyMappingField.Value.Replace(" ", "_x0020_");
                        itemById[propertyMappingField.Value] = Input[propertyMappingField.ID.ToString()];
                    }
                }
                catch (Exception ex2)
                {
                    string value2 = string.Concat(new object[]
                    {
                        DateTime.Now.ToString(),
                        this.processId,
                        "\tWarning - Cannot map capture field ",
                        propertyMappingField.ID,
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
                    this.processId,
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
                this.processId,
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
                        if (!enforceUniqueName)
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
                            String errorMsg = DateTime.Now.ToString() + processId + "\tError targeting folder: " + ex.Message;
                            Console.WriteLine(errorMsg);
                            throw new Exception(errorMsg);
                        }
                    }

                    FileCreationInformation fci = new FileCreationInformation();
                    fci.ContentStream = fileStream;
                    fci.Overwrite = allowOverwrite;
                    fci.Url = serverRelativeUrl;

                    Microsoft.SharePoint.Client.File newFile = destination.Files.Add(fci);

                    Ctx.Load(newFile);
                    Ctx.ExecuteQuery();

                    ListItem listItemAllFields = newFile.ListItemAllFields;
                    Ctx.Load<ListItem>(listItemAllFields, new Expression<Func<ListItem, object>>[0]);
                    Ctx.ExecuteQuery();
                    stopwatch.Stop();

                    Console.WriteLine(DateTime.Now.ToString() + processId + "\tFile posted - " + stopwatch.ElapsedMilliseconds + "ms");
                    id = listItemAllFields.Id;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    String errorText = DateTime.Now.ToString() + processId + "\tError in upload - " + stopwatch.ElapsedMilliseconds + "ms: " + ex.Message;
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
                    if (!enforceUniqueName)
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
                        String errorMsg = DateTime.Now.ToString() + processId + "\tError targeting folder: " + ex.Message;
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
                                        fileInfo.Overwrite = allowOverwrite;
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
                                    uploadFile = Ctx.Web.GetFileByServerRelativeUrl(destination.ServerRelativeUrl + System.IO.Path.AltDirectorySeparatorChar + serverRelativeUrl);// + System.IO.Path.AltDirectorySeparatorChar + uniqueFileName);

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
                                            Console.WriteLine(DateTime.Now.ToString() + processId + "\tFile posted - " + stopwatch.ElapsedMilliseconds + "ms");
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
                String errorText = DateTime.Now.ToString() + processId + "\tError in upload - " + stopwatch.ElapsedMilliseconds + "ms: " + ex.Message;
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