using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace FStorm
{
    public class GetRequest
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
        public GetRequest()
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

        internal GetRequest Configuration { get; set; } = null!;

        public override SQLCompiledQuery ToSQL()
        {
            var context = new CompilerContext<GetRequest>() { ContextData = Configuration };
            ODataUriParser parser = new ODataUriParser(fsService.Model, fsService.ServiceRoot, new Uri(Configuration.ResourcePath, UriKind.Relative));
            context.Resource.ODataPath = parser.ParsePath();
            context = compiler.Compile(context);

            

            return Compile(context);
        }


        public async Task<CommandResult<CompilerContext<GetRequest>>> ToListAsync()
        {
            if (connection == null || this.transaction == null)
            {
                throw new ArgumentNullException("Either connection or transaction are null. Cannot execute query.");
            }

            var _result = new CommandResult<CompilerContext<GetRequest>>();
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
                            var p = pathFactory.Parse(r.GetName(i));
                            row.Add(p, r.IsDBNull(i) ? null : Helpers.TypeConverter(r.GetValue(i), p.GetTypeKind()));
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