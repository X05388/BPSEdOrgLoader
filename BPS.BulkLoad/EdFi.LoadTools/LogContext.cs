using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using EdFi.LoadTools.Engine;

namespace EdFi.LoadTools
{
    public static class LogContext
    {
        public static void SetContextPrefix(IResource resource)
        {
            SetContextPrefix(resource.InterchangeName, resource.SourceFileName, resource.ElementName, resource.HashString);
            log4net.ThreadContext.Properties["XElement"] = resource.XElement;
            log4net.ThreadContext.Properties["Json"] = resource.Json;
            if (resource.Responses.Count != 0)
            {
                log4net.ThreadContext.Properties["StatusCode"] = resource.Responses?.Last().StatusCode;
                log4net.ThreadContext.Properties["Message"] = JObject.Parse(resource.Responses?.Last().Content)["message"].ToString();
            }
        }

        public static void SetContextPrefix(string interchangeName, string fileName = null, string resourceName = null,
            string resourceHash = null)
        {
            log4net.ThreadContext.Properties["InterchangeName"] = interchangeName;
            log4net.ThreadContext.Properties["FileName"] = fileName;
            log4net.ThreadContext.Properties["ResourceName"] = resourceName;
            log4net.ThreadContext.Properties["ResourceHash"] = resourceHash;
        }

        public static IDisposable SetInterchangeName(string interchangeName)
        {
            return SetThreadContext(interchangeName);
        }

        public static IDisposable SetFileName(string fileName)
        {
            return SetThreadContext(fileName);
        }

        public static IDisposable SetResourceName(string resourceName)
        {
            return SetThreadContext(resourceName);
        }

        public static IDisposable SetResourceHash(string resourceHash)
        {
            return SetThreadContext(resourceHash);
        }

        private static IDisposable SetThreadContext(string message, string context = "NDC")
        {
            return log4net.LogicalThreadContext.Stacks[context].Push(message);
        }
    }
}