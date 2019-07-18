using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPS.EdOrg.Loader
{
    
        public class StaffDescriptor
        {

            public string id { get; set; }
            public EdFiEducationReference educationOrganizationReference { get; set; }            
            public EdFiStaffReference staffReference { get; set; }            
            public string hireDate { get; set; }
            public string  endDate { get; set; }
            public string employmentStatusDescriptor { get; set; }
        
        }

        public class EdFiEducationReference
        {
            public string educationOrganizationId { get; set; }
            public Link Link { get; set; }
        }

        public class EdFiStaffReference
        {
           public string staffUniqueId { get; set; }
           public Link Link { get; set; }
        }

        public class Link
        {
            public string Rel { get; set; }
            public string Href { get; set; }
        }

    }

