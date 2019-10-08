using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using System.Xml;
using System.IO;
using System.Configuration;
using BPS.EdOrg.Loader.Models;

namespace BPS.EdOrg.Loader.XMLDataLoad
{
    class StudentSpecialEducationProgramXML
    {
        private readonly Configuration _configuration = null;
        private readonly ILog _log;
        public StudentSpecialEducationProgramXML(Configuration configuration, ILog logger)
        {
            _configuration = configuration;
            _log = logger;
        }

        public void CreateStudentSpcialEducationXml()
        {
        }
        public void CreateStudentSpcialEducatioXmlJob()
        { }
        private void CreateStudentSpecialEducationNodeJob(StaffAssociationData staffData, string descCode, string empCode, string jobIndicator, XmlTextWriter writer)
        { }

    }
}
