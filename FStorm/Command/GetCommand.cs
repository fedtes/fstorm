using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace FStorm
{
    public class GetRequest
    {
        /// <summary>
        /// Path to address a collection (of entities), a single entity within a collection, a singleton, as well as a property of an entity.
        /// </summary>
        public string RequestPath { get; set; }
        public GetRequest()
        {
            RequestPath = String.Empty;
        }
    }

    public class GetRequestCommand : Command
    {
        public GetRequestCommand(IServiceProvider serviceProvider, FStormService fStormService, SemanticVisitor visitor, EdmPathFactory pathFactory) : base(serviceProvider, fStormService, visitor, pathFactory){ }

        public override async Task<CommandResult<CompilerContext>> ToListAsync()
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

                DataTable dt = new DataTable(compileResult.Context.GetOutputPath());

                if (compileResult.Context.GetOutputKind() != OutputKind.RawValue) 
                {
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        int rowIdx = 0;
                        while (await r.ReadAsync())
                        {
                            // Add Columns to data table
                            if (0==rowIdx) {
                                for (int i = 0; i < r.FieldCount; i++) {
                                    dt.AddColumn(pathFactory.ParseString(r.GetName(i)));
                                }
                            }
                            // Fill row values
                            var row = dt.CreateRow();
                            for (int i = 0; i < r.FieldCount; i++)
                            {
                                var p = pathFactory.ParseString(r.GetName(i));
                                row[p] = r.IsDBNull(i) ? null : Helpers.TypeConverter(r.GetValue(i), p.GetTypeKind());
                            }
                        }
                    }
                }
                else 
                {
                    var valuePath = pathFactory.ParseString(EdmPath.PATH_ROOT + "/value");
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