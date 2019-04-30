using System.Collections.Generic;
using log4net.Appender;
using log4net.Core;

namespace EdFi.LoadTools.Test
{
    public class TestAppender : IAppender
    {
        public TestAppender()
        {
            Logs = new List<LoggingEvent>();
        }

        void IAppender.DoAppend(LoggingEvent loggingEvent)
        {
            Logs.Add(loggingEvent);
        }

        void IAppender.Close() { }
        string IAppender.Name { get; set; }

        public List<LoggingEvent> Logs { get; private set; }

        public void AttachToRoot()
        {
            ((log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository()).Root.AddAppender(this);
        }

        public void DetachFromRoot()
        {
            ((log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository()).Root.RemoveAppender(this);
        }
    }
}