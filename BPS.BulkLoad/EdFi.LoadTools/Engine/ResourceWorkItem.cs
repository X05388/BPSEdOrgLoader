using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace EdFi.LoadTools.Engine
{
    public class ResourceWorkItem : IResource
    {
        private class Response : IResponse
        {
            public string Content { get; set; }
            public string ErrorMessage { get; set; }
            public bool IsSuccess { get; set; }
            public HttpStatusCode StatusCode { get; set; }
        }
        
        private byte[] _hash = { };
        private string _hashString;
        private readonly string _interchangeName;
        private readonly string _sourceFileName;
        private readonly List<IResponse> _responses = new List<IResponse>();
        private readonly XElement _xElement;
        private XElement _jElement = new XElement("empty");

        public string ResourceType => _xElement.Name.LocalName;

        string IResource.ElementName => _xElement.Name.LocalName;

        byte[] IResource.Hash => _hash;
        string IResource.HashString => _hashString;
        string IResource.InterchangeName => _interchangeName;
        string IResource.SourceFileName => _sourceFileName;
        string IResource.Json => JsonConvert.SerializeXNode(_jElement, Formatting.None, true);
        IList<IResponse> IResource.Responses => _responses;
        XElement IResource.XElement => _xElement;

        public void SetJsonXElement(XElement xElement)
        {
            _jElement = xElement;
        }

        public void AddSubmissionResult(HttpResponseMessage response)
        {
            var tmpResponse = new Response
            {
                IsSuccess = response.IsSuccessStatusCode,
                StatusCode = response.StatusCode,
                ErrorMessage = response.ReasonPhrase,
                Content = response.Content?.ReadAsStringAsync().Result
            };
            _responses.Add(tmpResponse);
        }

        public ResourceWorkItem(string interchangeName, string sourceFileName, XElement xElement)
        {
            _interchangeName = interchangeName;
            _sourceFileName = sourceFileName;
            _xElement = xElement;
        }

        public void SetHash(byte[] hash)
        {
            Array.Resize(ref _hash, hash.Length);
            hash.CopyTo(_hash, 0);
            _hashString = BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }
}