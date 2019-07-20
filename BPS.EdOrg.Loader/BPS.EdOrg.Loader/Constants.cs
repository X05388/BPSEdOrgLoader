using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPS.EdOrg.Loader
{
    class Constants
    {
        public static string educationOrganizationId = @"?educationOrganizationId="+Constants.educationOrganizationIdValue;
        public static string educationOrganizationIdValue = @"350000";
        public static string employmentStatusDescriptor = @"&employmentStatusDescriptor="+Constants.employmentStatusDescriptorValue;
        public static string employmentStatusDescriptorValue = @"Tenured%20or%20permanent";
        public static string hireDate = @"&hireDate=";
        public static string staffUniqueId = @"&staffUniqueId=";

        public static string LOG_FILE { get; set; } = ConfigurationManager.AppSettings["LogFileDrive"] + DateTime.Today.ToString("yyyyMMdd") + ".csv";
        public static string LOG_FILE_ATT { get; set; } = @"Log File";
        public static string EmailFromAddress = ConfigurationManager.AppSettings["EmailFromAddr"];
        public static string LOG_FILE_REC { get; set; } = ConfigurationManager.AppSettings["ReviewTeam"];
        public static string LOG_FILE_SUB { get; set; } = @"EndDateDataReview";
        public static string LOG_FILE_BODY { get; set; } = @"EndDateDataReview Log File";

        public static string SmtpServerHost = ConfigurationManager.AppSettings["SmtpServerHost"];
    }
}
