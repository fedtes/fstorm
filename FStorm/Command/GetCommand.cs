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

    public class GetRequestCommand : Command
    {
        private readonly Compiler compiler;
        private readonly SemanticVisitor visitor;
        private readonly EdmPathFactory pathFactory;

        public GetRequestCommand(
            IServiceProvider serviceProvider,
            FStormService fStormService,
            Compiler compiler,
            SemanticVisitor visitor,
            EdmPathFactory pathFactory) : base(serviceProvider, fStormService)
        {
            this.compiler = compiler;
            this.visitor = visitor;
            this.pathFactory = pathFactory;
        }

        internal GetRequest Configuration { get; set; } = null!;

        public override SQLCompiledQuery ToSQL()
        {
            var context = new CompilerContext();
            ODataUriParser parser = new ODataUriParser(fsService.Model, fsService.ServiceRoot, new Uri(Configuration.ResourcePath, UriKind.Relative));
            context.Output.ODataPath = parser.ParsePath();
            visitor.VisitPath(context, context.Output.ODataPath);

            /* Write output */
            if (context.Output.OutputType == OutputType.Collection || context.Output.OutputType == OutputType.Object)
            {
                foreach (var property in context.Output.ResourceEdmType.DeclaredStructuralProperties())
                {
                    if (property.IsKey()) context.AddSelect(context.Output.ResourcePath, (EdmStructuralProperty)property, ":key");
                    context.AddSelect(context.Output.ResourcePath, (EdmStructuralProperty)property);

                }
            }


            //context = compiler.AddGet(context, Configuration);
            return Compile(context);
        }


        public async Task<CommandResult<CompilerContext>> ToListAsync()
        {
            if (connection == null || this.transaction == null)
            {
                throw new ArgumentNullException("Either connection or transaction are null. Cannot execute query.");
            }

            var _result = new CommandResult<CompilerContext>();
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

                DataTable dt = new DataTable(compileResult.Context.Output.ResourcePath);

                if (compileResult.Context.Output.OutputType != OutputType.RawValue) 
                {
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        int rowIdx = 0;
                        while (await r.ReadAsync())
                        {
                            // Add Columns to data table
                            if (0==rowIdx) {
                                for (int i = 0; i < r.FieldCount; i++) {
                                    dt.AddColumn(pathFactory.Parse(r.GetName(i)));
                                }
                            }
                            // Fill row values
                            var row = dt.CreateRow();
                            for (int i = 0; i < r.FieldCount; i++)
                            {
                                var p = pathFactory.Parse(r.GetName(i));
                                row[p] = r.IsDBNull(i) ? null : Helpers.TypeConverter(r.GetValue(i), p.GetTypeKind());
                            }
                        }
                    }
                }
                else 
                {
                    var valuePath = pathFactory.Parse(EdmPath.PATH_ROOT + "/value");
                    dt.AddColumn(valuePath);
                    var row = dt.CreateRow();
                    row[valuePath] = await cmd.ExecuteScalarAsync();
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