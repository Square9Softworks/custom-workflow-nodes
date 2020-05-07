using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace InboxImport
{
    public class RestRequests
    {
        public HttpClient Client;
        public string Token = Guid.Empty.ToString();
        // Any Square 9 API requests that require a token use an empty GUID. This is a requirement to use the workflow engine's API client.
        private JavaScriptSerializer serializer = new JavaScriptSerializer();

        public RestRequests(HttpClient client)
        {
            Client = client;
            Client.DefaultRequestHeaders.Add("User-Agent", $"InboxImport/{Assembly.GetExecutingAssembly().GetName().Version}");
        }

        /// <summary>
        /// Gets all Inboxes for a GlobalSearch instance.
        /// </summary>
        /// <returns></returns>
        public InboxList GetInboxes()
        {
            var builder = new UriBuilder(Client.BaseAddress + "inboxes");
            var query = HttpUtility.ParseQueryString(builder.Query);
            builder.Query = query.ToString();
            var requestUrl = builder.ToString();

            var response = Client.GetAsync(requestUrl).Result;
            var result = response.Content.ReadAsStringAsync().Result;
            if (response.IsSuccessStatusCode)
            {
                return serializer.Deserialize<InboxList>(result);
            }
            else
            {
                throw new Exception("Unable to get Inboxes: " + result);
            }
        }

        /// <summary>
        /// Gets all Archives for a GlobalSearch Database.
        /// </summary>
        /// <param name="DatabaseID"></param>
        /// <param name="ParentArchiveID"></param>
        /// <returns></returns>
        public ArchiveList GetArchives(int DatabaseID, int ParentArchiveID)
        {
            var builder = new UriBuilder(Client.BaseAddress + $"dbs/{DatabaseID}/archives/{ParentArchiveID}");
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["token"] = this.Token;
            builder.Query = query.ToString();
            var requestUrl = builder.ToString();

            var response = Client.GetAsync(requestUrl).Result;
            var result = response.Content.ReadAsStringAsync().Result;
            if (response.IsSuccessStatusCode)
            {
                var archives = serializer.Deserialize<ArchiveList>(result);

                if (archives != null && archives.Archives != null && archives.Archives.Count > 0)
                {
                    for (int i = archives.Archives.Count - 1; i >= 0; i--)
                    {
                        if ((archives.Archives[i].Properties & 2) == 2)
                        {
                            archives.Archives.InsertRange(i + 1, GetArchives(DatabaseID, archives.Archives[i].Id).Archives);
                        }
                    }
                }

                return archives;
            }
            else
            {
                throw new Exception("Unable to get Archives: " + result);
            }
        }

        /// <summary>
        /// Uploads a file to the GlobalSearch server.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public string PostFile(string file)
        {
            var builder = new UriBuilder(Client.BaseAddress + $"files");
            var query = HttpUtility.ParseQueryString(builder.Query);
            builder.Query = query.ToString();
            var requestUrl = builder.ToString();

            var fileContent = new MultipartFormDataContent();
            fileContent.Add(new ByteArrayContent(System.IO.File.ReadAllBytes(file)), Path.GetFileNameWithoutExtension(file), Path.GetFileName(file));

            var response = Client.PostAsync(requestUrl, fileContent).Result;
            var result = response.Content.ReadAsStringAsync().Result;
            if (response.IsSuccessStatusCode)
            {
                var uploadedFile = serializer.Deserialize<FilesList>(result).Files.FirstOrDefault();
                return uploadedFile.Name;
            }
            else
            {
                throw new Exception("Unable to upload file: " + result);
            }
        }

        /// <summary>
        /// Indexes an uploaded file to a GlobalSearch Archive.
        /// </summary>
        /// <param name="DatabaseID"></param>
        /// <param name="ArchiveID"></param>
        /// <param name="UploadedFileName"></param>
        /// <param name="Session"></param>
        /// <returns></returns>
        public KeyValuePair<int, string> IndexDocument(int DatabaseID, int ArchiveID, string UploadedFileName, KeyValuePair<Guid, string> Session)
        {
            var builder = new UriBuilder(Client.BaseAddress + $"dbs/{DatabaseID}/archives/{ArchiveID}");
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["token"] = this.Token;
            query["validateallfields"] = false.ToString();
            query["Session-ID"] = Session.Key.ToString();
            query["Session-Actions"] = Session.Value;
            builder.Query = query.ToString();
            var requestUrl = builder.ToString();

            var indexData = new Indexer();
            indexData.Files.Add(new File(UploadedFileName));

            var indexContent = new StringContent(serializer.Serialize(indexData), Encoding.UTF8, "application/json");

            var response = Client.PostAsync(requestUrl, indexContent).Result;
            if (response.IsSuccessStatusCode)
            {
                var docHashHeaderString = response.Headers.GetValues("Doc-Hashes").FirstOrDefault() 
                    ?? throw new Exception("Unable to retrieve document hash from import response.");
                var docHashHeaderArr = docHashHeaderString.Split(',');
                if (int.TryParse(docHashHeaderArr[0], out var docId))
                {
                    return new KeyValuePair<int, string>(docId, docHashHeaderArr[1]);
                }

                throw new FormatException($"Unable to convert the doc id value {docHashHeaderArr[0]} to an int.");
            }
            else
            {
                var error = response.Content.ReadAsStringAsync().Result;
                throw new Exception("Unable to index document: " + error);
            }
        }
    }
}