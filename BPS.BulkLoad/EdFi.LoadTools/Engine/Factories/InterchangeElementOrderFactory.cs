using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Schema;
using System.Xml.Serialization;
using log4net;

namespace EdFi.LoadTools.Engine.Factories
{
    public class InterchangeElementOrderFactory : IInterchangeElementOrderFactory
    {
        private readonly IInterchangeLoadOrderStreamFactory _interchangeLoadOrderStreamFactory;
        private readonly XmlSchemaSet _schemaSet;
        private List<Interchange> _interchanges;
        private ILog Log => LogManager.GetLogger(this.GetType().Name);

        private readonly Regex _typeRegex = new Regex(Constants.TypeRegex);

        public InterchangeElementOrderFactory(IInterchangeLoadOrderStreamFactory interchangeLoadOrderStreamFactory,
            XmlSchemaSet schemaSet)
        {
            _interchangeLoadOrderStreamFactory = interchangeLoadOrderStreamFactory;
            _schemaSet = schemaSet;
        }

        public IEnumerable<Interchange> GetInterchangeElementOrder()
        {
            if (_interchanges == null)
            {
                Log.Debug(MethodBase.GetCurrentMethod().Name);
                var serializer = new XmlSerializer(typeof(InterchangeElementOrder));
                using (var stream = _interchangeLoadOrderStreamFactory.GetStream())
                {
                    var result = (InterchangeElementOrder)serializer.Deserialize(stream);
                    _interchanges = result.Interchanges.OrderBy(x => x.Order).ToList();
                }

                foreach (XmlSchema schema in _schemaSet.Schemas())
                {
                    foreach (XmlSchemaElement element in schema.Elements.Values)
                    {
                        var match = _typeRegex.Match(element.Name);
                        var name = (match.Success ? match.Groups["TypeName"].Value : element.Name).Replace("Interchange", "");
                        if (_interchanges.All(i => i.Name != name))
                            _interchanges.Add(new Interchange { Name = name, Order = 99, Elements = new List<Element>() });
                        var interchange = _interchanges.Single(i => i.Name == name);
                        var otherElementNames = GetUnorderedElementNames(element.ElementSchemaType)
                            .Except(interchange.Elements.Select(x => x.Name));
                        interchange.Elements.AddRange(otherElementNames.Select(x => new Element { Name = x }));
                    }
                }
            }
            return _interchanges;
        }

        private IEnumerable<string> GetUnorderedElementNames(XmlSchemaObject obj)
        {
            var result = new List<string>();
            if (obj is XmlSchemaComplexType)
            {
                var ct = (XmlSchemaComplexType)obj;
                obj = ct.ContentTypeParticle;
            }

            if (obj is XmlSchemaChoice)
            {
                var choice = (XmlSchemaChoice)obj;
                foreach (var item in choice.Items.Cast<XmlSchemaObject>())
                {
                    result.AddRange(GetUnorderedElementNames(item));
                }
            }
            else if (obj is XmlSchemaSequence)
            {
                var seq = (XmlSchemaSequence)obj;
                foreach (var item in seq.Items.Cast<XmlSchemaObject>())
                {
                    result.AddRange(GetUnorderedElementNames(item));
                }
            }
            else if (obj is XmlSchemaElement)
            {
                var ele = (XmlSchemaElement)obj;
                result.Add(ele.Name);
            }
            return result;
        }
    }

    [XmlRoot("Interchanges")]
    public class InterchangeElementOrder
    {
        [XmlElement("Interchange")]
        public List<Interchange> Interchanges;
    }

    public class Interchange
    {
        [XmlAttribute("name")]
        public string Name;

        [XmlAttribute("order")]
        public int Order;

        [XmlElement("Element")]
        public List<Element> Elements;

        [XmlIgnore]
        public string Filename => $"{Name}.xml";
    }

    public class Element
    {
        [XmlAttribute("name")]
        public string Name;
    }
}
