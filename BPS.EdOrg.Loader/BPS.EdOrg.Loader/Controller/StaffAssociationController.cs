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

namespace BPS.EdOrg.Loader.Controller
{

    class StaffAssociationController
    {

        private RestServiceManager _restServiceManager;
        private Notification _notification;
        private readonly ILog _log;
        private ParseXmls _prseXML;
        private EdFiApiCrud _edfiApi ;
        public StaffAssociationController(string token, EdorgConfiguration configuration,ILog log)
        {
            _log = log;
            _prseXML = new ParseXmls(configuration, _log);
            _restServiceManager = new RestServiceManager(configuration, token, _log);
            _notification = new Notification();
            _edfiApi = new EdFiApiCrud();
        }
        /// <summary>
        /// Gets the data from the xml and updates StaffEducationOrganizationEmploymentAssociation table.
        /// </summary>
        /// <returns></returns>
        public void UpdateStaffEmploymentAssociationData(string token, EdorgConfiguration configuration)
        {
            try
            {
                XmlDocument xmlDoc = _prseXML.LoadStaffXml();
                _restServiceManager = new RestServiceManager(configuration, token, _log);
                //var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                //nsmgr.AddNamespace("a", "http://ed-fi.org/0220");
                var existingStaffIds = _restServiceManager.GetStaffList();
                var nodeList = xmlDoc.SelectNodes(@"//InterchangeStaffAssociation/StaffEducationOrganizationAssociation").Cast<XmlNode>().OrderBy(element => element.SelectSingleNode("EmploymentPeriod/EndDate").InnerText).ToList();

                foreach (XmlNode node in nodeList)
                {
                    // Extracting the data froom the XMl file
                    var staffEmploymentNodeList = GetEmploymentAssociationXml(node);

                    if (staffEmploymentNodeList != null)
                    {
                        if (staffEmploymentNodeList.status == "T")
                        {
                            int countStd = xmlDoc.SelectNodes(@"//InterchangeStaffAssociation/StaffEducationOrganizationAssociation/StaffReference/StaffIdentity/StaffUniqueId").Cast<XmlNode>().Where(a => a.InnerText == staffEmploymentNodeList.staffUniqueIdValue).Distinct().Count();
                            if (countStd > 1) staffEmploymentNodeList.endDateValue = null;

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
                                //Setting the Enddate with the one from AssignmentAssociation
                                if (endDate != null)
                                    staffEmploymentNodeList.endDateValue = endDate;
                                UpdateEndDate(token, id, staffEmploymentNodeList);
                            }

                        }
                    }

                }
                if (File.Exists(Constants.LOG_FILE))
                    _notification.SendMail(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
            }
            catch (Exception ex)
            {
                _log.Error("Error working on EmploymentAssociation Data : " + ex.Message);
            }
        }
 


        /// <summary>
        /// Gets the data from the xml file
        /// </summary>
        /// <returns></returns>
        private StaffEmploymentAssociationData GetEmploymentAssociationXml(XmlNode node)
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
                _log.Error("Error getting Emplyment data from StaffAssociation xml : Exception : " + ex.Message);
                return null;

            }
        }


        /// <summary>
        /// Gets the data from the xml and updates StaffEducationOrganizationAssignmentAssociation table.
        /// </summary>
        /// <returns></returns>
        public void UpdateStaffAssignmentAssociationData(string token, EdorgConfiguration configuration)
        {
            try
            {
                XmlDocument xmlDoc = _prseXML.LoadStaffXml();
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
                        if (staffAssignmentNodeList.EducationOrganizationIdValue != null)
                        {
                            if (schoolDeptids.Count > 0)
                            {
                                string schoolid = null;
                                // Getting the EdOrgId for the Department ID 
                                var educationOrganizationId = schoolDeptids.Where(x => x.DeptId.Equals(staffAssignmentNodeList.EducationOrganizationIdValue) && x.OperationalStatus.Equals("Active")).FirstOrDefault();

                                // setting the DeptId as EdOrgId for the staff, if no corresponding school is found
                                if (educationOrganizationId != null) schoolid = educationOrganizationId.SchoolId;
                                if (!string.IsNullOrEmpty(schoolid))
                                {
                                    UpdateStaffSchoolAssociation(token, schoolid, staffAssignmentNodeList);
                                    //Inserting new Assignments and updating the postioTitle with JobCode - JobDesc
                                    if (!string.IsNullOrEmpty(staffAssignmentNodeList.StaffUniqueIdValue) && !string.IsNullOrEmpty(staffAssignmentNodeList.BeginDateValue) && !string.IsNullOrEmpty(staffAssignmentNodeList.StaffClassification) && !string.IsNullOrEmpty(staffAssignmentNodeList.PositionCodeDescription))
                                    {
                                        string id = GetAssignmentAssociationId(token, schoolid, staffAssignmentNodeList);
                                        if (id != null)
                                            UpdatePostionTitle(token, id, schoolid, staffAssignmentNodeList);
                                    }
                                }

                            }

                        }
                    }


                }

                if (File.Exists(Constants.LOG_FILE))
                    _notification.SendMail(Constants.LOG_FILE_REC, Constants.LOG_FILE_SUB, Constants.LOG_FILE_BODY, Constants.LOG_FILE_ATT);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

        }

        private StaffAssignmentAssociationData GetAssignmentAssociationXml(XmlNode node)
        {
            try
            {
                StaffAssignmentAssociationData staffAssignmentList = null;
                XmlNode staffNode = node.SelectSingleNode("StaffReference/StaffIdentity");
                XmlNode EducationNode = node.SelectSingleNode("EducationOrganizationReference/EducationOrganizationIdentity");
                XmlNode EmploymentNode = node.SelectSingleNode("EmploymentPeriod");
                if (staffNode == null && EducationNode == null) _log.Error("Nodes not reurning any data for Assignment");
                XmlNode staffClassificationNode = node.SelectSingleNode("StaffClassification");
                XmlNode EmploymentStatus = node.SelectSingleNode("EmploymentStatus");
                if (staffNode != null && EducationNode != null)
                {
                    staffAssignmentList = new StaffAssignmentAssociationData
                    {
                        StaffUniqueIdValue = staffNode.SelectSingleNode("StaffUniqueId").InnerText ?? null,
                        EducationOrganizationIdValue = EducationNode.SelectSingleNode("EducationOrganizationId").InnerText ?? null,
                        EndDateValue = EmploymentNode.SelectSingleNode("EndDate").InnerText ?? null,
                        BeginDateValue = EmploymentNode.SelectSingleNode("BeginDate").InnerText ?? null,
                        HireDateValue = EmploymentNode.SelectSingleNode("HireDate").InnerText ?? null,
                        PositionCodeDescription = EmploymentNode.SelectSingleNode("PostionTitle").InnerText ?? null,
                        JobOrderAssignment = EmploymentNode.SelectSingleNode("OrderOfAssignment").InnerText ?? null,
                        StaffClassification = staffClassificationNode.SelectSingleNode("CodeValue").InnerText ?? null,
                        EmpDesc = EmploymentStatus.SelectSingleNode("CodeValue").InnerText ?? null,

                    };

                }
                return staffAssignmentList;
            }
            catch (Exception ex)
            {
                _log.Error("Error extracting data for AssignmentAssociation Exception : " + ex.Message);
                return null;

            }

        }
        private string GetAssignmentEndDate(string token, StaffEmploymentAssociationData staffData)
        {
            string date = null;
            try
            {
                IRestResponse response = null;

                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssignmentUrl + Constants.staffUniqueId1 + staffData.staffUniqueIdValue + Constants.employmentStatusDescriptor + staffData.empDesc);
                response = _edfiApi.GetData(client, token);
                if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
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
                _log.Error(ex.Message);
            }
            return date;
        }

        private void UpdatingNewStaffData(string token, string staffUniqueIdValue, string fname, string lname, string birthDate)
        {
            try
            {
                IRestResponse response = null;
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffUrl + Constants.staffUniqueId1 + staffUniqueIdValue);
                response = _edfiApi.GetData(client, token);
                if (!_restServiceManager.IsSuccessStatusCode((int)response.StatusCode) || (int)response.StatusCode == 404)
                {

                    //Insert Data 
                    var rootObject = new StaffDescriptor
                    {
                        StaffUniqueId = staffUniqueIdValue,
                        FirstName = fname,
                        LastSurname = lname,
                        BirthDate = birthDate

                    };

                    string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                    response = _edfiApi.PostData(json, client, token);
                    _log.Info("Updating  edfi.staff for Staff Id : " + staffUniqueIdValue);

                }
            }
            catch (Exception ex)
            {
                _log.Error("Error inserting staff in edfi.staff for Staff Id : " + staffUniqueIdValue + " Exception : " + ex.Message);

            }



        }
        /// <summary>
        /// Get the Id from the [StaffEducationOrganizationEmploymentAssociation] table.
        /// </summary>
        /// <returns></returns>
        private string GetEmploymentAssociationId(string token, StaffEmploymentAssociationData staffData)
        {
            IRestResponse response = null;

            var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffEmploymentUrl + Constants.educationOrganizationId + Constants.educationOrganizationIdValue + Constants.employmentStatusDescriptor + staffData.empDesc + Constants.hireDate + staffData.hireDateValue + Constants.staffUniqueId + staffData.staffUniqueIdValue);

            response = _edfiApi.GetData(client, token);
            if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
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
                _edfiApi.PostData(json, client, token);
                _log.Info("Inserted into StaffEducationOrganizationEmploymentAssociation for Staff Id : " + staffData.staffUniqueIdValue);
            }

            return null;
        }

        /// <summary>
        /// Get the Id from the [StaffEducationOrganizationAssignmentAssociation] table.
        /// </summary>
        /// <returns></returns>
        private  string GetAssignmentAssociationId(string token, string educationOrganizationId, StaffAssignmentAssociationData staffData)
        {
            IRestResponse response = null;
            try
            {
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssignmentUrl + Constants.educationOrganizationId + educationOrganizationId + Constants.beginDate + staffData.BeginDateValue + Constants.staffClassificationDescriptorId + staffData.StaffClassification + Constants.staffUniqueId + staffData.StaffUniqueIdValue);
                response = _edfiApi.GetData(client, token);
                if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
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

                            EducationOrganizationReference = new EdFiEducationReference
                            {
                                educationOrganizationId = educationOrganizationId,
                                Link = new Link()
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
                            EmploymentStaffEducationOrganizationEmploymentAssociationReference = new EdfiEmploymentAssociationReference
                            {
                                educationOrganizationId = Constants.educationOrganizationIdValue,
                                staffUniqueId = staffData.StaffUniqueIdValue,
                                employmentStatusDescriptor = staffData.EmpDesc,
                                hireDate = staffData.HireDateValue,
                                Link = new Link
                                {
                                    Rel = string.Empty,
                                    Href = string.Empty
                                }
                            },

                            StaffClassificationDescriptor = staffData.StaffClassification,
                            BeginDate = staffData.BeginDateValue,
                            EndDate = staffData.EndDateValue,
                            OrderOfAssignment = staffData.JobOrderAssignment,
                            PositionTitle = staffData.PositionCodeDescription
                        };

                        string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                        response = _edfiApi.PostData(json, client, token);

                    }


                }
            }

            catch (Exception ex)
            {
                _log.Error(" Error updating  StaffEducationOrganizationAssignmentAssociation for Staff Id : " + staffData.StaffUniqueIdValue + ex.Message);

            }
            return null;
        }

        /// <summary>
        /// Updates the Position title in  [StaffEducationOrganizationAssignmentAssociation] table.
        /// </summary>
        /// <returns></returns>
        private void UpdatePostionTitle(string token, string id, string educationOrganizationId, StaffAssignmentAssociationData staffData)
        {
            try
            {

                IRestResponse response = null;
                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssignmentUrl + "/" + id);
                var rootObject = new StaffAssignmentDescriptor
                {
                    id = id,
                    EducationOrganizationReference = new EdFiEducationReference
                    {
                        educationOrganizationId = educationOrganizationId,
                        Link = new Link()
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
                    EmploymentStaffEducationOrganizationEmploymentAssociationReference = new EdfiEmploymentAssociationReference
                    {
                        educationOrganizationId = Constants.educationOrganizationIdValue,
                        staffUniqueId = staffData.StaffUniqueIdValue,
                        employmentStatusDescriptor = staffData.EmpDesc,
                        hireDate = staffData.HireDateValue,
                        Link = new Link
                        {
                            Rel = string.Empty,
                            Href = string.Empty
                        }
                    },

                    StaffClassificationDescriptor = staffData.StaffClassification,
                    BeginDate = staffData.BeginDateValue,
                    EndDate = staffData.EndDateValue,
                    OrderOfAssignment = staffData.JobOrderAssignment,
                    PositionTitle = staffData.PositionCodeDescription
                };
                string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                response = _edfiApi.PutData(json, client, token);
                _log.Info("Updating  StaffEducationOrganizationAssignmentAssociation for Staff Id : " + staffData.StaffUniqueIdValue);

            }

            catch (Exception ex)
            {
                _log.Error(" Error updating  StaffEducationOrganizationAssignmentAssociation for Staff Id : " + staffData.StaffUniqueIdValue + ex.Message);

            }

        }

        /// <summary>
        /// Updates the enddate to [StaffEducationOrganizationAssignmentAssociation] table.
        /// </summary>
        /// <returns></returns>
        private void UpdateEndDate(string token, string id, StaffEmploymentAssociationData staffData)
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
                response = _edfiApi.PutData(json, client, token);
                _log.Info("Updated StaffEducationOrganizationEmploymentAssociation for Staff Id : " + staffData.staffUniqueIdValue);

            }

            catch (Exception ex)
            {
                _log.Error("Something went wrong while updating the data in ODS, check the XML values" + ex.Message);
            }

        }


        private void UpdateStaffSchoolAssociation(string token, string schoolid, StaffAssignmentAssociationData staffData)
        {
            IRestResponse response = null;
            try
            {

                var client = new RestClient(ConfigurationManager.AppSettings["ApiUrl"] + Constants.StaffAssociationUrl + Constants.programAssignmentDescriptor + Constants.schoolId + schoolid + Constants.staffUniqueId + staffData.StaffUniqueIdValue);
                response = _edfiApi.GetData(client, token);
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

                if (_restServiceManager.IsSuccessStatusCode((int)response.StatusCode))
                {
                    if (response.Content.Length <= 2)
                    {
                        string json = JsonConvert.SerializeObject(rootObject, Newtonsoft.Json.Formatting.Indented);
                        response = _edfiApi.PostData(json, client, token);
                    }
                    _log.Info("Updating  edfi.StaffAssociation for Schoolid : " + schoolid);
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error updating  edfi.StaffAssociation for Schoolid : " + schoolid + " Exception : " + ex.Message);
                _log.Error(ex.Message);
            }

        }

        private  int GetSchoolYear()
        {
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
                _log.Error("Error getting schoolYear :  Exception : " + ex.Message);
            }
            return year;
        }
        private List<SchoolDept> GetDeptList(EdorgConfiguration configuration)
        {

            List<SchoolDept> existingDeptIds = new List<SchoolDept>();
            try
            {
                TokenRetriever tokenRetriever = new TokenRetriever(ConfigurationManager.AppSettings["OAuthUrl"], ConfigurationManager.AppSettings["APP_Key"], ConfigurationManager.AppSettings["APP_Secret"]);

                string token = tokenRetriever.ObtainNewBearerToken();
                if (!string.IsNullOrEmpty(token))
                {
                    _log.Info($"Crosswalk API token retrieved successfully");
                    RestServiceManager restManager = new RestServiceManager(configuration, token, _log);
                    existingDeptIds = restManager.GetSchoolList();
                }
                else
                {
                    _log.Error($"Error while retrieving access token for Crosswalk API");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error while getting school list:{ex.Message}");
            }

            return existingDeptIds;
        }

        private  List<string> GetStaffList(EdorgConfiguration configuration)
        {

            List<string> existingStaffIds = new List<string>();
            try
            {
                TokenRetriever tokenRetriever = new TokenRetriever(ConfigurationManager.AppSettings["OAuthUrl"], ConfigurationManager.AppSettings["APP_Key"], ConfigurationManager.AppSettings["APP_Secret"]);

                string token = tokenRetriever.ObtainNewBearerToken();
                if (!string.IsNullOrEmpty(token))
                {
                    _log.Info($"Crosswalk API token retrieved successfully");
                    RestServiceManager restManager = new RestServiceManager(configuration, token, _log);
                    existingStaffIds = restManager.GetStaffList();
                }
                else
                {
                    _log.Error($"Error while retrieving access token for Crosswalk API");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error while getting school list:{ex.Message}");
            }

            return existingStaffIds;
        }
    }
}
