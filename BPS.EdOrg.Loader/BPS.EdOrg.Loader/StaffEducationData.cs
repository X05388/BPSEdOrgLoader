using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Mail;


namespace BPS.EdOrg.Loader
{

        public class StaffDescriptor
        {

            public string staffUniqueId { get; set; }            
            public string firstName { get; set; }
            public string lastSurname { get; set; }
            public string birthDate { get; set; }

        }   
        public class StaffEmploymentDescriptor
        {

            public string id { get; set; }
            public EdFiEducationReference educationOrganizationReference { get; set; }            
            public EdFiStaffReference staffReference { get; set; } 
            public string hireDate { get; set; }
            public string  endDate { get; set; }
            public string employmentStatusDescriptor { get; set; }
        
        }

        public class EdfiEmploymentAssociationReference
        {
            public string educationOrganizationId { get; set; }
            public string staffUniqueId { get; set; }
            public string hireDate { get; set; }            
            public string employmentStatusDescriptor { get; set; }
            public Link Link { get; set; }

    }
        public class StaffAssignmentDescriptor
        {

            public string id { get; set; }
            public EdFiEducationReference educationOrganizationReference { get; set; }
            public EdFiStaffReference staffReference { get; set; }
            public EdfiEmploymentAssociationReference employmentStaffEducationOrganizationEmploymentAssociationReference { get; set; }
            public string positionTitle { get; set; }
            public string beginDate { get; set; }
            public string endDate { get; set; }
            public string staffClassificationDescriptor { get; set; }
            public string orderOfAssignment { get; set; }
        

        }
        public class EdFiProgram
        {
            public EdFiEducationReference educationOrganizationReference { get; set; }
            public int? programId { get; set; }
            public string type { get; set; }
            public string sponsorType { get; set; }
            public string name { get; set; }
            public string ProgramEducationOrganizationId { get; set; }

        }
        public class SpecialEducationReference
        {
             public string id { get; set; }
             public EdFiEducationReference educationOrganizationReference { get; set; }
             public ProgramReference programReference { get; set; }
             public StudentReference studentReference { get; set; }
             public string beginDate { get; set; }
             public string endDate{ get; set; }
             public bool ideaEligibility { get; set; }
             public string iepBeginDate { get; set; }
             public string iepEndDate { get; set; }
             public string iepParentResponse { get; set; }
             public string iepSignatureDate { get; set; }
             public string Eligibility504 { get; set; }
             public string iepReviewDate { get; set; }
             public string lastEvaluationDate { get; set; }
             public bool medicallyFragile { get; set; }
             public bool multiplyDisabled { get; set; }
             public string reasonExitedDescriptor { get; set; }
             public int schoolHoursPerWeek { get; set; }
             public bool servedOutsideOfRegularSession{ get; set; }
             public int specialEducationHoursPerWeek{ get; set; }
             public string specialEducationSettingDescriptor{ get; set; }

            
        }   
        
        public class EdFiEducationReference
        {
            public string educationOrganizationId { get; set; }
            public Link Link { get; set; }
        }

         public class ProgramReference
         {
            public string educationOrganizationId { get; set; }
            public string type { get; set; }
            public string name { get; set; }
            public Link Link { get; set; }
         }
         public class EdFiStaffReference
         {
            public string staffUniqueId { get; set; }
            public Link Link { get; set; }
         }

        public class StudentReference
        {
           public string studentUniqueId  { get; set; }
           public Link Link { get; set; }
        }

        public class Link
        {
            public string Rel { get; set; }
            public string Href { get; set; }
        }

        public class SchoolDept
        {
            public string schoolId { get; set; }
            public string DeptId { get; set; }
        }

    public class ErrorLog
    {
        public string staffUniqueId { get; set; }
        public string endDate { get; set; }
        public string ErrorMessage { get; set; }
        public string PositionTitle { get; set; }

    }

    public class SendEmail
    {
        public string FromAddr { get; set; }

        public ArrayList ToAddr { get; set; }

        public string EmailSubject { get; set; }

        public string EmailContent { get; set; }

        public string BccToAdr { get; set; }
        public List<Attachment> AttachmentList { get; set; }
    }

}

