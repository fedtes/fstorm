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
        private readonly GetCompiler compiler;
        private readonly SelectPropertyCompiler selectPropertyCompiler;

        public GetCommand(
            IServiceProvider serviceProvider,
            FStormService fStormService,
            GetCompiler compiler,
            SelectPropertyCompiler selectPropertyCompiler) : base(serviceProvider, fStormService)
        {
            this.compiler = compiler;
            this.selectPropertyCompiler = selectPropertyCompiler;
        }

        internal GetConfiguration Configuration { get; set; } = null!;

        public override SQLCompiledQuery ToSQL()
        {
            var context = compiler.Compile(new CompilerContext<GetConfiguration>() { ContextData = Configuration });

            if (context.ResourceType == ResourceType.Object || context.ResourceType == ResourceType.Collection)
            {
                foreach (EdmStructuralProperty p in (context.ResourceEdmType.AsElementType() as EdmEntityType)!.StructuralProperties().Cast<EdmStructuralProperty>())
                {
                    selectPropertyCompiler
                        .Compile(context.CloneTo(new ReferenceToProperty() { property = p, path = context.ResourcePath }))
                        .CopyTo(context);
                }
            }

            return Compile(context.Query);
        }

    }

}