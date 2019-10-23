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
using BPS.EdOrg.Loader.MetaData;
using BPS.EdOrg.Loader.EdFi.Api;
using BPS.EdOrg.Loader.Controller;
namespace BPS.EdOrg.Loader
{
    class Program
    {
        private static readonly Process Process = new Process();
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static EdFiApiCrud edfiApi = new EdFiApiCrud();
        private static StudentSpecialEducationController studentSpecController = new StudentSpecialEducationController();
        public static ParseXmls parseXmls = null;
        public static Notification notification = new Notification();
        public static StaffAssociationController staffController;

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
                    parseXmls = new ParseXmls(param.Object, Log);
                    parseXmls.Archive(param.Object);
                    LogConfiguration(param.Object);

                    // creating the xml and executing the file through command line parser
                    RunAlertFile(param);
                    RunDeptFile(param);
                    RunJobCodeFile(param);                   

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

            var token = edfiApi.GetAuthToken();
            if (token != null)
            {
                staffController = new StaffAssociationController(token, param.Object, Log);
                staffController.UpdateStaffEmploymentAssociationData(token, param.Object);
                //UpdateStaffEmploymentAssociationData(token, param.Object, Log);
                staffController.UpdateStaffAssignmentAssociationData(token, param.Object);
            }

            else Log.Error("Token is not generated, ODS not updated");

        }
        private static void RunAlertIEPFile(CommandLineParser param)
        {

            //For 504xml.xml            
            var token = edfiApi.GetAuthToken();
            if (token != null)
            {
                studentSpecController.UpdateAlertSpecialEducationData(token, parseXmls);
                studentSpecController.UpdateIEPSpecialEducationProgramAssociationData(token, parseXmls);
            }
            else Log.Error("Token is not generated, ODS not updated");

        }

        private static bool IsSuccessStatusCode(int statusCode)
        {
            return ((int)statusCode >= 200) && ((int)statusCode <= 204);
        }
                        if (staffAssignmentNodeList.EducationOrganizationIdValue != null)
                                var educationOrganizationId = schoolDeptids.Where(x => x.DeptId.Equals(staffAssignmentNodeList.EducationOrganizationIdValue)&& x.OperationalStatus.Equals("Active")).FirstOrDefault();
                                if (educationOrganizationId != null)schoolid = educationOrganizationId.SchoolId;
                                    //Update StaffSchoolAssociation for staff schools
                                    UpdateStaffSchoolAssociation(token,schoolid, staffAssignmentNodeList);
                                    if (!string.IsNullOrEmpty(staffAssignmentNodeList.StaffUniqueIdValue) && !string.IsNullOrEmpty(staffAssignmentNodeList.BeginDateValue) && !string.IsNullOrEmpty(staffAssignmentNodeList.StaffClassification) && !string.IsNullOrEmpty(staffAssignmentNodeList.PositionCodeDescription))
                        StaffUniqueIdValue = staffNode.SelectSingleNode("StaffUniqueId").InnerText ?? null,
                        EducationOrganizationIdValue = EducationNode.SelectSingleNode("EducationOrganizationId").InnerText ?? null,
                        EndDateValue = EmploymentNode.SelectSingleNode("EndDate").InnerText ?? null,
                        BeginDateValue = EmploymentNode.SelectSingleNode("BeginDate").InnerText ?? null,
                        HireDateValue = EmploymentNode.SelectSingleNode("HireDate").InnerText ?? null,
                        PositionCodeDescription = EmploymentNode.SelectSingleNode("PostionTitle").InnerText ?? null,
                        JobOrderAssignment = EmploymentNode.SelectSingleNode("OrderOfAssignment").InnerText ?? null,
                        StaffClassification = staffClassificationNode.SelectSingleNode("CodeValue").InnerText ?? null,
                        EmpDesc = EmploymentStatus.SelectSingleNode("CodeValue").InnerText ?? null,

        private static void UpdateStaffSchoolAssociation(string token, string schoolid, StaffAssignmentAssociationData staffData)
        {
            IRestResponse response = null;
            try
            {
                
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssociationUrl +Constants.programAssignmentDescriptor+Constants.schoolId+ schoolid + Constants.staffUniqueId + staffData.StaffUniqueIdValue);
                response = GetData(client, token);
                var rootObject = new StaffSchoolAssociation
                {
                    SchoolReference = new EdFiSchoolReference
                    {
                        schoolId = schoolid,
                        Link = new Link()
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    SchoolYearTypeReference = new EdFiSchoolYearTypeReference
                    {
                        SchoolYear = GetSchoolYear().ToString(),

                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },

                    StaffReference = new EdFiStaffReference
                    {
                        staffUniqueId = staffData.StaffUniqueIdValue,
                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },
                    ProgramAssignmentDescriptor = "Regular Education",
                };
               
                if (IsSuccessStatusCode((int)response.StatusCode))
                {                    
                    if (response.Content.Length <= 2)
                    { 
                        string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                        response = PostData(json, client, token);
                    }
                    Log.Info("Updating  edfi.StaffAssociation for Schoolid : " + schoolid);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error updating  edfi.StaffAssociation for Schoolid : " + schoolid + " Exception : " + ex.Message);
                Log.Error(ex.Message);
            }
            
        }

        private static int GetSchoolYear() {
            var date = DateTime.Today;
            var month = date.Month;
            int year = date.Year;
            try
            {
                if (month <= 6)               
                    year = date.Year;
                else               
                    year = date.Year + 1;              
            }
            catch (Exception ex)
            {
               Log.Error("Error getting schoolYear :  Exception : " + ex.Message);
            }
            return year;
        }


                Log.Error("Error getting Assignment EndDate from StaffAssignment table :  Exception : " + ex.Message);
                        StaffUniqueId = staffUniqueIdValue,
                        FirstName = fname,
                        LastSurname = lname,
                        BirthDate = birthDate
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssignmentUrl + Constants.educationOrganizationId + educationOrganizationId + Constants.beginDate + staffData.BeginDateValue + Constants.staffClassificationDescriptorId + staffData.StaffClassification + Constants.staffUniqueId + staffData.StaffUniqueIdValue);
                                staffUniqueId = staffData.StaffUniqueIdValue,
                                staffUniqueId = staffData.StaffUniqueIdValue,
                                employmentStatusDescriptor = staffData.EmpDesc,
                                hireDate = staffData.HireDateValue,
                            staffClassificationDescriptor = staffData.StaffClassification,
                            beginDate = staffData.BeginDateValue,
                            endDate = staffData.EndDateValue,
                            orderOfAssignment = staffData.JobOrderAssignment,
                            positionTitle = staffData.PositionCodeDescription
                Log.Error(" Error updating  StaffEducationOrganizationAssignmentAssociation for Staff Id : " + staffData.StaffUniqueIdValue +ex.Message);
                        staffUniqueId = staffData.StaffUniqueIdValue,
                        staffUniqueId = staffData.StaffUniqueIdValue,
                        employmentStatusDescriptor = staffData.EmpDesc,
                        hireDate = staffData.HireDateValue,
                    staffClassificationDescriptor = staffData.StaffClassification,
                    beginDate = staffData.BeginDateValue,
                    endDate = staffData.EndDateValue,
                    orderOfAssignment = staffData.JobOrderAssignment,
                    positionTitle = staffData.PositionCodeDescription
                Log.Info("Updating  StaffEducationOrganizationAssignmentAssociation for Staff Id : " + staffData.StaffUniqueIdValue);
                Log.Error(" Error updating  StaffEducationOrganizationAssignmentAssociation for Staff Id : " + staffData.StaffUniqueIdValue);
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
                sw.WriteLine("{0},{1},{2},{3},{4}", errorLog.StaffUniqueId, errorLog.EndDate, errorLog.ErrorMessage);
                sw.Close();
            }


        }
    }
}
