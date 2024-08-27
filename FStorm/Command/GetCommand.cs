using Microsoft.OData.Edm;

namespace FStorm
{
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
        private readonly EdmPathFactory pathFactory;

        public GetCommand(
            IServiceProvider serviceProvider,
            FStormService fStormService,
            GetCompiler compiler,
            SelectPropertyCompiler selectPropertyCompiler,
            EdmPathFactory pathFactory) : base(serviceProvider, fStormService)
        {
            this.compiler = compiler;
            this.selectPropertyCompiler = selectPropertyCompiler;
            this.pathFactory = pathFactory;
        }

        internal GetConfiguration Configuration { get; set; } = null!;

        public override SQLCompiledQuery ToSQL()
        {
            var context = compiler.Compile(new CompilerContext<GetConfiguration>() { ContextData = Configuration });

            if (context.Resource.ResourceType == ResourceType.Object || context.Resource.ResourceType == ResourceType.Collection)
            {
                foreach (EdmStructuralProperty p in (context.Resource.ResourceEdmType.AsElementType() as EdmEntityType)!.StructuralProperties().Cast<EdmStructuralProperty>())
                {
                    selectPropertyCompiler
                        .Compile(context.CloneTo(new ReferenceToProperty() { property = p, path = context.Resource.ResourcePath }))
                        .CopyTo(context);
                }
            }

            return Compile(context);
        }


        public async Task<CommandResult<CompilerContext<GetConfiguration>>> ToListAsync()
        {
            if (connection == null || this.transaction == null)
            {
                throw new ArgumentNullException("Either connection or transaction are null. Cannot execute query.");
            }

            var _result = new CommandResult<CompilerContext<GetConfiguration>>();
            var con = base.connection.connection;

            try
            {
                var cmd = con.CreateCommand();
                cmd.Transaction = transaction.transaction;
                var compileResult = ToSQL();
                cmd.CommandText = compileResult.Statement;

                foreach (var b in compileResult.Bindings)
                {
                    var p = cmd.CreateParameter();
                    p.ParameterName=b.Key;
                    p.Value = b.Value;
                    cmd.Parameters.Add(p);
                }

                DataTable dt = new DataTable();

                using (var r = await cmd.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                    {
                        Row row = new Row();
                        for (int i = 0; i < r.FieldCount; i++)
                        {
                            row.Add(pathFactory.Parse(r.GetName(i)), r.IsDBNull(i) ? null : r.GetValue(i));
                        }
                        dt.Add(row);
                    }
                }

                _result.Value = dt;
                _result.Context = compileResult.Context;
                transaction.Commit();
                return _result;
            }
            catch (Exception)
            {
                if (transaction != null)
                    transaction.Rollback();
                throw;
            }
        }

    }

}