using System;
using System.Linq;
using System.Xml;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;

/*  The following settings in the App Settings must be set.  This should work fine on local storage,
 *  as long as the Storage Emulator and local SQL Server is up and running.
 * 
 *  CloudTableConnection - Connection String - either "UseDevelopmentStorage=true" for Local storage, 
 *      or the connection string for an existing Cloud Storage Account
 *  ApplicationName - string - value="name of the application": This is used as the partition key to 
 *      isolate logs of different applications using the same storage.
 *  All_LedgerLevel - string - value="-1": This can be set to levels that act to allow only errors 
 *      at or above a certain value.  If set to -1, that means all.
 */
namespace Coolftc.Sourcelinks.Utilities
{
    /// <summary>
    /// This class represents the data model of our Table.  It will be used to create the scheme and populate
    /// individual records.  Note the key can be completely determined internally because the Log is unique
    /// to each application and entries are time ordered.
    /// </summary>
    public class LogLedger : TableEntity
    {
        public LogLedger()
        {
            PartitionKey = ServiceLedger.ApplicationName;
            RowKey = String.Format("{0:10}", (DateTime.MaxValue.Ticks - DateTime.Now.Ticks));
        }
        public LogLedger(string key)
        {
            PartitionKey = ServiceLedger.ApplicationName;
            RowKey = key;
            Severity = 0;
            Message = "";
        }
        public LogLedger(int severity, string message)
        {
            PartitionKey = ServiceLedger.ApplicationName;
            RowKey = String.Format("{0:10}", (DateTime.MaxValue.Ticks - DateTime.Now.Ticks));
            Severity = severity;
            Message = message;
        }
        public int Severity { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// This class supports certain constants, configuration values and static methods that make is easy to 
    /// place an entry into the log table and delete an entry.  Note that both entry and delete only apply
    /// to the records of this application.  This is not a GLOBAL delete, each application supports its own delete.
    /// Creating new Audit records in the SQL table is also supported in this class.
    /// </summary>
    static public class ServiceLedger
    {
        public enum SEV_CODES
        {
            SEV_EXCEPTION = 1,    // Serious Problem
            SEV_ALERT = 3,        // Item to take note of
            SEV_INFO = 5,         // Informational message
            SEV_DEBUG = 7         // Debug messages
        };
        // Per MSFT May 2009 - Due to a known performance issue with the ADO.NET Data Services client library, 
        // it is recommended that you use the table name for the class definition (which I have done here).
        private static readonly string m_LogTableName = "LogLedger";
        private static readonly string m_AppName = "UnknownApplication";
        private static readonly string m_connectSettingName = "CloudTableConnection";
        private static readonly int MAX_SEV = -1;
        private static readonly object HelperLock = new Object();

        static ServiceLedger()
        {
            try
            {
                MAX_SEV = Convert.ToInt32(Environment.GetEnvironmentVariable("All_LedgerLevel"));
            }
            catch { MAX_SEV = -1; }
            try
            {
                m_AppName = Environment.GetEnvironmentVariable("ApplicationName");
            }
            catch { m_AppName = "UnknownApplication"; }
            // Check if TABLE exists and create if not.  Should be done rarely (e.g. first run or after a table delete), but when
            // it needs to be done, it really needs to be done.  This static constructor should be a reasonable compromise. 
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            CloudTable table = storageAccount.CreateCloudTableClient().GetTableReference(LogTableName);
            table.CreateIfNotExistsAsync();
        }

        /// <summary>
        /// Place the entry into the log.
        /// </summary>
        static public void Entry(ClassExp.EXP_CODES code, SEV_CODES sev, string msg, string loc) { Entry(code, sev, msg, loc, ""); }
        static public void Entry(ClassExp.EXP_CODES code, SEV_CODES sev, string msg, string loc, string who)
        {
            lock (HelperLock)
            {
                try
                {
                    if (MAX_SEV.Equals(-1) || MAX_SEV >= Convert.ToInt32(sev))
                    {
                        string fmtMsg = "";
                        if (code != ClassExp.EXP_CODES.EXP_OK) fmtMsg += code.ToString() + ": ";
                        if (loc.Length > 0) fmtMsg += " : (- " + loc + " -) ";
                        if (who.Length > 0) fmtMsg += " : (id-" + who + " -) ";
                        fmtMsg += msg;
                        LogLedger entry = new LogLedger(Convert.ToInt32(sev), fmtMsg);
                        /* Write to Database */
                        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                        CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                        CloudTable table = tableClient.GetTableReference(LogTableName);
                        TableOperation addOperation = TableOperation.Insert(entry);
                        table.ExecuteAsync(addOperation);
                    }
                }
                catch { }// Not much recourse if this fails. 
            }
        }

        /// <summary>
        /// Read all log records that fall within a given date. Returns an IEnumeration that can be used to get the actual
        /// data. Use a "foreach" to read the actual data, and it will automatically only pull down the chunks it needs.
        /// Note: The DateTime Min and Max dates are not valid inputs and will cause an exception.
        /// </summary>
        static public IEnumerable<LogLedger> Read(DateTimeOffset start, DateTimeOffset end)
        {
            lock (HelperLock)
            {
                try
                {
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    CloudTable table = tableClient.GetTableReference(LogTableName);

                    string filterPartitionKey = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, ServiceLedger.ApplicationName);
                    string filterTimestamp = TableQuery.CombineFilters(TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThan, start),
                                             TableOperators.And,
                                             TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.LessThan, end));
                    string filterCombined = TableQuery.CombineFilters(filterPartitionKey, TableOperators.And, filterTimestamp);

                    TableQuery<LogLedger> query = new TableQuery<LogLedger>().Where(filterCombined);
                    return (IEnumerable<LogLedger>)table.ExecuteQuerySegmentedAsync(query, null);
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = ParseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "ServiceLedger.Read(ByDate)", realMsg);
                }
            }
        }

        /// <summary>
        /// This will wipe the Log table completely.  This is handy if some out of control error condition 
        /// decides to flood your log table.  The table will be out of commission for a while after this
        /// method is used, but the table will be recreated by the constructor when the delete is done.
        /// ** If you call this method, you will need to restart the service, as this is a static class
        /// ** and (re)creating the table will only happen in this class' constructor.
        /// </summary>
        static public long Delete()
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                CloudTable table = storageAccount.CreateCloudTableClient().GetTableReference(LogTableName);
                table.DeleteIfExistsAsync();
                return 1;
            }
            catch (Exception)
            {
                // Did not seem to work 
                return 0;
            }
        }

        /// <summary>
        /// Remove a Log Record.
        /// </summary>
        static public void Delete(string key)
        {
            lock (HelperLock)
            {
                try
                {
                    LogLedger entry = new LogLedger(key);
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    CloudTable table = tableClient.GetTableReference(LogTableName);
                    TableOperation delOperation = TableOperation.Delete(entry);
                    table.ExecuteAsync(delOperation);
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = ParseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "ServiceLedger.Delete", realMsg);
                }
            }
        }

        /// <summary>
        /// Remove any Log Records in the date range.
        /// </summary>
        static public long Delete(DateTimeOffset start, DateTimeOffset end)
        {
            IEnumerable<LogLedger> backing = ServiceLedger.Read(start, end);
            long total = backing.Count();
            lock (HelperLock)
            {
                try
                {
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                    TableBatchOperation batchOperation = new TableBatchOperation();
                    foreach (LogLedger entry in backing)
                    {
                        batchOperation.Delete(entry);
                    }
                    CloudTable table = tableClient.GetTableReference(LogTableName);
                    table.ExecuteBatchAsync(batchOperation);
                    return total;
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = ParseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "ServiceLedger.Delete(ByDate)", realMsg);
                }
            }
        }

        static public string ApplicationName { get { return m_AppName; } }

        static public string LogTableName { get { return m_LogTableName; } }

        static public string CONNECT_SET_NAME { get { return m_connectSettingName; } }

        /// <summary>
        /// Increment a record in the Count table.
        /// The Count table works by writing a record with a time stamp as an increment.  Then one can 
        /// just do a SQL count with a date range on the counter type to find the total.
        /// </summary>
        /// 
        public enum CNT_CODES
        {
            CNT_WT_LOG = 1,         // An entry was written to the log
            CNT_WT_SENT = 10,       // A prompt was sent
            CNT_WT_VALID = 11,      // A validation was sent
            CNT_WT_INVITE = 12,     // An invitation was sent
            CNT_WT_CONFIRM = 13,    // A confirmation was sent
        };

        static private string ParseTSErr(string msg)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                xml.LoadXml(msg);
                string code = xml.GetElementsByTagName("code")[0].InnerText;
                string mess = xml.GetElementsByTagName("message")[0].InnerText;
                return code + "?" + mess;
            }
            catch { return "No further Info."; }
        }
    }
}
