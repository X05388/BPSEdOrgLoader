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
using BPS.EdOrg.Loader.Models;
using BPS.EdOrg.Loader.XMLDataLoad;
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
                    ParseXmls parseXmls = new ParseXmls(param.Object, Log);
                    parseXmls.Archive(param.Object);

                    LogConfiguration(param.Object);

                    // creating the xml and executing the file through command line parser
                    RunDeptFile(param);
                    RunJobCodeFile(param);
                    RunAlertFile(param);

                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
            }
        }

        private static void LogConfiguration(EdorgConfiguration configuration)
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
        private static void LoadXml(EdorgConfiguration configuration)
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
        private static string GetArguments(EdorgConfiguration configuration)
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

            //List<SchoolDept> existingSchools = GetDeptList(param.Object);
            ParseXmls parseXmls = new ParseXmls(param.Object, Log);
            parseXmls.CreateXml();
            LoadXml(param.Object);

        }

        private static void RunJobCodeFile(CommandLineParser param)
        {

            // For JobCode_tbl.txt
            //List<string> existingStaffId = GetStaffList(param.Object);
            ParseXmls parseXmls = new ParseXmls(param.Object, Log);
            parseXmls.CreateXmlJob();

            var token = GetAuthToken();
            if (token != null)
            {

                UpdateStaffEmploymentAssociationData(token, param.Object);
                UpdateStaffAssignmentAssociationData(token, param.Object);
            }

            else Log.Error("Token is not generated, ODS not updated");

        }
        private static void RunAlertFile(CommandLineParser param)
        {

            //For 504xml.xml            
            var token = GetAuthToken();
            if (token != null)
                UpdateSpecialEducationData(token);
            else Log.Error("Token is not generated, ODS not updated");

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
        /// Update the data from EdFi ODS
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
                Log.Error("Error occured while fetching the generated xml, please check if xml file exists" + ex.Message);
                return null;
            }

        }


        /// <summary>
        /// Gets the data from the xml and updates StaffEducationOrganizationEmploymentAssociation table.
        /// </summary>
        /// <returns></returns>
        private static void UpdateStaffEmploymentAssociationData(string token, EdorgConfiguration configuration)
        {
            try
            {
                XmlDocument xmlDoc = LoadXml();
                //var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                //nsmgr.AddNamespace("a", "http://ed-fi.org/0220");
                var existingStaffIds = GetStaffList(configuration);
                var nodeList = xmlDoc.SelectNodes(@"//InterchangeStaffAssociation/StaffEducationOrganizationAssociation").Cast<XmlNode>().OrderBy(element => element.SelectSingleNode("EmploymentPeriod/EndDate").InnerText).ToList();

                foreach (XmlNode node in nodeList)
                {
                    // Extracting the data froom the XMl file
                    var staffEmploymentNodeList = GetEmploymentAssociationXml(node);

                    if (staffEmploymentNodeList != null)
                    {
                        if (staffEmploymentNodeList.status == "T")
                        {
                            int count = xmlDoc.SelectNodes(@"//InterchangeStaffAssociation/StaffEducationOrganizationAssociation/StaffReference/StaffIdentity/StaffUniqueId").Cast<XmlNode>().Where(a => a.InnerText == staffEmploymentNodeList.staffUniqueIdValue).Distinct().Count();
                            if (count > 1)
                                staffEmploymentNodeList.endDateValue = null;
                        }

                        // Adding new staff from peoplesoft file.
                        if (!string.IsNullOrEmpty(staffEmploymentNodeList.staffUniqueIdValue) && !string.IsNullOrEmpty(staffEmploymentNodeList.staff.firstName) && !string.IsNullOrEmpty(staffEmploymentNodeList.staff.lastName) && !string.IsNullOrEmpty(staffEmploymentNodeList.staff.birthDate))
                        {
                            if (!existingStaffIds.Any(p => p == staffEmploymentNodeList.staffUniqueIdValue))
                                UpdatingNewStaffData(token, staffEmploymentNodeList.staffUniqueIdValue, staffEmploymentNodeList.staff.firstName, staffEmploymentNodeList.staff.lastName, staffEmploymentNodeList.staff.birthDate);
                        }


                        // updating the values in Employment Association 
                        if (!string.IsNullOrEmpty(staffEmploymentNodeList.staffUniqueIdValue) && !string.IsNullOrEmpty(staffEmploymentNodeList.hireDateValue) && !string.IsNullOrEmpty(staffEmploymentNodeList.empDesc))
                        {
                            string id = GetEmploymentAssociationId(token, staffEmploymentNodeList);
                            if (id != null)
                            {
                                string endDate = GetAssignmentEndDate(token, staffEmploymentNodeList);
                                UpdateEndDate(token, id, staffEmploymentNodeList);
                            }

                        }
                    }

                }
                if (File.Exists(Constants.LOG_FILE))
                    SendMail(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
            }
            catch (Exception ex)
            {
                Log.Error("Error working on EmploymentAssociation Data : " + ex.Message);
            }
        }


        /// <summary>
        /// Gets the data from the xml file
        /// </summary>
        /// <returns></returns>
        private static StaffEmploymentAssociationData GetEmploymentAssociationXml(XmlNode node)
        {
            try
            {
                StaffEmploymentAssociationData staffEmploymentList = null;
                XmlNode staffNode = node.SelectSingleNode("StaffReference/StaffIdentity");
                //XmlNode EducationNode = node.SelectSingleNode("EducationOrganizationReference/EducationOrganizationIdentity");
                XmlNode EmploymentNode = node.SelectSingleNode("EmploymentPeriod");
                XmlNode EmploymentStatus = node.SelectSingleNode("EmploymentStatus");
                if (staffNode != null && EmploymentNode != null)
                {
                    staffEmploymentList = new StaffEmploymentAssociationData
                    {

                        //educationOrganizationIdValue = EducationNode.SelectSingleNode("EducationOrganizationId").InnerText ?? null,
                        staffUniqueIdValue = staffNode.SelectSingleNode("StaffUniqueId").InnerText ?? null,
                        staff = new StaffData
                        {
                            firstName = staffNode.SelectSingleNode("FirstName").InnerText ?? null,
                            lastName = staffNode.SelectSingleNode("LastName").InnerText ?? null,
                            birthDate = staffNode.SelectSingleNode("BirthDate").InnerText ?? null,
                        },
                        status = EmploymentNode.SelectSingleNode("Status").InnerText ?? null,
                        hireDateValue = EmploymentNode.SelectSingleNode("HireDate").InnerText ?? null,
                        endDateValue = EmploymentNode.SelectSingleNode("EndDate").InnerText ?? null,
                        empDesc = EmploymentStatus.SelectSingleNode("CodeValue").InnerText ?? null

                    };
                }

                return staffEmploymentList;
            }

            catch (Exception ex)
            {
                Log.Error("Error getting Emplyment data from StaffAssociation xml : Exception : " + ex.Message);
                return null;

            }
        }


        /// <summary>
        /// Gets the data from the xml and updates StaffEducationOrganizationAssignmentAssociation table.
        /// </summary>
        /// <returns></returns>
        private static void UpdateStaffAssignmentAssociationData(string token, EdorgConfiguration configuration)
        {
            try
            {
                XmlDocument xmlDoc = LoadXml();
                //var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                //nsmgr.AddNamespace("a", "http://ed-fi.org/0220");
                XmlNodeList nodeList = xmlDoc.SelectNodes("//InterchangeStaffAssociation/StaffEducationOrganizationAssociation");
                var schoolDeptids = GetDeptList(configuration);
                foreach (XmlNode node in nodeList)
                {
                    // Extracting the data froom the XMl file
                    var staffAssignmentNodeList = GetAssignmentAssociationXml(node);
                    if (staffAssignmentNodeList != null)
                    {
                        if (staffAssignmentNodeList.educationOrganizationIdValue != null)
                        {
                            if (schoolDeptids.Count > 0)
                            {
                                string schoolid = null;
                                // Getting the EdOrgId for the Department ID 
                                var educationOrganizationId = schoolDeptids.Where(x => x.DeptId.Equals(staffAssignmentNodeList.educationOrganizationIdValue)).FirstOrDefault();

                                // setting the DeptId as EdOrgId for the staff, if no corresponding school is found
                                if (educationOrganizationId == null)
                                    schoolid = staffAssignmentNodeList.educationOrganizationIdValue;
                                else
                                    schoolid = educationOrganizationId.schoolId;

                                //Inserting new Assignments and updating the postioTitle with JobCode - JobDesc
                                if (!string.IsNullOrEmpty(staffAssignmentNodeList.staffUniqueIdValue) && !string.IsNullOrEmpty(staffAssignmentNodeList.beginDateValue) && !string.IsNullOrEmpty(staffAssignmentNodeList.staffClassification) && !string.IsNullOrEmpty(staffAssignmentNodeList.positionCodeDescription))
                                {
                                    string id = GetAssignmentAssociationId(token, schoolid, staffAssignmentNodeList);
                                    if (id != null)
                                        UpdatePostionTitle(token, id, schoolid, staffAssignmentNodeList);
                                }
                            }

                        }
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

        private static StaffAssignmentAssociationData GetAssignmentAssociationXml(XmlNode node)
        {
            try
            {
                StaffAssignmentAssociationData staffAssignmentList = null;
                XmlNode staffNode = node.SelectSingleNode("StaffReference/StaffIdentity");
                XmlNode EducationNode = node.SelectSingleNode("EducationOrganizationReference/EducationOrganizationIdentity");
                XmlNode EmploymentNode = node.SelectSingleNode("EmploymentPeriod");
                if (staffNode == null && EducationNode == null) Log.Error("Nodes not reurning any data for Assignment");
                XmlNode staffClassificationNode = node.SelectSingleNode("StaffClassification");
                XmlNode EmploymentStatus = node.SelectSingleNode("EmploymentStatus");
                if (staffNode != null && EducationNode != null)
                {
                    staffAssignmentList = new StaffAssignmentAssociationData
                    {
                        staffUniqueIdValue = staffNode.SelectSingleNode("StaffUniqueId").InnerText ?? null,
                        educationOrganizationIdValue = EducationNode.SelectSingleNode("EducationOrganizationId").InnerText ?? null,
                        endDateValue = EmploymentNode.SelectSingleNode("EndDate").InnerText ?? null,
                        beginDateValue = EmploymentNode.SelectSingleNode("BeginDate").InnerText ?? null,
                        hireDateValue = EmploymentNode.SelectSingleNode("HireDate").InnerText ?? null,
                        positionCodeDescription = EmploymentNode.SelectSingleNode("PostionTitle").InnerText ?? null,
                        jobOrderAssignment = EmploymentNode.SelectSingleNode("OrderOfAssignment").InnerText ?? null,
                        staffClassification = staffClassificationNode.SelectSingleNode("CodeValue").InnerText ?? null,
                        empDesc = EmploymentStatus.SelectSingleNode("CodeValue").InnerText ?? null,

                    };

                }
                return staffAssignmentList;
            }
            catch (Exception ex)
            {
                Log.Error("Error extracting data for AssignmentAssociation Exception : " + ex.Message);
                return null;

            }

        }
        private static string GetAssignmentEndDate(string token, StaffEmploymentAssociationData staffData)
        {
            string date = null;
            try
            {
                IRestResponse response = null;

                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssignmentUrl + Constants.staffUniqueId1 + staffData.staffUniqueIdValue + Constants.employmentStatusDescriptor + staffData.empDesc);
                response = GetData(client, token);
                if (IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                        dynamic original = JsonConvert.DeserializeObject(response.Content);
                        foreach (var data in original)
                        {
                            var endDate = Convert.ToString(data.endDate) ?? null;
                            if (endDate != null)
                                date = endDate;

                            else break;
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
            return date;
        }

        private static void UpdatingNewStaffData(string token, string staffUniqueIdValue, string fname, string lname, string birthDate)
        {
            try
            {
                IRestResponse response = null;
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffUrl + Constants.staffUniqueId1 + staffUniqueIdValue);
                response = GetData(client, token);
                if (IsSuccessStatusCode((int)response.StatusCode) || (int)response.StatusCode == 404)
                {

                    //Insert Data 
                    var rootObject = new StaffDescriptor
                    {
                        staffUniqueId = staffUniqueIdValue,
                        firstName = fname,
                        lastSurname = lname,
                        birthDate = birthDate

                    };

                    string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                    response = PostData(json, client, token);
                    Log.Info("Updating  edfi.staff for Staff Id : " + staffUniqueIdValue);

                }
            }
            catch (Exception ex)
            {
                Log.Error("Error inserting staff in edfi.staff for Staff Id : " + staffUniqueIdValue + " Exception : " + ex.Message);

            }



        }
        /// <summary>
        /// Get the Id from the [StaffEducationOrganizationEmploymentAssociation] table.
        /// </summary>
        /// <returns></returns>
        private static string GetEmploymentAssociationId(string token, StaffEmploymentAssociationData staffData)
        {
            IRestResponse response = null;

            var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffEmploymentUrl + Constants.educationOrganizationId + Constants.educationOrganizationIdValue + Constants.employmentStatusDescriptor + staffData.empDesc + Constants.hireDate + staffData.hireDateValue + Constants.staffUniqueId + staffData.staffUniqueIdValue);

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
                        staffUniqueId = staffData.staffUniqueIdValue,

                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    employmentStatusDescriptor = staffData.empDesc,
                    hireDate = staffData.hireDateValue,
                    endDate = staffData.endDateValue
                };

                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                PostData(json, client, token);
                Log.Info("Inserted into StaffEducationOrganizationEmploymentAssociation for Staff Id : " + staffData.staffUniqueIdValue);
            }

            return null;
        }

        /// <summary>
        /// Get the Id from the [StaffEducationOrganizationAssignmentAssociation] table.
        /// </summary>
        /// <returns></returns>
        private static string GetAssignmentAssociationId(string token, string educationOrganizationId, StaffAssignmentAssociationData staffData)
        {
            IRestResponse response = null;
            try
            {
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssignmentUrl + Constants.educationOrganizationId + educationOrganizationId + Constants.beginDate + staffData.beginDateValue + Constants.staffClassificationDescriptorId + staffData.staffClassification + Constants.staffUniqueId + staffData.staffUniqueIdValue);
                response = GetData(client, token);
                if (IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                        dynamic original = JObject.Parse(response.Content.TrimStart(new char[] { '[' }).TrimEnd(new char[] { ']' }).ToString());
                        var id = original.id;
                        if (id != null)
                            return id.ToString();
                    }
                    else
                    {
                        //Insert Data 
                        var rootObject = new StaffAssignmentDescriptor
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
                            staffReference = new EdFiStaffReference
                            {
                                staffUniqueId = staffData.staffUniqueIdValue,

                                Link = new Link
                                {
                                    Rel = string.Empty,
                                    Href = string.Empty
                                }
                            },
                            employmentStaffEducationOrganizationEmploymentAssociationReference = new EdfiEmploymentAssociationReference
                            {
                                educationOrganizationId = Constants.educationOrganizationIdValue,
                                staffUniqueId = staffData.staffUniqueIdValue,
                                employmentStatusDescriptor = staffData.empDesc,
                                hireDate = staffData.hireDateValue,
                                Link = new Link
                                {
                                    Rel = string.Empty,
                                    Href = string.Empty
                                }
                            },

                            staffClassificationDescriptor = staffData.staffClassification,
                            beginDate = staffData.beginDateValue,
                            endDate = staffData.endDateValue,
                            orderOfAssignment = staffData.jobOrderAssignment,
                            positionTitle = staffData.positionCodeDescription
                        };

                        string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                        response = PostData(json, client, token);

                    }


                }
            }

            catch (Exception ex)
            {
                Log.Error(" Error updating  StaffEducationOrganizationAssignmentAssociation for Staff Id : " + staffData.staffUniqueIdValue + ex.Message);

            }
            return null;
        }

        /// <summary>
        /// Updates the Position title in  [StaffEducationOrganizationAssignmentAssociation] table.
        /// </summary>
        /// <returns></returns>
        private static void UpdatePostionTitle(string token, string id, string educationOrganizationId, StaffAssignmentAssociationData staffData)
        {
            try
            {

                IRestResponse response = null;
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssignmentUrl + "/" + id);
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
                        staffUniqueId = staffData.staffUniqueIdValue,

                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    employmentStaffEducationOrganizationEmploymentAssociationReference = new EdfiEmploymentAssociationReference
                    {
                        educationOrganizationId = Constants.educationOrganizationIdValue,
                        staffUniqueId = staffData.staffUniqueIdValue,
                        employmentStatusDescriptor = staffData.empDesc,
                        hireDate = staffData.hireDateValue,
                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },

                    staffClassificationDescriptor = staffData.staffClassification,
                    beginDate = staffData.beginDateValue,
                    endDate = staffData.endDateValue,
                    orderOfAssignment = staffData.jobOrderAssignment,
                    positionTitle = staffData.positionCodeDescription
                };
                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                response = PutData(json, client, token);
                Log.Info("Updating  StaffEducationOrganizationAssignmentAssociation for Staff Id : " + staffData.staffUniqueIdValue);

            }

            catch (Exception ex)
            {
                Log.Error(" Error updating  StaffEducationOrganizationAssignmentAssociation for Staff Id : " + staffData.staffUniqueIdValue);

            }

        }

        /// <summary>
        /// Updates the enddate to [StaffEducationOrganizationAssignmentAssociation] table.
        /// </summary>
        /// <returns></returns>
        private static void UpdateEndDate(string token, string id, StaffEmploymentAssociationData staffData)
        {

            try
            {
                IRestResponse response = null;
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffEmploymentUrl + "/" + id);
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
                        staffUniqueId = staffData.staffUniqueIdValue,

                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    employmentStatusDescriptor = staffData.empDesc,
                    hireDate = staffData.hireDateValue,
                    endDate = staffData.endDateValue
                };

                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                response = PutData(json, client, token);
                Log.Info("Updated StaffEducationOrganizationEmploymentAssociation for Staff Id : " + staffData.staffUniqueIdValue);

            }

            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values" + ex.Message);
            }

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


                var fragments = File.ReadAllText(ConfigurationManager.AppSettings["XMLDeploymentPath"] + $"/504inXML.xml").Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "");
                fragments = fragments.Replace("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>", "");
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

                    string beginDate = node.SelectSingleNode("beginDate").InnerText ?? null;
                    string endDate = node.SelectSingleNode("endDate").InnerText ?? null;
                    bool ideaEligibility = node.SelectSingleNode("ideaEligiblity").InnerText.Equals("true") ? true : false;
                    string iepBeginDate = node.SelectSingleNode("iepBeginDate").InnerText ?? null;
                    string iepEndDate = node.SelectSingleNode("iepEndDate").InnerText ?? null;
                    string iepReviewDate = node.SelectSingleNode("iepReviewDate").InnerText ?? null;
                    string iepParentResponse = node.SelectSingleNode("iepParentResponse").InnerText ?? null;
                    string iepSignatureDate = node.SelectSingleNode("iepSignatureDate").InnerText ?? null;
                    string Eligibility504 = node.SelectSingleNode("_504Eligibility").InnerText ?? null;

                    if (educationOrganizationIdValue != null && nameValue != null && typeValue != null)
                    {
                        // Check if the Program already exists in the ODS if not first enter the Progam.
                        VerifyProgramData(token, educationOrganizationIdValue, nameValue, typeValue);
                        if (studentUniqueIdValue != null)
                            InsertAlertDataSpecialEducation(token, typeValue, nameValue, educationOrganizationIdValue, studentUniqueIdValue, beginDate, endDate, ideaEligibility, iepBeginDate, iepEndDate, iepReviewDate, iepParentResponse, iepSignatureDate, Eligibility504);

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

        private static void UpdateSpecialEducationProgramAssociationData(string token)
        {
            try
            {
                var fragments = File.ReadAllText(ConfigurationManager.AppSettings["XMLDeploymentPath"] + $"Aspen_in_XML.xml").Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "");
                fragments = fragments.Replace("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>", "");
                fragments = fragments.Replace("504Eligibility", "_504Eligibility");
                var myRootedXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?><roots>" + fragments + "</roots>";
                var doc = XDocument.Parse(myRootedXml);
                XmlDocument xmlDoc = ToXmlDocument(doc);
                XmlNodeList nodeList = xmlDoc.SelectNodes("//roots/iep");
                foreach (XmlNode node in nodeList)
                {
                    SpecialEducationReference spEducation = new SpecialEducationReference();// Need to add all the Link references 
                    spEducation.educationOrganizationReference.educationOrganizationId = Constants.educationOrganizationIdValue;
                    XmlNode ProgramNode = node.SelectSingleNode("programReference");
                    if (ProgramNode != null)
                    {
                        spEducation.programReference.educationOrganizationId = ProgramNode.SelectSingleNode("educationOrganizationId").InnerText ?? null;
                        spEducation.programReference.type = ProgramNode.SelectSingleNode("type").InnerText ?? null;
                        spEducation.programReference.name = ProgramNode.SelectSingleNode("name").InnerText ?? null;
                    }
                    XmlNode studentNode = node.SelectSingleNode("studentReference");
                    if (studentNode != null)
                    {
                        spEducation.studentReference.studentUniqueId = studentNode.SelectSingleNode("studentUniqueId").InnerText ?? null;
                    }
                    spEducation.beginDate = node.SelectSingleNode("beginDate").InnerText ?? null;
                    spEducation.endDate = node.SelectSingleNode("endDate").InnerText ?? null;
                    spEducation.ideaEligibility = node.SelectSingleNode("ideaEligiblity").InnerText.Equals("true") ? true : false;
                    spEducation.iepBeginDate = node.SelectSingleNode("iepBeginDate").InnerText ?? null;
                    spEducation.iepEndDate = node.SelectSingleNode("iepEndDate").InnerText ?? null;
                    spEducation.iepReviewDate = node.SelectSingleNode("iepReviewDate").InnerText ?? null;
                    spEducation.lastEvaluationDate = node.SelectSingleNode("lastEvaluationDate").InnerText ?? null;
                    //spEducation.medicallyFragile = null;
                    //spEducation.multiplyDisabled = null;
                    spEducation.reasonExitedDescriptor = node.SelectSingleNode("reasonExitedDescriptor").InnerText ?? null;
                    spEducation.schoolHoursPerWeek = Int32.Parse(node.SelectSingleNode("schoolHoursPerWeek").InnerText ?? null); // Null Check req need to Modify
                    spEducation.specialEducationHoursPerWeek = Int32.Parse(node.SelectSingleNode("specialEducationHoursPerWeek").InnerText ?? null); // Null Check req need to Modify
                    spEducation.specialEducationSettingDescriptor = Constants.getSpecialEducationSetting(Int32.Parse(node.SelectSingleNode("SpecialEducationSetting").InnerText ?? null)); // Null Check req need to Modify
                    //Check required fied exist in XML source 
                    if (spEducation.programReference.educationOrganizationId != null && spEducation.programReference.name != null && spEducation.programReference.type != null && spEducation.beginDate != null && spEducation.studentReference.studentUniqueId != null) // 
                    {
                        // Check if the Program already exists in the ODS if not first enter the Progam.
                        // VerifyProgramData(token, spEducation.programReference.educationOrganizationId, spEducation.programReference.name, spEducation.programReference.type);
                        //KRS - Proram Type ID does not exist in the XML 
                        InsertAlertDataStudentSpecialEducation(token, spEducation);
                    }
                    else
                    {
                        Log.Info("Required fields are empty for studentUniqueId:" + spEducation.studentReference.studentUniqueId);
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


        public static IRestResponse VerifyProgramData(string token, string educationOrganizationId, string programName, string programTypeId)
        {
            IRestResponse response = null;
            try
            {
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.API_Program + Constants.educationOrganizationId + educationOrganizationId + Constants.programName + programName + Constants.programType + programTypeId);

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
                    response = PostData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.API_Program), token); // Need to Check - Get requred here 
                    Log.Info("Check if the program data exists in EdfI Program for programTypeId : " + programTypeId);
                }
                return response;
            }
            catch (Exception ex)
            {
                Log.Error("Error getting the program data :" + ex);
                return null;
            }


        }


        private static void InsertAlertDataSpecialEducation(string token, string type, string name, string educationId, string studentId, string beginDate, string endDate, bool ideaEligibility, string iepBeginDate, string iepEndDate, string iepReviewDate, string iepParentResponse, string iepSignatureDate, string Eligibility504)
        {
            try
            {
                IRestResponse response = null;
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
                    beginDate = iepBeginDate,
                    ideaEligibility = ideaEligibility,
                    //specialEducationSettingDescriptor = specialEducationSettingDescriptor,
                    //specialEducationHoursPerWeek = specialEducationHoursPerWeek,
                    //multiplyDisabled = multiplyDisabled,
                    //medicallyFragile = medicallyFragile,
                    //lastEvaluationDate = lastEvaluationDate,
                    iepReviewDate = iepReviewDate,
                    iepBeginDate = iepBeginDate,
                    iepEndDate = iepEndDate,
                    //reasonExitedDescriptor = reasonExitedDescriptor,
                    //schoolHoursPerWeek = schoolHoursPerWeek,
                    //servedOutsideOfRegularSession = servedOutsideOfRegularSession,
                    endDate = endDate,
                };
                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);

                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + Constants.studentUniqueId + studentId + Constants.beginDate + iepBeginDate);
                response = GetData(client, token);

                dynamic original = JsonConvert.DeserializeObject(response.Content);
                if (IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                        foreach (var data in original)
                        {
                            var id = data.id;

                            string stuId = data.studentReference.studentUniqueId;
                            DateTime iepDate = data.iepBeginDate;
                            if (id != null)
                            {
                                if (iepBeginDate != null)
                                {
                                    if (stuId != null && iepDate != null)
                                    {

                                        DateTime inputDateTime;
                                        if (DateTime.TryParse(iepBeginDate, out inputDateTime))
                                        {
                                            var result = DateTime.Compare(inputDateTime, iepDate);
                                            if (stuId == studentId && result == 0)
                                                response = PutData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + "/" + id), token);
                                            else
                                                response = PostData(json, client, token);

                                        }
                                    }

                                }

                            }
                        }
                    }

                    else
                        response = PostData(json, client, token);

                }
            }


            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values" + ex.Message);
            }


        }

        private static void InsertAlertDataStudentSpecialEducation(string token, SpecialEducationReference spEducation)
        {
            try
            {
                IRestResponse response = null;
                string json = JsonConvert.SerializeObject(spEducation, Newtonsoft.Json.Formatting.Indented);

                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.beginDate + spEducation.iepBeginDate + Constants.
                    educationOrganizationId + spEducation.educationOrganizationReference.educationOrganizationId + Constants.programEducationOrganizationId + spEducation.programReference.educationOrganizationId +
                    Constants.programName + spEducation.programReference.name + Constants.programType + spEducation.programReference.type + Constants.studentUniqueId + spEducation.studentReference.studentUniqueId);
                response = GetData(client, token);

                dynamic original = JsonConvert.DeserializeObject(response.Content);
                if (IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length > 2)
                    {
                        foreach (var data in original)
                        {
                            var id = data.id;

                            string stuId = data.studentReference.studentUniqueId;
                            DateTime iepDate = data.iepBeginDate;
                            if (id != null)
                            {
                                if (spEducation.iepBeginDate != null)
                                {
                                    if (stuId != null && iepDate != null)
                                    {

                                        DateTime inputDateTime;
                                        if (DateTime.TryParse(spEducation.iepBeginDate, out inputDateTime))
                                        {
                                            var result = DateTime.Compare(inputDateTime, iepDate);
                                            if (stuId == spEducation.studentReference.studentUniqueId && result == 0)
                                                response = PutData(json, new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StudentSpecialEducation + "/" + id), token);
                                            else
                                                response = PostData(json, client, token);

                                        }
                                    }

                                }

                            }
                        }
                    }

                    else
                        response = PostData(json, client, token);

                }
            }


            catch (Exception ex)
            {
                Log.Error("Something went wrong while updating the data in ODS, check the XML values" + ex.Message);
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


        private static List<SchoolDept> GetDeptList(EdorgConfiguration configuration)
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

        private static List<string> GetStaffList(EdorgConfiguration configuration)
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
                string message = $"Exception while sending email " + ex;
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
