using System.IO;
using log4net;

namespace EdFi.LoadTools.Engine.InterchangePipeline
{
    public class IsNotEmptyStep : IInterchangePipelineStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(IsNotEmptyStep).Name);
        public bool Process(string sourceFileName, Stream stream)
        {
            var result =  stream.Length > 0;
            if (result)
                Log.Info("not empty");
            else
                Log.Warn("empty");
            return result;
        }
    }
}
