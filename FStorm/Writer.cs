using Microsoft.OData.Edm;
using Microsoft.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FStorm
{
    public class Message : IODataResponseMessage
    {
        private Dictionary<string, string> _headers = new Dictionary<string, string>();
        private Stream _stream;
        public Message(Stream stream)
        {
            _stream = stream;
        }

        public IEnumerable<KeyValuePair<string, string>> Headers => _headers;

        public int StatusCode { get; set; }

        public string GetHeader(string headerName) => _headers.ContainsKey("headerName") ? _headers[headerName] : string.Empty;

        public void SetHeader(string headerName, string headerValue)
        {
            _headers.Add(headerName, headerValue);
        }

        public Stream GetStream() => _stream;
    }

    public class Writer
    {
        private readonly FStormService service;

        public Writer(FStormService service) 
        {
            this.service = service;
        }


        public string WriteResult(CommandResult<CompilerContext<GetConfiguration>> result)
        {
            MemoryStream stream = new MemoryStream();
            Message message = new Message(stream);
            ODataMessageWriterSettings settings = new ODataMessageWriterSettings();
            settings.ODataUri = new ODataUri { ServiceRoot = new Uri(service.options.ServiceRoot) };
            ODataMessageWriter writer = new ODataMessageWriter(message, settings, service.Model);
            IEdmEntitySet entitySet = service.Model.EntityContainer.EntitySets().
                Where(x => x.EntityType == result.Context.Resource.ResourceEdmType).First();
            //ODataWriter odataWriter = writer.CreateODataResourceSetWriter(entitySet);
            

            switch (result.Context.Resource.ResourceType)
            {
                case ResourceType.Collection:
                    //WriteCollection(odataWriter);
                    break;
                case ResourceType.Object:
                    WriteObject(writer.CreateODataResourceWriter(entitySet), result);
                    break;
                case ResourceType.Property:
                    //WriteValue(odataWriter);
                    break;
                default:
                    break;
            }

            

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        protected ODataWriter WriteCollection(ODataWriter odataWriter) 
        {
            ODataResourceSet set = new ODataResourceSet();
            odataWriter.WriteStart(set);
            odataWriter.WriteEnd();
            throw new NotImplementedException();
        }

        protected ODataWriter WriteObject(ODataWriter odataWriter, CommandResult<CompilerContext<GetConfiguration>> result) 
        {
            ODataResource entity = new ODataResource();
            if (result.Value != null)
            {
                entity.Properties = result.Value.First()
                    .Select(x => new ODataProperty() { Name = x.Key.Last().ToString(), Value = x.Value })
                    .ToList();
            }
            else { 
                entity.Properties = new List<ODataProperty>();
            }
            odataWriter.WriteStart(entity);
            odataWriter.WriteEnd();
            return odataWriter;
        }
        protected ODataWriter WriteValue(ODataWriter odataWriter) { throw new NotImplementedException(); }

    }
}
