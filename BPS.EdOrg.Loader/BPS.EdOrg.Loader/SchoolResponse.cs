using System.Collections.Generic;

namespace BPS.EdOrg.Loader
{
    public class SchoolResponse
    {
        public List<EducationOrganizationIdentificationSystem> IdentificationCodes { get; set; }
    }

    public class EducationOrganizationIdentificationSystem
    {
        public string EducationOrganizationIdentificationSystemDescriptor { get; set; }
        public string IdentificationCode { get; set; }
    }

    public class StaffResponse
    {
        public List<string> staffUniqueId { get; set; }
    }

    
    
}
