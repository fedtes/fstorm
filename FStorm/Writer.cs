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


        public void WriteResult(CompilerContext context ,IEnumerable<IDictionary<string, object?>> data, Stream stream)
        {
            Message message = new Message(stream);
            ODataMessageWriterSettings settings = new ODataMessageWriterSettings();
            settings.ODataUri = new ODataUri 
            { 
                ServiceRoot = new Uri(service.options.ServiceRoot),
                Path = context.GetOdataRequestPath(),
                SelectAndExpand = context.GetSelectAndExpand(),
                Filter = context.GetFilterClause(),
                OrderBy = context.GetOrderByClause(),
                Skip = context.GetPaginationClause().Skip,
                Top = context.GetPaginationClause().Top
            };
            ODataMessageWriter writer = new ODataMessageWriter(message, settings, service.Model);
            IEdmEntitySet entitySet = service.Model.EntityContainer.EntitySets().
                Where(x => x.EntityType == context.GetOutputType()).First();

            switch (context.GetOutputKind())
            {
                case OutputKind.Collection:
                    WriteCollection(writer.CreateODataResourceSetWriter(entitySet), data);
                    break;
                case OutputKind.Object:
                case OutputKind.Property:
                    WriteObject(writer.CreateODataResourceWriter(entitySet), data.First());
                    break;
                case OutputKind.RawValue:
                    writer.WriteValue(data.First().First().Value);
                    break;
                default:
                    break;
            }

            stream.Position = 0;
            // StreamReader reader = new StreamReader(stream);
            // return reader.ReadToEnd();
        }

        protected void WriteCollection(ODataWriter odataWriter, IEnumerable<IDictionary<string, object?>> data) 
        {
            ODataResourceSet set = new ODataResourceSet();
            odataWriter.WriteStart(set);
            foreach (var item in data)
            {
                WriteObject(odataWriter, item);
            }
            odataWriter.WriteEnd();
        }

        protected void WriteObject(ODataWriter odataWriter, IDictionary<string, object?> record) 
        {
            ODataResource entity = new ODataResource();
            List<ODataProperty> entityProperties = new List<ODataProperty>();


            foreach (var prop in record.Where(p => !(p.Value is IEnumerable<IDictionary<string, object?>> || p.Value is IDictionary<string, object?>)))
            {
                entityProperties.Add(new ODataProperty() { Name = prop.Key, Value = prop.Value });
            }
            entity.Properties= entityProperties;
            odataWriter.WriteStart(entity);

            foreach(var prop in record.Where(p => p.Value is IDictionary<string, object?>))
            {
                odataWriter.WriteStart(new ODataNestedResourceInfo
                {
                    Name = prop.Key,
                    IsCollection = false
                });
                WriteObject(odataWriter, (IDictionary<string, object?>)prop.Value ?? new Dictionary<string,object?>());
                odataWriter.WriteEnd();
            }

            foreach(var prop in record.Where(p => p.Value is IEnumerable<IDictionary<string, object?>>))
            {
                odataWriter.WriteStart(new ODataNestedResourceInfo
                {
                    Name = prop.Key,
                    IsCollection = true
                });
                WriteCollection(odataWriter, (IEnumerable<IDictionary<string, object?>>)prop.Value ?? Enumerable.Empty<IDictionary<string, object?>>());
                odataWriter.WriteEnd();
            }
            odataWriter.WriteEnd();
        }
    
    
    
        internal void WriteServiceDocument(Stream stream)
        {
            Message message = new Message(stream);
            ODataMessageWriterSettings settings = new ODataMessageWriterSettings();
            settings.ODataUri = new ODataUri 
            { 
                ServiceRoot = new Uri(service.options.ServiceRoot)
            };
            ODataMessageWriter writer = new ODataMessageWriter(message, settings);
            writer.WriteServiceDocument(service.Model.GenerateServiceDocument());
        }
    
    }
}
