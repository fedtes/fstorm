using Microsoft.OData.Edm;
using Microsoft.OData;
using System.Text;

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


        public string WriteResult(CommandResult<CompilerContext<GetRequest>> result)
        {
            MemoryStream stream = new MemoryStream();
            Message message = new Message(stream);
            ODataMessageWriterSettings settings = new ODataMessageWriterSettings();
            settings.ODataUri = new ODataUri 
            { 
                ServiceRoot = new Uri(service.options.ServiceRoot),
                Path = result.Context.Resource.ODataPath,
                SelectAndExpand = null,
                Apply = null
            };
            ODataMessageWriter writer = new ODataMessageWriter(message, settings, service.Model);
            IEdmEntitySet entitySet = service.Model.EntityContainer.EntitySets().
                Where(x => x.EntityType == result.Context.Resource.ResourceEdmType).First();

            switch (result.Context.Resource.ResourceType)
            {
                case ResourceType.Collection:
                    WriteCollection(writer.CreateODataResourceSetWriter(entitySet), result);
                    break;
                case ResourceType.Object:
                case ResourceType.Property:
                    WriteObject(writer.CreateODataResourceWriter(entitySet), result);
                    break;
                default:
                    break;
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        protected void WriteCollection(ODataWriter odataWriter, CommandResult<CompilerContext<GetRequest>> result) 
        {
            ODataResourceSet set = new ODataResourceSet();
            odataWriter.WriteStart(set);
            if (result.Value != null)
            {
                foreach (var item in result.Value) {
                    ODataResource entity = new ODataResource();
                    entity.Properties= item
                        .Cells
                        .Select(x => new ODataProperty() { Name = x.Key.Last().ToString(), Value = x.Value })
                        .ToList();
                    odataWriter.WriteStart(entity);
                    odataWriter.WriteEnd();
                }

            }
            odataWriter.WriteEnd();
        }

        protected void WriteObject(ODataWriter odataWriter, CommandResult<CompilerContext<GetRequest>> result) 
        {
            ODataResource entity = new ODataResource();
            if (result.Value != null)
            {
                entity.Properties = result.Value.First()
                    .Cells
                    .Select(x => new ODataProperty() { Name = x.Key.Last().ToString(), Value = x.Value })
                    .ToList();
            }
            else { 
                entity.Properties = new List<ODataProperty>();
            }
            odataWriter.WriteStart(entity);
            odataWriter.WriteEnd();
        }
    }
}
