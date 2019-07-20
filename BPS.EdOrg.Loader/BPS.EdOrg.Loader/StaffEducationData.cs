using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Mail;


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

    public class ErrorLog
    {
        public string staffUniqueId { get; set; }
        public string endDate { get; set; }
        public string ErrorMessage { get; set; }

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

