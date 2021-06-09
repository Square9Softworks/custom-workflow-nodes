using System;
using System.Data.SqlClient;
using Square9.CustomNode;

namespace SQLSelectQuery
{
    public class SQLSelectQuery : CustomNode
    {
        public override void Run()
        {
            var returnField = "";
            try
            {
                returnField = Settings.GetStringSetting("ReturnField");
            }
            catch(Exception ex)
            {
                Process.SetStatus(ProcessStatus.Errored);
                LogHistory($"Error while getting ReturnField Property: { ex.Message}");
                return;
            }

            try
            {
                SqlConnection connection = new SqlConnection();
                SqlCommand command = new SqlCommand();

                connection.ConnectionString = Settings.GetStringSetting("ConnectionString");

                var sqlQuery = Settings.GetStringSetting("SqlStatement");

                if (Process is ActionProcess actionProcess)
                {
                    sqlQuery = sqlQuery.Replace("#ARCHIVEID#", actionProcess.Document.ArchiveId.ToString());
                    sqlQuery = sqlQuery.Replace("#DOCUMENTID#", actionProcess.Document.DocumentId.ToString());
                    sqlQuery = sqlQuery.Replace("#DOCID#", actionProcess.Document.DocumentId.ToString());
                    sqlQuery = sqlQuery.Replace("#DATABASEID#", actionProcess.Document.DatabaseId.ToString());
                }

                var propertiesList = Process.Properties.GetPropertyNames();
                foreach (var propertyName in propertiesList)
                {
                    var propertyValue = Process.Properties.GetSingleValue(propertyName);

                    if (!String.IsNullOrEmpty(propertyValue))
                        sqlQuery = sqlQuery.Replace($"#{propertyValue.ToUpper()}#", propertyValue);
                }

                command.CommandText = sqlQuery;

                if (connection.ConnectionString != String.Empty && command.CommandText != String.Empty)
                {
                    connection.Open();
                    command.Connection = connection;
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read() && reader[0] != null && !String.IsNullOrEmpty(reader[0].ToString()))
                        {
                            Process.Properties.SetSingleProperty(returnField, reader[0].ToString());
                            LogHistory($"Set Field {returnField} to value {reader[0].ToString()}");
                            SetNextNodeByLinkName("FOUND");
                        }
                        else
                        {
                            SetNextNodeByLinkName("NODATA");
                            LogHistory($"Data for {returnField} Property not found using SQL Statement:{sqlQuery}");
                            return;
                        }                       
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Process.SetStatus(ProcessStatus.Errored);
                LogHistory($"Error Connecting or Querying SQL: { ex.Message}");
                return;
            }
        }
    }
}
