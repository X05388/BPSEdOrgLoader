using System;
using log4net;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using BPS.EdOrg.Loader.ApiClient;
using Newtonsoft.Json;
using RestSharp;
using EdFi.OdsApi.Sdk;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Linq;

namespace BPS.EdOrg.Loader
{
    class Program
    {
        private static readonly Process Process = new Process();
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        

        static void Main(string[] args)
        {
            var param = new CommandLineParser();
            param.SetupHelp("?", "Help").Callback(text =>
            {
                System.Console.WriteLine(text);
                Environment.Exit(0);
            });

            var result = param.Parse(args);

            if (result.HasErrors || !param.Object.IsValid)
            {
                System.Console.Write(result.ErrorText);
                System.Console.Write(param.Object.ErrorText);
            }
            else
            {
                try
                {
                    Archive(param.Object);
                    LogConfiguration(param.Object);

                    // creating the xml and executing the file through command line parser                      
                    RunJobCodeFile(param);
                    RunAlertFile(param);
                    RunDeptFile(param);         
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
            }
        }

        private static void LogConfiguration(Configuration configuration)
        {
            Log.Info($"Api Url: {configuration.ApiUrl}");
            Log.Info($"CrossWalk Cross Walk OAuth Url:   {configuration.CrossWalkOAuthUrl}");
            Log.Info($"CrossWalk School Api Url:   {configuration.CrossWalkSchoolApiUrl}");
            Log.Info($"CrossWalk Staff Api Url:   {configuration.CrossWalkStaffApiUrl}");
            Log.Info($"School Year: {configuration.SchoolYear}");
            Log.Info($"CrossWalk Key:   {configuration.CrossWalkKey}");
            Log.Info($"CrossWalk Secret:   {configuration.CrossWalkSecret}");
            Log.Info($"Oauth Key:   {configuration.OauthKey}");
            Log.Info($"OAuth Secret:   {configuration.OauthSecret}");
            Log.Info($"Metadata Url:    {configuration.MetadataUrl}");
            Log.Info($"Data Folder: {configuration.XMLOutputPath}");
            Log.Info($"Input Data Text File Path:   {configuration.DataFilePath}");
            Log.Info($"Input Data Text File Path Job:   {configuration.DataFilePathJob}");
            Log.Info($"CrossWalk File Path: {configuration.CrossWalkFilePath}");
            Log.Info($"Working Folder: {configuration.WorkingFolder}");
            Log.Info($"Xsd Folder:  {configuration.XsdFolder}");
            Log.Info($"InterchangeOrder Folder:  {configuration.InterchangeOrderFolder}");
        }
        private static void LoadXml(Configuration configuration)
        {
            try
            {
                Log.Info($"Started executing EdFi.ApiLoader.Console from path :{configuration.ApiLoaderExePath}");
                Process.EnableRaisingEvents = true;
                Process.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(process_OutputDataReceived);
                Process.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(process_ErrorDataReceived);
                Process.Exited += new System.EventHandler(process_Exited);
                Process.StartInfo.FileName = configuration.ApiLoaderExePath;
                Process.StartInfo.Arguments = GetArguments(configuration);
                Process.StartInfo.UseShellExecute = false;
                Process.StartInfo.RedirectStandardError = true;
                Process.StartInfo.RedirectStandardOutput = true;
                Process.Start();
                Process.BeginErrorReadLine();
                Process.BeginOutputReadLine();
            }
            catch (Exception ex)
            {
                Log.Error($"Exception while executing EdFi.ApiLoader.Console : {ex.Message}");
            }
        }
        private static string GetArguments(Configuration configuration)
        {
            StringBuilder argumentBuilder = new StringBuilder();
            try
            {
                argumentBuilder.Append($"/a {configuration.ApiUrl} ");
                argumentBuilder.Append($"/d {configuration.XMLOutputPath} ");
                argumentBuilder.Append($"/k {configuration.OauthKey} ");
                argumentBuilder.Append($"/s {configuration.OauthSecret} ");
                argumentBuilder.Append($"/m {configuration.MetadataUrl} ");
                argumentBuilder.Append($"/o {configuration.OauthUrl} ");
                argumentBuilder.Append($"/x {configuration.XsdFolder} ");
                argumentBuilder.Append($"/i {configuration.InterchangeOrderFolder} ");
                argumentBuilder.Append($"/w {configuration.WorkingFolder} ");
                argumentBuilder.Append($"/y {configuration.SchoolYear}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error in parsing arguments :{ex.Message}");
            }
            return argumentBuilder.ToString();
        }
        private static void process_Exited(object sender, EventArgs e)
        {
            Log.Info($"process exited with code {Process.ExitCode.ToString()}");
        }
        private static void process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Log.Error($"Error while running API loader : {e.Data}");
        }
        private static void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Log.Info(e.Data);
        }

        private static void RunDeptFile(CommandLineParser param)
        {

            //For Dept_tbl.txt
            List<SchoolDept> existingSchools = GetDeptList(param.Object);
            CreateXml(param.Object, existingSchools);
            LoadXml(param.Object);

        }
        private static void RunAlertFile(CommandLineParser param)
        {

            //For 504xml.xml            
            var token = GetAuthToken();
            if (token != null)
            {
                UpdateSpecialEducationData(token);
                
            }

            else throw new Exception("Token is not generated, ODS not updated");

        }

        private static void RunJobCodeFile(CommandLineParser param)
        {

            // For JobCode_tbl.txt
            List<string> existingStaffId = GetStaffList(param.Object);
            CreateXmlJob(param.Object, existingStaffId);
            var token = GetAuthToken();
            if (token != null)
            {
                UpdateStaffEmploymentAssociationData(token);
                UpdateStaffAssignmentAssociationData(token, param.Object);
            }
                
            else throw new Exception("Token is not generated, ODS not updated");
            
        }
        
        private static void CreateXml(Configuration configuration, List<SchoolDept> existingDeptIds)
        {
            try
            {
                string xmlOutPutPath = ConfigurationManager.AppSettings["XMLOutputPath"];
                string filePath = Path.Combine(xmlOutPutPath, $"EducationOrganization-{DateTime.Now.Date.Month}-{ DateTime.Now.Date.Day}-{ DateTime.Now.Date.Year}.xml");
                XmlTextWriter writer = new XmlTextWriter(filePath, System.Text.Encoding.UTF8);
                CreateXmlGenericStart(writer);
                writer.WriteStartElement("InterchangeEducationOrganization");
                writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                writer.WriteAttributeString("xmlns", "ann", null, "http://ed-fi.org/annotation");
                writer.WriteAttributeString("xmlns", null, "http://ed-fi.org/0220");

                string dataFilePath = configuration.DataFilePath;
                string[] lines = File.ReadAllLines(dataFilePath);
                int i = 0;
                int deptIdIndex = 0;
                int deptTitleIndex = 0;
                int numberOfRecordsCreatedInXml = 0, numberOfRecordsSkipped = 0;
                foreach (string line in lines)
                {
                    Log.Debug(line);
                    if (i++ == 0)
                    {
                        string[] header = line.Split('\t');
                        deptIdIndex = Array.IndexOf(header, "DeptID");
                        deptTitleIndex = Array.IndexOf(header, "Dept Title");
                        if (deptIdIndex < 0 || deptTitleIndex < 0)
                        {
                            Log.Error($"Input data text file does not contains the DeptID or Dept Title headers");
                        }
                        continue;
                    }

                    string[] fields = line.Split('\t');
                    if (fields.Length > 0)
                    {
                        string deptId = fields[deptIdIndex]?.Trim();
                        string deptTitle = fields[deptTitleIndex]?.Trim();
                        if (!existingDeptIds.Any(p => p.DeptId == deptId))
                        {
                            Log.Debug($"Creating node for {deptId}-{deptTitle}");
                            CreateNode(deptId, deptTitle, writer);
                            numberOfRecordsCreatedInXml++;
                        }
                        else
                        {
                            Log.Debug($"Record skipped : {line}");
                            numberOfRecordsSkipped++;
                        }
                    }
                }
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Close();
                    if (numberOfRecordsSkipped > 0)
                    {
                        Log.Info($"Number Of records created In Xml {numberOfRecordsCreatedInXml}");
                        Log.Info($"Number of records skipped because crosswalk contains the PeopleSoftIds - {numberOfRecordsSkipped}");
                    }
                    Log.Info("CreateXML ended successfully");
                
                
            }
            catch (Exception ex)
            {
                Log.Error($"Error while creating Dept XML , Exception: {ex.Message}");
            }
        }
        private static void CreateXmlJob(Configuration configuration, List<string> existingStaffId)
        {
            try
            {
                string xmlOutPutPath = ConfigurationManager.AppSettings["XMLOutputPath"];
                string filePath = Path.Combine(xmlOutPutPath, $"StaffAssociation-{DateTime.Now.Date.Month}-{ DateTime.Now.Date.Day}-{ DateTime.Now.Date.Year}.xml");
                XmlTextWriter writer = new XmlTextWriter(filePath, System.Text.Encoding.UTF8);
                CreateXmlGenericStart(writer);
                writer.WriteStartElement("InterchangeStaffAssociation");
                //writer.WriteAttributeString("xmlns", null, "http://ed-fi.org/0220");
                //writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                //writer.WriteAttributeString("xmlns", "ann", null, "http://ed-fi.org/annotation");
                


                string dataFilePath = configuration.DataFilePathJob;
                string[] lines = File.ReadAllLines(dataFilePath);
                int i = 0;
                int empIdIndex = 0;
                int deptIdIndex = 0; int unionCodeIndex = 0; int emplClassIndex = 0; int jobIndicatorIndex = 0;
                int actionIndex=0; int  actionDateIndex = 0; int hireDateIndex = 0; int jobCodeIndex = 0; int jobTitleIndex = 0;
                int entryDateIndex = 0;
                int numberOfRecordsCreatedInXml = 0, numberOfRecordsSkipped = 0;
                foreach (string line in lines)
                {
                    Log.Debug(line);
                    if (i++ == 0)
                    {
                        string[] header = line.Split('\t');
                        empIdIndex = Array.IndexOf(header, "ID");
                        deptIdIndex = Array.IndexOf(header, "Deptid");
                        actionIndex = Array.IndexOf(header, "Action");
                        actionDateIndex = Array.IndexOf(header, "Eff Date");
                        hireDateIndex = Array.IndexOf(header, "Orig Hire Date");
                        jobCodeIndex = Array.IndexOf(header, "Job Code");
                        jobTitleIndex = Array.IndexOf(header, "Job Title");
                        entryDateIndex = Array.IndexOf(header, "Entry Date");
                        unionCodeIndex = Array.IndexOf(header, "Union Code");
                        emplClassIndex = Array.IndexOf(header, "Empl Class");
                        jobIndicatorIndex = Array.IndexOf(header, "Job Indicator");
                        if (deptIdIndex < 0 || actionIndex < 0 || empIdIndex<0 || hireDateIndex <0 || entryDateIndex<0)
                        {
                            Log.Error($"Input data text file does not contains the ID or JobCode or ActionDt headers");
                        }
                        continue;
                    }

                    string[] fields = line.Split('\t');
                    if (fields.Length > 0)
                    {
                        string staffId = fields[empIdIndex]?.Trim();
                        string deptID = fields[deptIdIndex]?.Trim();
                        string action = fields[actionIndex]?.Trim();
                        string endDate = fields[actionDateIndex]?.Trim();
                        string hireDate = fields[hireDateIndex]?.Trim();
                        string jobCode = fields[jobCodeIndex]?.Trim();
                        string jobTitle = fields[jobTitleIndex]?.Trim();
                        string entryDate = fields[entryDateIndex]?.Trim();
                        string unionCode = fields[unionCodeIndex]?.Trim();
                        string empClass = fields[emplClassIndex]?.Trim();
                        string jobIndicator = fields[jobIndicatorIndex]?.Trim();
                        if (existingStaffId.Contains(staffId))
                        {
                            string descCode = Constants.StaffClassificationDescriptorCode(jobCode, int.Parse(deptID), unionCode).ToString().Trim();
                            string empClassCode = Constants.EmpClassCode(empClass);
                            string jobOrderAssignment= Constants.JobOrderAssignment(jobIndicator);

                            Log.Debug($"Creating node for {staffId}-{deptID}-{endDate}");                            
                            CreateNodeJob(staffId, deptID, action, endDate, hireDate, jobCode, jobTitle, entryDate, descCode, empClassCode, jobOrderAssignment, writer);                            
                            numberOfRecordsCreatedInXml++;
                        }
                        else
                        {
                            Log.Debug($"Record skipped : {line}");
                            numberOfRecordsSkipped++;
                        }
                    }
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
                if (numberOfRecordsSkipped > 0)
                {
                    Log.Info($"Number Of records created In Xml {numberOfRecordsCreatedInXml}");
                    Log.Info($"Number of records skipped because crosswalk contains the PeopleSoftIds - {numberOfRecordsSkipped}");
                }
                Log.Info("CreateXML ended successfully");
            }
            catch (Exception ex)
            {
                Log.Error($"Error while creating JobCode XML , Exception: {ex.Message}");
            }
        }

        private static void CreateNodeJob(string staffId,string deptID, string action, string endDate, string hireDate,string jobCode,string jobTitle,string entryDate,string descCode,string empCode,string jobIndicator, XmlTextWriter writer)
        {
            try
            {
                if (action.Equals("TER"))
                {
                    Log.Info($"CreateNode started for jobcode:{deptID} and EndDate:{endDate}");

                    writer.WriteStartElement("StaffEducationOrganizationEmploymentAssociation");

                    writer.WriteStartElement("StaffReference");
                    writer.WriteStartElement("StaffIdentity");
                    writer.WriteStartElement("StaffUniqueId");
                    writer.WriteString(staffId);
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.WriteEndElement();

                    //writer.WriteStartElement("Department");
                    //writer.WriteString(jobCode);
                    //writer.WriteEndElement();

                    writer.WriteStartElement("EducationOrganizationReference");

                    writer.WriteStartElement("EducationOrganizationIdentity");
                    writer.WriteStartElement("EducationOrganizationId");
                    writer.WriteString(deptID);
                    writer.WriteEndElement();
                    writer.WriteEndElement();

                    writer.WriteStartElement("EducationOrganizationLookup");
                    writer.WriteStartElement("EducationOrganizationIdentificationCode");
                    writer.WriteStartElement("EducationOrganizationIdentificationSystem");
                    writer.WriteStartElement("CodeValue");
                    writer.WriteString("School");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.WriteEndElement();

                    writer.WriteEndElement();

                    writer.WriteStartElement("EmploymentStatus");
                    writer.WriteStartElement("CodeValue");
                    writer.WriteString(empCode);
                    writer.WriteEndElement();
                    writer.WriteEndElement();

                    writer.WriteStartElement("EmploymentPeriod");
                    writer.WriteStartElement("HireDate");
                    writer.WriteString(hireDate);
                    writer.WriteEndElement();
                    writer.WriteStartElement("EndDate");
                    writer.WriteString(endDate);                    
                    writer.WriteEndElement();
                    writer.WriteEndElement();                    

                    writer.WriteEndElement();

                    Log.Info($"CreateNode Ended successfully for jobcode:{deptID} and EndDate:{endDate}");
                }
                else
                {
                    Log.Info($"CreateNode started for jobcode:{deptID} and EntryDate:{entryDate}");

                    writer.WriteStartElement("StaffEducationOrganizationAssignmentAssociation");

                    writer.WriteStartElement("StaffReference");
                    writer.WriteStartElement("StaffIdentity");
                    writer.WriteStartElement("StaffUniqueId");
                    writer.WriteString(staffId);
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.WriteEndElement();

                    //writer.WriteStartElement("Department");
                    //writer.WriteString(jobCode);
                    //writer.WriteEndElement();

                    writer.WriteStartElement("EducationOrganizationReference");

                    writer.WriteStartElement("EducationOrganizationIdentity");
                    writer.WriteStartElement("EducationOrganizationId");
                    writer.WriteString(deptID);
                    writer.WriteEndElement();
                    writer.WriteEndElement();

                    writer.WriteStartElement("StaffClassification");
                    writer.WriteStartElement("CodeValue");
                    writer.WriteString(descCode);
                    writer.WriteEndElement();
                    writer.WriteEndElement();                    

                    
                    writer.WriteStartElement("BeginDate");
                    writer.WriteString(entryDate);
                    writer.WriteEndElement();
                    writer.WriteStartElement("EndDate");
                    writer.WriteString(endDate);
                    writer.WriteEndElement();
                    writer.WriteStartElement("HireDate");
                    writer.WriteString(hireDate);
                    writer.WriteEndElement();


                    writer.WriteStartElement("EmploymentStatus");
                    writer.WriteStartElement("CodeValue");
                    writer.WriteString(empCode);
                    writer.WriteEndElement();
                    writer.WriteEndElement();

                    writer.WriteStartElement("PostionTitle");
                    writer.WriteString(jobCode + "-" + jobTitle);
                    writer.WriteEndElement();

                    writer.WriteStartElement("OrderOfAssignment");
                    writer.WriteString(jobIndicator);
                    writer.WriteEndElement();

                    writer.WriteEndElement();
                    writer.WriteEndElement();

                    Log.Info($"CreateNode Ended successfully for jobcode:{deptID} and EntryDate:{entryDate}");


                }
               
               
                
            }
            catch (Exception ex)
            {
                Log.Error($"There is exception while creating Node for jobcode:{deptID} and EndDate:{endDate}, Exception  :{ex.Message}");
            }
        }

        private static void CreateNode(string deptId, string deptTitle, XmlTextWriter writer)
            {
                try
                {
                Log.Info($"CreateNode started for DeptId:{deptId} and DeptTitle:{deptTitle}");
                writer.WriteStartElement("EducationServiceCenter");

                writer.WriteStartElement("StateOrganizationId");
                writer.WriteString(deptId);
                writer.WriteEndElement();

                writer.WriteStartElement("EducationOrganizationIdentificationCode");
                writer.WriteStartElement("IdentificationCode");
                writer.WriteString(deptId);
                writer.WriteEndElement();

                writer.WriteStartElement("EducationOrganizationIdentificationSystem");
                writer.WriteStartElement("CodeValue");
                writer.WriteString("School");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("NameOfInstitution");
                writer.WriteString(deptTitle);
                writer.WriteEndElement();

                writer.WriteStartElement("EducationOrganizationCategory");
                writer.WriteString("Education Service Center");
                writer.WriteEndElement();

                writer.WriteStartElement("Address");
                writer.WriteStartElement("StreetNumberName");
                writer.WriteString("2300 Washington St");
                writer.WriteEndElement();

                writer.WriteStartElement("City");
                writer.WriteString("Roxbury");
                writer.WriteEndElement();

                writer.WriteStartElement("StateAbbreviation");
                writer.WriteString("MA");
                writer.WriteEndElement();

                writer.WriteStartElement("PostalCode");
                writer.WriteString("02119");
                writer.WriteEndElement();

                writer.WriteStartElement("AddressType");
                writer.WriteString("Physical");
                writer.WriteEndElement();

                writer.WriteEndElement();

                writer.WriteStartElement("EducationServiceCenterId");
                writer.WriteString(deptId);
                writer.WriteEndElement();

                writer.WriteEndElement();

                Log.Info($"CreateNode Ended successfully for DeptId:{deptId} and DeptTitle:{deptTitle}");
            }
            catch (Exception ex)
            {
                Log.Error($"There is exception while creating Node for DeptId:{deptId} and Dept title: {deptTitle}, Exception  :{ex.Message}");
            }
        }
        private static void Archive(Configuration configuration)
        {
            try
            {
                Log.Info("Archiving started");
                MoveFiles(configuration);
                DeleteOldFiles(configuration);
                Log.Info("Archiving ended");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }

        }

        private static void CreateXmlGenericStart(XmlTextWriter writer)
        {
            try
            {
                Log.Info("CreateXML started");
                writer.WriteStartDocument();
                writer.Formatting = System.Xml.Formatting.Indented;
                writer.Indentation = 2;

            }
            catch (Exception ex)
            {
                Log.Error($"Error while creating Generic XML start, Exception: {ex.Message}");
            }
        }

        private static void CreateXmlGenericEnd(XmlTextWriter writer, int numberOfRecordsCreatedInXml, int numberOfRecordsSkipped)
        {
            try
            {
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
                if (numberOfRecordsSkipped > 0)
                {
                    Log.Info($"Number Of records created In Xml {numberOfRecordsCreatedInXml}");
                    Log.Info($"Number of records skipped because crosswalk contains the PeopleSoftIds - {numberOfRecordsSkipped}");
                }
                Log.Info("CreateXML ended successfully");
            }
            catch (Exception ex)
            {
                Log.Error($"Error while creating Generic XML End, Exception: {ex.Message}");
            }


        }
        private static void MoveFiles(Configuration configuration)
        {
            try
            {
                string rootFolderPath = configuration.XMLOutputPath;
                string backupPath = Path.Combine(rootFolderPath, "Backup");
                string filesToDelete = @"*.xml";   // Only delete WAV files ending by "_DONE" in their filenames
                string[] fileList = System.IO.Directory.GetFiles(rootFolderPath, filesToDelete);
                if (!Directory.Exists(backupPath))
                {
                    Directory.CreateDirectory(backupPath);
                }

                foreach (string file in fileList)
                {
                    string fileToMove = Path.Combine(rootFolderPath, Path.GetFileName(file));
                    string moveTo = Path.Combine(backupPath, Path.GetFileName(file));
                    if (File.Exists(moveTo))
                    {
                        File.Delete(moveTo);
                    }
                    File.Move(fileToMove, moveTo);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }

        }
        private static void DeleteOldFiles(Configuration configuration)
        {
            try
            {
                string backupPath = Path.Combine(configuration.XMLOutputPath, "Backup");
                string[] files = Directory.GetFiles(backupPath);
                int numberOfBackupDays = Convert.ToInt16(ConfigurationManager.AppSettings.Get("BackupDays"));
                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);

                    if (fi.LastAccessTime < DateTime.Now.AddDays(-1 * numberOfBackupDays))
                    {
                        fi.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }


        /// <summary>
        /// Token is needed for the Bearer value to Post data 
        /// </summary>
        /// <returns></returns>
        private static string GetAuthToken()
        {
            var tokenRetriever = new TokenRetriever(ConfigurationManager.AppSettings["OAuthUrl"], ConfigurationManager.AppSettings["APP_Key"], ConfigurationManager.AppSettings["APP_Secret"]);
            var token = tokenRetriever.ObtainNewBearerToken();
            return token;
        }

        /// <summary>
        /// POST the data from EdFi ODS
        /// </summary>
        /// <returns></returns>
        private static IRestResponse PostData(string jsonData, RestClient client, string token)
        {
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", "Bearer  " + token);
            request.AddParameter("application/json; charset=utf-8", jsonData, ParameterType.RequestBody);
            request.RequestFormat = DataFormat.Json;
            return client.Execute(request);

        }

        /// <summary>
        /// Updates the data from EdFi ODS
        /// </summary>
        /// <returns></returns>
        private static IRestResponse PutData(string jsonData, RestClient client, string token)
        {
            var request = new RestRequest(Method.PUT);
            request.AddHeader("Authorization", "Bearer  " + token);
            request.AddParameter("application/json; charset=utf-8", jsonData, ParameterType.RequestBody);
            request.RequestFormat = DataFormat.Json;
            return client.Execute(request);

        }

        /// <summary>
        /// Gets the Data from the ODS
        /// </summary>
        /// <returns></returns>
        private static IRestResponse GetData(RestClient client, string token)
        {
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", "Bearer  " + token);
            request.AddParameter("application/json; charset=utf-8", ParameterType.RequestBody);
            request.RequestFormat = DataFormat.Json;
            return client.Execute(request);
        }

        /// <summary>
        /// Checks the value of the Status code. 
        /// </summary>
        /// <returns></returns>
        public static bool IsSuccessStatusCode(int statusCode)
        {
            return ((int)statusCode >= 200) && ((int)statusCode <= 204);
        }


        /// <summary>
        /// Loading the generated Xml to get required values
        /// </summary>
        /// <returns></returns>

        private static XmlDocument LoadXml()
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(ConfigurationManager.AppSettings["XMLOutputPath"] + $"/StaffAssociation-{DateTime.Now.Date.Month}-{ DateTime.Now.Date.Day}-{ DateTime.Now.Date.Year}.xml");
                return xmlDoc;

            }
            catch (Exception ex)
            {
                throw new Exception(" Error occured while fetching the generated xml, please check if xml file exists" + ex.Message);
            }

        }
        /// <summary>
        /// Gets the data from the xml and updates the table.
        /// </summary>
        /// <returns></returns>
        private static void UpdateStaffEmploymentAssociationData(string token)
        {
            try
            {
                string staffUniqueIdValue = null;
                string educationOrganizationIdValue = null;
                string endDateValue = null;
                string hireDateValue = null;
                string empDesc = null;

                XmlDocument xmlDoc = LoadXml();
                //var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                //nsmgr.AddNamespace("a", "http://ed-fi.org/0220");
                XmlNodeList nodeList = xmlDoc.SelectNodes("//InterchangeStaffAssociation/StaffEducationOrganizationEmploymentAssociation");

                foreach (XmlNode node in nodeList)
                {

                    XmlNode staffNode = node.SelectSingleNode("StaffReference/StaffIdentity");
                    // get the values from the <Staff> node
                    if (staffNode != null)
                    {
                        staffUniqueIdValue = staffNode.SelectSingleNode("StaffUniqueId").InnerText ?? null;

                    }
                    XmlNode EducationNode = node.SelectSingleNode("EducationOrganizationReference/EducationOrganizationIdentity");

                    // get the values from the <Staff> node
                    if (EducationNode != null)
                    {
                        educationOrganizationIdValue = EducationNode.SelectSingleNode("EducationOrganizationId").InnerText ?? null;

                    }
                    XmlNode EmploymentStatus = node.SelectSingleNode("EmploymentStatus");

                    if (EmploymentStatus != null)
                    {
                        empDesc = EmploymentStatus.SelectSingleNode("CodeValue").InnerText ?? null;
                    }
                    XmlNode EmploymentNode = node.SelectSingleNode("EmploymentPeriod");

                    if (EmploymentNode != null)
                    {
                        endDateValue = EmploymentNode.SelectSingleNode("EndDate").InnerText ?? null;
                        hireDateValue = EmploymentNode.SelectSingleNode("HireDate").InnerText ?? null;

                    }
                    if (staffUniqueIdValue != null && hireDateValue != null && empDesc != null)
                    {
                        string id = GetEmploymentAssociationId(token, staffUniqueIdValue, hireDateValue, empDesc, endDateValue);
                        if (id != null && endDateValue != null)
                            UpdateEndDate(token, id, endDateValue, hireDateValue, staffUniqueIdValue, empDesc);
                    }
                }
                if (File.Exists(Constants.LOG_FILE))
                    SendMail(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }

        private static void UpdateStaffAssignmentAssociationData(string token, Configuration configuration)
        {
            try
            {
                string staffUniqueIdValue = null;
                string positionCodeDescription = null;
                string educationOrganizationIdValue = null;
                string endDateValue = null;
                string beginDateValue = null;
                string hireDateValue = null;
                string staffClassification = null;
                string empDesc = null;
                string jobOrderAssignment = null;

                XmlDocument xmlDoc = LoadXml();
                //var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                //nsmgr.AddNamespace("a", "http://ed-fi.org/0220");
                XmlNodeList nodeList = xmlDoc.SelectNodes("//InterchangeStaffAssociation/StaffEducationOrganizationAssignmentAssociation");
                var schoolDeptids = GetDeptList(configuration);
                foreach (XmlNode node in nodeList)
                {

                    XmlNode staffNode = node.SelectSingleNode("StaffReference/StaffIdentity");

                    // get the values from the <Staff> node
                    if (staffNode != null)
                    {
                        staffUniqueIdValue = staffNode.SelectSingleNode("StaffUniqueId").InnerText ?? null;

                    }
                    XmlNode EducationNode = node.SelectSingleNode("EducationOrganizationReference");

                    // get the values from the <Staff> node
                    if (EducationNode != null)
                    {
                        XmlNode EducationOrgNode = EducationNode.SelectSingleNode("EducationOrganizationIdentity");
                        
                        if (EducationOrgNode != null)
                            educationOrganizationIdValue = EducationOrgNode.SelectSingleNode("EducationOrganizationId").InnerText ?? null;
                        endDateValue = EducationNode.SelectSingleNode("EndDate").InnerText ?? null;
                        beginDateValue = EducationNode.SelectSingleNode("BeginDate").InnerText ?? null;
                        hireDateValue = EducationNode.SelectSingleNode("HireDate").InnerText ?? null;
                        positionCodeDescription = EducationNode.SelectSingleNode("PostionTitle").InnerText ?? null;
                        jobOrderAssignment = EducationNode.SelectSingleNode("OrderOfAssignment").InnerText ?? null; 

                        XmlNode staffClassificationNode = EducationNode.SelectSingleNode("StaffClassification");
                        staffClassification = staffClassificationNode.SelectSingleNode("CodeValue").InnerText ?? null;

                        XmlNode EmploymentStatus = EducationNode.SelectSingleNode("EmploymentStatus");
                        if (EmploymentStatus != null)
                        {
                            empDesc = EmploymentStatus.SelectSingleNode("CodeValue").InnerText ?? null;
                        }
                    }

                    if (educationOrganizationIdValue!= null)
                    {
                        
                        if (schoolDeptids.Count > 0)
                        {
                            var educationOrganizationId = schoolDeptids.Where(x => x.DeptId == educationOrganizationIdValue).FirstOrDefault();
                            if (educationOrganizationId != null && staffUniqueIdValue != null && beginDateValue != null && staffClassification != null)
                            {
                                var schoolid = educationOrganizationId.schoolId;
                                string id = GetAssignmentAssociationId(token, schoolid, staffClassification, staffUniqueIdValue, beginDateValue);
                                if (id != null && positionCodeDescription != null)
                                    UpdatePostionTitle(token, id, schoolid, staffClassification, endDateValue, beginDateValue, hireDateValue, staffUniqueIdValue, positionCodeDescription, empDesc, jobOrderAssignment);
                            }
                        }

                    }

                }

                if (File.Exists(Constants.LOG_FILE))
                    SendMail(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
            }
            catch(Exception ex)
            {
                Log.Error(ex.Message);
            }       
            
        }

        /// <summary>
        /// Get the Id from the [StaffEducationOrganizationEmploymentAssociation] table.
        /// </summary>
        /// <returns></returns>
        private static string GetEmploymentAssociationId(string token, string staffUniqueIdValue, string hireDateValue, string empDesc, string endDateValue)
        {
            IRestResponse response = null;
            var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffEmploymentUrl + Constants.educationOrganizationId + Constants.educationOrganizationIdValue + Constants.employmentStatusDescriptor + empDesc + Constants.hireDate + hireDateValue + Constants.staffUniqueId + staffUniqueIdValue);

            response = GetData(client, token);
            if (IsSuccessStatusCode((int)response.StatusCode))
            {
                if (response.Content.Length > 2)
                {
                    dynamic original = JObject.Parse(response.Content.ToString());
                    var id = original.id;
                    if (id != null)
                        return id;
                }      

            }
            else
            {
                //Insert Data 
                var rootObject = new StaffEmploymentDescriptor
                {

                    educationOrganizationReference = new EdFiEducationReference
                    {
                        educationOrganizationId = Constants.educationOrganizationIdValue,
                        Link = new Link()
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    staffReference = new EdFiStaffReference
                    {
                        staffUniqueId = staffUniqueIdValue,

                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    employmentStatusDescriptor = empDesc,
                    hireDate = hireDateValue,
                    endDate = endDateValue
                };

                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                response = PostData(json, client, token);
                if ((int)response.StatusCode > 204 || (int)response.StatusCode < 200)
                {
                    //Log the Error
                    ErrorLog errorLog = new ErrorLog();
                    errorLog.staffUniqueId = staffUniqueIdValue;
                    errorLog.endDate = endDateValue;
                    errorLog.ErrorMessage = response.Content.ToString().Replace(System.Environment.NewLine, string.Empty) ?? null;
                    //ErrorLogging(errorLog);

                }
            }
            return null;
        }

        /// <summary>
        /// Get the Id from the [StaffEducationOrganizationAssignmentAssociation] table.
        /// </summary>
        /// <returns></returns>
        private static string GetAssignmentAssociationId(string token,string educationOrganizationIdValue,string staffClassification, string staffUniqueIdValue, string beginDateValue)
        {
            IRestResponse response = null;

            var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssociationUrl+Constants.educationOrganizationId + educationOrganizationIdValue+ Constants.beginDate + beginDateValue+ Constants.staffClassificationDescriptorId + staffClassification + Constants.staffUniqueId + staffUniqueIdValue);
            response = GetData(client, token);
            if (IsSuccessStatusCode((int)response.StatusCode))
            {
                if (response.Content.Length >2)
                {
                    dynamic original = JObject.Parse(response.Content.TrimStart(new char[] { '[' }).TrimEnd(new char[] { ']' }).ToString());
                    var id = original.id;
                    if (id != null)
                        return id.ToString();
                }
                

            }
            return null;
        }

        /// <summary>
        /// Updates the Position title in  [StaffEducationOrganizationAssignmentAssociation] table.
        /// </summary>
        /// <returns></returns>
        private static void UpdatePostionTitle(string token,string id,string educationOrganizationId, string staffClassification,string endDateValue,string beginDateValue, string hireDateValue, string staffUniqueIdValue, string jobCodeDescription, string empDesc, string jobOrderAssignment)
        {
            try
            {

                IRestResponse response = null;
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] +Constants.StaffAssociationUrl + "/" + id);
                var rootObject = new StaffAssignmentDescriptor
                {
                    id = id,
                    educationOrganizationReference = new EdFiEducationReference
                    {
                        educationOrganizationId = educationOrganizationId,
                        Link = new Link()
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    staffReference = new EdFiStaffReference
                    {
                        staffUniqueId = staffUniqueIdValue,

                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    employmentStaffEducationOrganizationEmploymentAssociationReference = new EdfiEmploymentAssociationReference
                    {
                        educationOrganizationId =Constants.educationOrganizationIdValue,
                        staffUniqueId = staffUniqueIdValue,
                        employmentStatusDescriptor = empDesc,
                        hireDate = hireDateValue,
                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },

                    staffClassificationDescriptor = staffClassification,
                    beginDate = beginDateValue,
                    endDate = endDateValue,
                    orderOfAssignment = jobOrderAssignment,
                    positionTitle = jobCodeDescription
                };                
                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                response = PutData(json, client, token);
                if ((int)response.StatusCode > 204 || (int)response.StatusCode < 200)
                {
                    if (response.Content.Length > 2)
                    {
                        //Log the Error
                        ErrorLog errorLog = new ErrorLog();
                        errorLog.staffUniqueId = staffUniqueIdValue;
                        errorLog.PositionTitle = jobCodeDescription;
                        errorLog.ErrorMessage = response.Content.ToString().Replace(System.Environment.NewLine, string.Empty) ?? null;
                        //ErrorLogging(errorLog);
                    }

                }
                               
            }

            catch (Exception ex)
            {
                throw new Exception("Something went wrong while updating the data in ODS, check the XML values" + ex.Message);
            }

        }
                             
        /// <summary>
        /// Updates the enddate to [StaffEducationOrganizationAssignmentAssociation] table.
        /// </summary>
        /// <returns></returns>
        private static bool UpdateEndDate(string token, string id, string endDateValue, string hireDateValue, string staffUniqueIdValue, string empDesc)
        {
            bool isPosted = true;
            try
            {                
                IRestResponse response = null;
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffEmploymentUrl + "/"+id);
                var rootObject = new StaffEmploymentDescriptor
                {
                    id = id,
                    educationOrganizationReference = new EdFiEducationReference
                    {
                        educationOrganizationId = Constants.educationOrganizationIdValue,
                        Link = new Link()
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    staffReference = new EdFiStaffReference
                    {
                        staffUniqueId = staffUniqueIdValue,

                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    employmentStatusDescriptor = empDesc,
                    hireDate = hireDateValue,
                    endDate = endDateValue
                };
                
                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                response = PutData(json, client, token);
                if ((int)response.StatusCode > 204 || (int)response.StatusCode < 200)
                {
                    if (response.Content.Length > 2)
                    {
                        //Log the Error
                        ErrorLog errorLog = new ErrorLog();
                        errorLog.staffUniqueId = staffUniqueIdValue;
                        errorLog.endDate = endDateValue;
                        errorLog.ErrorMessage = response.Content.ToString().Replace(System.Environment.NewLine, string.Empty) ?? null;
                        //ErrorLogging(errorLog);
                        isPosted = false;
                    }
                    
                }                      
                
            }

            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values" + ex.Message);
            }
            return isPosted;
        }


        private static XmlDocument ToXmlDocument(XDocument xDocument)
        {
            var xmlDocument = new XmlDocument();
            using (var xmlReader = xDocument.CreateReader())
            {
                xmlDocument.Load(xmlReader);
            }
            return xmlDocument;
        }

        private static void UpdateSpecialEducationData(string token)
        {
            
            try
            {
                string typeValue = null;
                string nameValue = null;
                string educationOrganizationIdValue = null;
                string studentUniqueIdValue = null;     
               

                var fragments = File.ReadAllText(ConfigurationManager.AppSettings["XMLDeploymentPath"] + $"/504 in XML.xml").Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "");
                fragments =fragments.Replace("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>", "");
                fragments = fragments.Replace("504Eligibility", "_504Eligibility");
                var myRootedXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?><roots>" + fragments + "</roots>";
                var doc = XDocument.Parse(myRootedXml);
                XmlDocument xmlDoc = ToXmlDocument(doc);
                //var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                //nsmgr.AddNamespace("a", "http://ed-fi.org/0220");
                XmlNodeList nodeList = xmlDoc.SelectNodes("//roots/root");
               
                foreach (XmlNode node in nodeList)
                {
                    List<SpecialEducationReference> spEducationList = new List<SpecialEducationReference>();
                    
                    XmlNode ProgramNode = node.SelectSingleNode("programReference");                    
                    if (ProgramNode != null)
                    {
                        educationOrganizationIdValue = ProgramNode.SelectSingleNode("educationOrganizationId").InnerText ?? null;
                        typeValue = ProgramNode.SelectSingleNode("type").InnerText ?? null;
                        nameValue = ProgramNode.SelectSingleNode("name").InnerText ?? null;
                        
                    }

                    XmlNode studentNode = node.SelectSingleNode("studentReference");                    
                    if (studentNode != null)
                    {
                        studentUniqueIdValue = studentNode.SelectSingleNode("studentUniqueId").InnerText ?? null;

                    }

                    spEducationList.Add(new SpecialEducationReference()
                    {
                        
                        beginDate = node.SelectSingleNode("beginDate").InnerText ?? null,
                        endDate = node.SelectSingleNode("endDate").InnerText ?? null,
                        ideaEligibility = node.SelectSingleNode("ideaEligiblity").InnerText.Equals("true")? true : false,
                        iepBeginDate = node.SelectSingleNode("iepBeginDate").InnerText ?? null,
                        iepEndDate = node.SelectSingleNode("iepEndDate").InnerText ?? null,
                        iepReviewDate = node.SelectSingleNode("iepReviewDate").InnerText ?? null,
                        iepParentResponse = node.SelectSingleNode("iepParentResponse").InnerText ?? null,
                        iepSignatureDate = node.SelectSingleNode("iepSignatureDate").InnerText ?? null,
                        Eligibility504 = node.SelectSingleNode("_504Eligibility").InnerText ?? null,
                    });
                    if (educationOrganizationIdValue != null && nameValue != null && typeValue != null)
                    { 
                        // Check if the Program already exists in the ODS if not first enter the Progam.
                        VerifyProgramData(token, educationOrganizationIdValue, nameValue, typeValue);
                        if(studentUniqueIdValue != null)
                        InsertDataSpecialEducation(token,typeValue,nameValue,educationOrganizationIdValue,studentUniqueIdValue,spEducationList);

                    }

                }

                if (File.Exists(Constants.LOG_FILE))
                    SendMail(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }

        }


        public static IRestResponse VerifyProgramData(string token, string educationOrganizationId,string programName,string programTypeId)
        {
            IRestResponse response = null;
            try
            {
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.API_Program+ Constants.educationOrganizationId + educationOrganizationId +Constants.programName+programName +Constants.programType+programTypeId);

                response = GetData(client, token);
                if (!IsSuccessStatusCode((int)response.StatusCode))
                {
                    var rootObject = new EdFiProgram
                    {
                        educationOrganizationReference = new EdFiEducationReference
                        {
                            educationOrganizationId = educationOrganizationId,
                            Link = new Link()
                            {
                                Rel = string.Empty,
                                Href = string.Empty
                            }
                        },
                        programId = null,
                        type = programTypeId,
                        sponsorType = string.Empty,
                        name = programName,


                    };
                    
                    string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                    response = PostData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.API_Program), token);
                    if ((int)response.StatusCode > 204 || (int)response.StatusCode < 200)
                    {
                        if (response.Content.Length > 2)
                        {
                            //Log the Error
                            ErrorLog errorLog = new ErrorLog();
                            //errorLog.staffUniqueId = studentUniqueId;
                            //errorLog.endDate = endDateValue;
                            errorLog.ErrorMessage = response.Content.ToString().Replace(System.Environment.NewLine, string.Empty) ?? null;
                            //ErrorLogging(errorLog);

                        }

                    }
                }
                return response;
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting the program data :" + ex);
            }


        }

       
        private static void InsertDataSpecialEducation(string token,string type,string name,string educationId, string studentId, List<SpecialEducationReference> spList)
        {
            try
            {
                IRestResponse response = null;
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation);
                response = GetData(client, token);
                if (IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length <= 2) throw new Exception("No Data retrieved from specialEducation ");
                    
                        dynamic original = JsonConvert.DeserializeObject(response.Content);
                        foreach (var data in original)
                        {
                            var id = data.id;
                            var stuId = data.studentReference.studentUniqueId;
                            DateTime iepDate = data.iepBeginDate;
                            if (id != null)
                                response = GetData(new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + "/" + id), token);
                            if (IsSuccessStatusCode((int)response.StatusCode))
                            foreach (var item in spList)
                            {
                                if (item.iepBeginDate != null)
                                {
                                    var rootObject = new SpecialEducationReference
                                    {
                                        educationOrganizationReference = new EdFiEducationReference
                                        {
                                            educationOrganizationId = Constants.educationOrganizationIdValue,
                                            Link = new Link()
                                            {
                                                Rel = string.Empty,
                                                Href = string.Empty
                                            }
                                        },
                                        programReference = new ProgramReference
                                        {
                                            educationOrganizationId = educationId,
                                            type = type,
                                            name = name,
                                            Link = new Link
                                            {
                                                Rel = string.Empty,
                                                Href = string.Empty
                                            }
                                        },
                                        studentReference = new StudentReference
                                        {
                                            studentUniqueId = studentId,
                                            Link = new Link
                                            {
                                                Rel = string.Empty,
                                                Href = string.Empty
                                            }
                                        },
                                        beginDate = item.iepBeginDate,
                                        ideaEligibility = item.ideaEligibility,
                                        specialEducationSettingDescriptor = item.specialEducationSettingDescriptor,
                                        specialEducationHoursPerWeek = item.specialEducationHoursPerWeek,
                                        multiplyDisabled = item.multiplyDisabled,
                                        medicallyFragile = item.medicallyFragile,
                                        lastEvaluationDate = item.lastEvaluationDate,
                                        iepReviewDate = item.iepReviewDate,
                                        iepBeginDate = item.iepBeginDate,
                                        iepEndDate = item.iepEndDate,
                                        reasonExitedDescriptor = item.reasonExitedDescriptor,
                                        schoolHoursPerWeek = item.schoolHoursPerWeek,
                                        servedOutsideOfRegularSession = item.servedOutsideOfRegularSession,
                                        endDate = item.endDate,
                                    };
                                    string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                                    if (response.Content.Length > 2)
                                    {
                                        if (stuId != null && iepDate != null)
                                        {

                                            DateTime inputDateTime;
                                            if (DateTime.TryParse(item.iepBeginDate, out inputDateTime))
                                            {
                                                var result = DateTime.Compare(inputDateTime, iepDate);
                                                if (stuId == studentId && result == 0)
                                                    response = PutData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + "/" + id), token);
                                            }
                                        }
                                    }
                                    else
                                        response = PostData(json, client, token);


                                }

              
                }
            }

            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values" + ex.Message);
            }
            

        }


        private static void InsertAlertData(string type,string name,string educationId, string studentId,List<SpecialEducationReference> spList)
        {

            
                       
                    
                }
            }
        }
        private static void ErrorLogging(ErrorLog errorLog)
        {
            string strPath = Constants.LOG_FILE;
            bool isFirst = false;
            if (!File.Exists(strPath))
            {
                File.Create(strPath).Dispose();
                isFirst = true;
            }
            using (StreamWriter sw = File.AppendText(strPath))
            {
                if (isFirst)
                    sw.WriteLine("StudentId,EducationOrganizationId,ProgramTypeID,ProgramName,ErrorMessage");
                sw.WriteLine("{0},{1},{2},{3},{4}", errorLog.staffUniqueId, errorLog.endDate, errorLog.ErrorMessage);
                sw.Close();
            }


        }

       
        private static List<SchoolDept> GetDeptList(Configuration configuration)
        {
            
            List<SchoolDept> existingDeptIds = new List<SchoolDept>();
            try
            {
                TokenRetriever tokenRetriever = new TokenRetriever(ConfigurationManager.AppSettings["OAuthUrl"], ConfigurationManager.AppSettings["APP_Key"], ConfigurationManager.AppSettings["APP_Secret"]);

                string token = tokenRetriever.ObtainNewBearerToken();
                if (!string.IsNullOrEmpty(token))
                {
                    Log.Info($"Crosswalk API token retrieved successfully");
                    RestServiceManager restManager = new RestServiceManager(configuration, token, Log);
                    existingDeptIds = restManager.GetSchoolList();
                }
                else
                {
                    Log.Error($"Error while retrieving access token for Crosswalk API");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error while getting school list:{ex.Message}");
            }

            return existingDeptIds;
        }

        private static List<string> GetStaffList(Configuration configuration)
        {

            List<string> existingStaffIds = new List<string>();
            try
            {
                TokenRetriever tokenRetriever = new TokenRetriever(ConfigurationManager.AppSettings["OAuthUrl"], ConfigurationManager.AppSettings["APP_Key"], ConfigurationManager.AppSettings["APP_Secret"]);

                string token = tokenRetriever.ObtainNewBearerToken();
                if (!string.IsNullOrEmpty(token))
                {
                    Log.Info($"Crosswalk API token retrieved successfully");
                    RestServiceManager restManager = new RestServiceManager(configuration, token, Log);
                    existingStaffIds = restManager.GetStaffList();
                }
                else
                {
                    Log.Error($"Error while retrieving access token for Crosswalk API");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error while getting school list:{ex.Message}");
            }

            return existingStaffIds;
        }


        /// <summary>
        /// Sending the log in the email
        /// </summary>
        /// <returns></returns>
        public static bool SendMail(string recipient, string subject, string body, string attachmentFilename)
        {
            try
            {
                SendEmail emailObj = new SendEmail();
                using (MemoryStream stream = new MemoryStream())
                {
                    stream.Seek(0, SeekOrigin.Begin);

                    Attachment att = null;
                    if (File.Exists(Constants.LOG_FILE))
                    {
                        att = new Attachment(Constants.LOG_FILE);
                        emailObj.AttachmentList = new List<Attachment> { att };
                    }
                    emailObj.ToAddr = new System.Collections.ArrayList();
                    emailObj.ToAddr.Add(recipient);
                    emailObj.FromAddr = Constants.EmailFromAddress;
                    emailObj.EmailSubject = subject;
                    emailObj.EmailContent = body;
                    SendEmailNotification(emailObj);
                }
                return true;
            }

            catch (Exception ex)
            {
                string message = $"Exception while sending email "+ ex;
                //new EmailException(message, ex);
                return false;
            }

        }



        /// <summary>
        /// Send & Save email notification - general 
        /// </summary>
        /// <param name="emailObj"></param>
        /// <returns></returns>
        public static bool SendEmailNotification(SendEmail emailObj)
        {

            MailMessage message = new MailMessage();
            message.From = new MailAddress(emailObj.FromAddr);
            foreach (string toString in emailObj.ToAddr)
            {
                message.To.Add(toString);
            }

            //send if there are attachments
            if (emailObj.AttachmentList != null && emailObj.AttachmentList.Count > 0)
            {
                foreach (Attachment att in emailObj.AttachmentList)
                {
                    message.Attachments.Add(att);
                }
            }

            if (!String.IsNullOrEmpty(emailObj.BccToAdr))
            {
                message.Bcc.Add(new MailAddress(emailObj.BccToAdr));
            }

            message.Subject = emailObj.EmailSubject;
            message.IsBodyHtml = true;
            AlternateView av = AlternateView.CreateAlternateViewFromString(emailObj.EmailContent, null, MediaTypeNames.Text.Html);
            message.AlternateViews.Add(av);
            using (SmtpClient smtp = new SmtpClient())
            {
                smtp.Host = Constants.SmtpServerHost;
                smtp.Send(message);
                return true;
            }
        }

    }
}
