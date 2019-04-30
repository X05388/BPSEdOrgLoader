using System;
using System.Linq;
using System.Xml.Schema;

namespace EdFi.LoadTools.Engine.Factories
{
    public class SchemaSetFactory
    {
        private readonly XsdStreamsRetriever _streamsRetriever;
        public SchemaSetFactory(XsdStreamsRetriever streamsRetriever)
        {
            _streamsRetriever = streamsRetriever;
        }

        public XmlSchemaSet GetSchemaSet()
        {
            var streams = _streamsRetriever.GetStreams();
            var set = new XmlSchemaSet();
            var schemas = streams.Select(x => XmlSchema.Read(x, (s, e) =>
            {
                Console.WriteLine(e.Message);
            }));

            foreach (var schema in schemas)
            {
                set.Add(schema);
            }
            set.Compile();
            return set;
        }
    }
}
