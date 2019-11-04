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


}
