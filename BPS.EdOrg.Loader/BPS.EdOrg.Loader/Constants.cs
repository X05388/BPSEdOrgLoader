using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPS.EdOrg.Loader
{
    class Constants
    {
        public static string educationOrganizationId = @"?educationOrganizationId=350000";
        public static string employmentStatusDescriptor = @"&employmentStatusDescriptor="+Constants.employmentStatusDescriptorValue;
        public static string employmentStatusDescriptorValue = @"Tenured%20or%20permanent";
        public static string hireDate = @"&hireDate=";
        public static string staffUniqueId = @"&staffUniqueId=";
    }
}
