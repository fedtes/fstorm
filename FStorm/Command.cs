using Microsoft.OData.UriParser;
using SqlKata.Compilers;
using SqlKata;
using Microsoft.OData.Edm;

namespace FStorm
{
    public class Command
    {
        public class SQLCompiledQuery
        {
            public string Statement { get; }
            public Dictionary<string, object> Bindings { get; }

            public SQLCompiledQuery(string statement, Dictionary<string, object> bindings)
            {
                Statement = statement;
                Bindings = bindings;
            }
        }

        public readonly string CommandId;
        protected readonly IServiceProvider serviceProvider;
        protected readonly FStormService fStormService;

        public Command(IServiceProvider serviceProvider, FStormService fStormService) 
        {
            CommandId = Guid.NewGuid().ToString();
            this.serviceProvider = serviceProvider;
            this.fStormService = fStormService;
        }

        public virtual SQLCompiledQuery ToSQL()
        {
            throw new NotImplementedException();
        }

        protected virtual SQLCompiledQuery Compile(Query query) 
        {
            var compiler = fStormService.options.SQLCompilerType switch
            {
                SQLCompilerType.MSSQL => new SqlServerCompiler(),
                _ => throw new ArgumentException("Unexpected compiler type value")
            };

            var _compilerOutput = compiler.Compile(query);
            return new SQLCompiledQuery(_compilerOutput.Sql, _compilerOutput.NamedBindings);
        }
    }


    public class GetConfiguration
    {
        /// <summary>
        /// Path to address a collection (of entities), a single entity within a collection, a singleton, as well as a property of an entity.
        /// </summary>
        public string ResourcePath { get; set; }
        public string? Filter { get; set; }
        public string? Select { get; set; }
        public string? Count { get; set; }
        public string? OrderBy { get; set; }
        public string? Top { get; set; }
        public string? Skip { get; set; }
        public GetConfiguration()
        {
            ResourcePath = String.Empty;
        }
    }

    public class GetCommand : Command
    {
        public GetCommand(IServiceProvider serviceProvider, FStormService fStormService) : base(serviceProvider, fStormService)
        { }

        internal GetConfiguration Configuration { get; set; } = null!;

        private enum ResultType
        {
            Collection,
            Object,
            Property
        }

        public override SQLCompiledQuery ToSQL()
        {
            ODataUriParser parser = new ODataUriParser(fStormService.Model, fStormService.ServiceRoot, new Uri(fStormService.ServiceRoot, Configuration.ResourcePath));
            ODataPath path = parser.ParsePath();

            Query query = new Query();

            int index = 0;
            string table_alias = "#";
            ResultType result = ResultType.Collection;

            foreach (var segment in path)
            {
                switch (segment)
                {
                    case EntitySetSegment collection:
                        if (index == 0)
                        {
                            var _type = (collection.EdmType.AsElementType() as EdmEntityType)!;
                            table_alias += "/" + _type.Name;
                            query.From(_type.Table + $" as {table_alias}");
                        }
                        else
                        {
                            throw new NotImplementedException("Should not pass here");
                        }
                        result = ResultType.Collection;
                        break;
                    case KeySegment single:

                        var k = single.Keys.First();
                        EdmStructuralProperty keyProperty = (EdmStructuralProperty)((EdmEntityType)single.EdmType).DeclaredKey.First(x => x.Name == k.Key);
                        query.Where($"{table_alias}.{keyProperty.columnName}", k.Value);
                        result = ResultType.Object;

                        break;
                    case PropertySegment property:

                        var _prop = (property.Property as EdmStructuralProperty)!;
                        query.Select($"{table_alias}." + _prop.columnName + $" as {table_alias}/{_prop.Name}");
                        result = ResultType.Property;

                        break;
                    default:
                        throw new NotImplementedException("Should not pass here");
                }

                index++;
            }

            if (result == ResultType.Object || result == ResultType.Collection)
            {
                foreach (EdmStructuralProperty p in (path.LastSegment.EdmType.AsElementType() as EdmEntityType)!.StructuralProperties())
                {
                    query.Select($"{table_alias}." + p.columnName + $" as {table_alias}/{p.Name}");
                }
            }

            return Compile(query);
        }

    }

}