using Microsoft.SharePoint.Client;
using System;
using System.Net;
using System.Security;

namespace SharePointRelease
{
    public static class Authentication
    {
        public static ICredentials GetUserAuth(string Username, string Password, string InstanceType, string Domain = "")
        {
            ICredentials result;
            try
            {
                bool flag = InstanceType.ToUpper() == "SHAREPOINTONLINE" || InstanceType.ToUpper() == "ONEDRIVE";
                if (flag)
                {
                    SecureString secureString = new SecureString();
                    for (int i = 0; i < Password.Length; i++)
                    {
                        char c = Password[i];
                        secureString.AppendChar(c);
                    }
                    result = new SharePointOnlineCredentials(Username, secureString);
                }
                else
                {
                    result = new NetworkCredential(Username, Password, Domain);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(DateTime.Now.ToString() + "\tError in credential builder: " + ex.Message);
            }
            return result;
        }

        public static string Connect(string URL, string Path, string Instance, string User, string Pass, string Domain = "")
        {
            string result;
            try
            {
                ICredentials userAuth = Authentication.GetUserAuth(User, Pass, Instance, Domain);
                ClientContext clientContext = new ClientContext(URL);
                clientContext.Credentials = userAuth;
                string title = Path.Trim(new char[]
                {
                    '/'
                }).Split(new char[]
                {
                    '/'
                })[0];

                Web web = clientContext.Web;

                // Retrieve all lists from the server. 
                clientContext.Load(web.Lists,
                             lists => lists.Include(list => list.Title, // For each list, retrieve Title and Id. 
                                                    list => list.Id));

                // Execute query. 
                clientContext.ExecuteQuery();

                // Enumerate the web.Lists. 
                String strList = "";
                foreach (List list in web.Lists)
                {
                    strList = strList + ", " + list.Title;
                }



                clientContext.Web.Lists.GetByTitle(title);
                clientContext.ExecuteQuery();
                result = "Connection Successful!";
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }
            return result;
        }
    }
}