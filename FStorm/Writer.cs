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


        public string WriteResult(CommandResult<CompilerContext> result)
        {
            MemoryStream stream = new MemoryStream();
            Message message = new Message(stream);
            ODataMessageWriterSettings settings = new ODataMessageWriterSettings();
            settings.ODataUri = new ODataUri 
            { 
                ServiceRoot = new Uri(service.options.ServiceRoot),
                Path = result.Context.Output.ODataPath,
                SelectAndExpand = null,
                Apply = null
            };
            ODataMessageWriter writer = new ODataMessageWriter(message, settings, service.Model);
            IEdmEntitySet entitySet = service.Model.EntityContainer.EntitySets().
                Where(x => x.EntityType == result.Context.GetOutputType()).First();

            switch (result.Context.GetOutputKind())
            {
                case OutputType.Collection:
                    WriteCollection(writer.CreateODataResourceSetWriter(entitySet), result);
                    break;
                case OutputType.Object:
                case OutputType.Property:
                    WriteObject(writer.CreateODataResourceWriter(entitySet), result);
                    break;
                case OutputType.RawValue:
                    if (result.Value != null) {
                        writer.WriteValue(result.Value.First().First().Value);//;
                    }
                    break;
                default:
                    break;
            }

            stream.Position = 0;
            StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        protected void WriteCollection(ODataWriter odataWriter, CommandResult<CompilerContext> result) 
        {
            ODataResourceSet set = new ODataResourceSet();
            odataWriter.WriteStart(set);
            WriteObject(odataWriter, result);
            odataWriter.WriteEnd();
        }

        protected void WriteObject(ODataWriter odataWriter, CommandResult<CompilerContext> result) 
        {
            if (result.Value != null)
            {
                foreach (var item in result.Value.ToDataObjects()) {
                    ODataResource entity = new ODataResource();
                    List<ODataProperty> entityProperties = new List<ODataProperty>();
                    foreach (var prop in item)
                    {
                        if (prop.Value != null && prop.Value.GetType() == typeof(DataObjects))
                        {

                        }
                        else
                        {
                            entityProperties.Add(new ODataProperty() { Name = prop.Key, Value = prop.Value });
                        }
                    }

                    entity.Properties= entityProperties;
                    odataWriter.WriteStart(entity);
                    odataWriter.WriteEnd();
                }
            }
        }
    }
}
