using log4net.Config;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EdFi.LoadTools.Test
{
    [TestClass]
    public class LogConfiguration
    {
        [AssemblyInitialize]
        public static void ConfigureLogging(TestContext context)
        {
            BasicConfigurator.Configure();
        }
    }
}
