using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using Aqua.GraphCompare;
using EdFi.LoadTools.Engine;
using EdFi.LoadTools.Engine.Factories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EdFi.LoadTools.Test
{
    public class InterchangeElementOrderTests
    {
        [TestClass]
        public class WhenSerializingInterchangeOrder
        {
            private readonly InterchangeElementOrder _object = new InterchangeElementOrder
            {
                Interchanges = new List<Interchange>
                {
                    new Interchange
                    {
                        Name = "Interchange1",
                        Order = 1,
                        Elements = new List<Element>
                        {
                            new Element {Name = "Element1a"},
                            new Element {Name = "Element1b"}
                        }
                    },
                    new Interchange
                    {
                        Name = "Interchange2",
                        Order = 2,
                        Elements = new List<Element>
                        {
                            new Element {Name = "Element2a" },
                            new Element {Name = "Element2b" }
                        }
                    }
                }
            };

            private readonly XElement _xElement = new XElement("Interchanges",
                new XElement("Interchange",
                    new XAttribute("name", "Interchange1"), new XAttribute("order", 1),
                    new XElement("Element", new XAttribute("name", "Element1a")),
                    new XElement("Element", new XAttribute("name", "Element1b"))
                    ),
                new XElement("Interchange",
                    new XAttribute("name", "Interchange2"), new XAttribute("order", 2),
                    new XElement("Element", new XAttribute("name", "Element2a")),
                    new XElement("Element", new XAttribute("name", "Element2b"))
                    )
                );

            private readonly string _xmlString = "<Interchanges> " +
                                              "    <Interchange name = \"Interchange2\" order=\"2\"> " +
                                              "        <Element name = \"Element2a\" /> " +
                                              "        <Element name=\"Element2b\" /> " +
                                              "    </Interchange>" +
                                              "    <Interchange name=\"Interchange1\" order=\"1\"> " +
                                              "        <Element name = \"Element1a\" /> " +
                                              "        <Element name=\"Element1b\" /> " +
                                              "    </Interchange> " +
                                              "</Interchanges>";

            [TestMethod]
            public void Should_serialize_to_conforming_Xml()
            {
                var serializer = new XmlSerializer(typeof(InterchangeElementOrder));
                var ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                var builder = new StringBuilder();
                var writer = new StringWriter(builder);
                serializer.Serialize(writer, _object, ns);
                var serializedXml = XElement.Parse(builder.ToString());
                System.Console.WriteLine(serializedXml);

                Assert.IsTrue(XNode.DeepEquals(_xElement, serializedXml));
            }

            [TestMethod]
            public void Should_deserialize_to_object()
            {
                var serializer = new XmlSerializer(typeof(InterchangeElementOrder));
                var reader = new StringReader(_xmlString);
                var target = (InterchangeElementOrder)serializer.Deserialize(reader);
                var result = new GraphComparer().Compare(_object, target);
                Assert.IsTrue(result.IsMatch);
            }
        }

        [TestClass]
        public class WhenLoadingLoadOrderFileStreamFactory
        {
            private readonly IInterchangeLoadOrderStreamFactory _factory =
                new InterchangeLoadOrderFileStreamFactory(JsonMetadataTests.InterchangeOrderConfiguration);

            private readonly XmlSchemaSet _schemaSet = new XmlSchemaSet();

            private Interchange[] _value;

            [TestInitialize]
            public void Setup()
            {
                var orderFactory = new InterchangeElementOrderFactory(_factory, _schemaSet);
                _value = orderFactory.GetInterchangeElementOrder().ToArray();
            }

            [TestMethod]
            public void Should_return_serializable_interchange_order_file()
            {
                Assert.IsTrue(_value.Length > 0);
            }

            [TestMethod]
            public void Should_sort_by_order()
            {
                var order = int.MinValue;
                foreach (var interchange in _value)
                {
                    Assert.IsTrue(order <= interchange.Order);
                    order = interchange.Order;
                }
            }
        }
    }
}
