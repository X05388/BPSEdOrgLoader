using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Schema;
using log4net;
using EdFi.LoadTools.ApiClient;

namespace EdFi.LoadTools.Engine.Factories
{
    public class XsdMetadataFactory : IMetadataFactory<XmlModelMetadata>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(XsdMetadataFactory).Name);
        public XmlSchemaSet Schemas { get; }

        public XsdMetadataFactory(XmlSchemaSet schemaSet)
        {
            Schemas = schemaSet;
        }

        private readonly Regex _typeRegex = new Regex(Constants.TypeRegex);

        public IEnumerable<XmlModelMetadata> GetMetadata()
        {
            Log.Info("Loading XSD Metadata");
            var results = new List<XmlModelMetadata>();
            foreach (XmlSchema schema in Schemas.Schemas())
            {
                foreach (XmlSchemaElement element in schema.Elements.Values)
                {
                    var match = _typeRegex.Match(element.Name);
                    var name = match.Success ? match.Groups["TypeName"].Value : element.Name;

                    AddElementMetadata(element.Name, element, results);
                }
            }

            return results.Where(x =>
                !string.IsNullOrEmpty(x.Model) &&
                !string.IsNullOrEmpty(x.Property) &&
                !string.IsNullOrEmpty(x.Type)
                )
                .Distinct(new ModelMetadataEqualityComparer<XmlModelMetadata>())
                .OrderBy(x => x.Model).ToArray();
        }

        private void AddElementMetadata(string model, XmlSchemaObject obj, List<XmlModelMetadata> results)
        {
            while (true)
            {
                if (obj is XmlSchemaComplexType)
                {
                    var ct = (XmlSchemaComplexType)obj;
                    obj = ct.ContentTypeParticle;
                    continue;
                }
                else if (obj is XmlSchemaChoice)
                {
                    var choice = (XmlSchemaChoice)obj;
                    foreach (var item in choice.Items.Cast<XmlSchemaObject>())
                    {
                        AddElementMetadata(model, item, results);
                    }
                }
                else if (obj is XmlSchemaSequence)
                {
                    var seq = (XmlSchemaSequence)obj;
                    foreach (var item in seq.Items.Cast<XmlSchemaObject>())
                    {
                        AddElementMetadata(model, item, results);
                    }
                }
                else if (obj is XmlSchemaElement)
                {
                    var ele = (XmlSchemaElement)obj;
                    var part = (XmlSchemaParticle)obj;
                    var st = ele.ElementSchemaType as XmlSchemaSimpleType;
                    if ((ele.ElementSchemaType.Name?.EndsWith("LookupType") ?? false) ||
                        (ele.ElementSchemaType.Name?.Equals("Citizenship") ?? false) ||
                        ((model?.EndsWith("Descriptor") ?? false) && ele.Name == "PriorDescriptor")) return;

                    var modelMatch = _typeRegex.Match(model ?? string.Empty);
                    model = modelMatch.Success ? modelMatch.Groups["TypeName"].Value : model;

                    var typeMatch = _typeRegex.Match(ele.ElementSchemaType.Name ?? string.Empty);
                    var type = typeMatch.Success ? typeMatch.Groups["TypeName"].Value : ele.ElementSchemaType.Name;

                    results.Add(new XmlModelMetadata
                    {
                        Model = model,
                        Property = ele.Name,
                        Type = st?.TypeCode.ToString() ?? type,
                        IsAttribute = false,
                        IsArray = part.MaxOccurs > 1,
                        IsRequired = part.MinOccurs > 0,
                        IsSimpleType = st != null
                    });

                    if (st == null)
                    {
                        model = ele.ElementSchemaType.Name;
                        obj = ele.ElementSchemaType;
                        continue;
                    }
                }
                else
                {
                    throw new Exception("Unexpected XML Element Type found in XSD");
                }
                break;
            }
        }
    }
}
