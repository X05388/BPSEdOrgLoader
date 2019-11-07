using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPS.EdOrg.Loader.Models
{
        /// <summary>
        /// Service Descriptor
        /// </summary>
         public class ServiceDescriptor
         {
            public string CodeValue { get; set; }
            public string Description { get; set; }
            public string EffectiveBeginDate { get; set; }
            public string EffectiveEndDate { get; set; }
            public string Namespace { get; set; }
            public int PriorDescriptorId { get; set; }
            public string ShortDescription { get; set; }
         }

         /// <summary>
         /// Service
        /// </summary>
        public class Service
        {
            public string ServiceDescriptor { get; set; }
            public bool PrimaryIndicator { get; set; }
            public string ServiceBeginDate { get; set; }
            public string ServiceEndDate { get; set; }
        }


        public class StaffAlertDescriptor
        {

            public string id { get; set; }
            public EdFiEducationReference educationOrganizationReference { get; set; }
            public EdFiStaffReference staffReference { get; set; }
            public string hireDate { get; set; }
            public string endDate { get; set; }
            public string employmentStatusDescriptor { get; set; }

        }
        public class SpecialEducation
        {
        public SpecialEducation()
        { }
        public string EducationOrganizationId { get; set; }
        public string Type { get; set; }        
        public string Name { get; set; }
        public string StudentUniqueId { get; set; }        
        public string ServiceDescriptor { get; set; }
        public string BeginDate { get; set; }
        public string EndDate { get; set; }
        public bool IdeaEligibility { get; set; }
        public string IepBeginDate { get; set; }
        public string IepEndDate { get; set; }
        public string IepParentResponse { get; set; }
        public string IepSignatureDate { get; set; }
        public string Eligibility504 { get; set; }
        public string IepReviewDate { get; set; }
        public string lastEvaluationDate { get; set; }
        public bool medicallyFragile { get; set; }
        public bool multiplyDisabled { get; set; }
        public string reasonExitedDescriptor { get; set; }
        public double schoolHoursPerWeek { get; set; }
        public bool servedOutsideOfRegularSession { get; set; }
        public double specialEducationHoursPerWeek { get; set; }
        public string specialEducationSettingDescriptor { get; set; }


        }

}
