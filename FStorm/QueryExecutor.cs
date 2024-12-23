using Microsoft.Extensions.DependencyInjection;
using SqlKata;
using SqlKata.Execution;

namespace FStorm
{
    public class DBCommandQueryExecutor : IQueryExecutor
    {
        private readonly ODataService service;

        public DBCommandQueryExecutor(ODataService service)
        {
            this.service = service;
        }

        public async Task<IEnumerable<IDictionary<string, object?>>> Execute(Command command)
        {
            var compiledQuery = command.context.GetQuery().Compile();
            return (await InnerExecute(command,command.context, compiledQuery)).Select(x=> CleanOutput(command.context, x)).ToList();
        }

        public async Task<Rows> InnerExecute(Command command,ICompilerContext context, SQLCompiledQuery compiledQuery)
        {
            var result = await LocalExecute(command, compiledQuery);
            if (!result.Any()) return result;
            if (!context.HasSubContext()) return result;

            foreach (var sc in context.GetSubContextes())
            {
                var q = service.serviceProvider.GetService<IQueryBuilder>()!;
                
                var foreignKey = sc.Key+"/:fkey";
                SQLCompiledQuery cq = q.From(sc.Value.GetQuery(), "E")
                    .WhereIn(foreignKey, result.Select(x => x[foreignKey]).ToArray())
                    .Compile();

                var r = await InnerExecute(command, sc.Value, cq);
                result = result
                    .GroupJoin(r, y => y[foreignKey], x => x[foreignKey], (y,x) => ResultSelector(sc, y, x)).ToRows();
            }

            return result;
        }

        private Dictionary<string, object?> ResultSelector(KeyValuePair<string,ICompilerContext> sc, Dictionary<string, object?> y, IEnumerable<Dictionary<string, object?>> x) 
        {
            var expContext = (ExpansionCompilerContext)sc.Value;
            if (sc.Value.GetOutputKind() == OutputKind.Collection)
            {
                y.Add(sc.Key, x.Skip(expContext.Skip).Take(expContext.Top).ToList());
            }
            else 
            {
                y.Add(sc.Key, x.FirstOrDefault());
            }
            return y;
        }

        public async Task<Rows> LocalExecute(Command command, SQLCompiledQuery compiledQuery)
        {
            System.Data.Common.DbCommand dbcommand = command.connection.DBConnection.CreateCommand();
            dbcommand.Transaction = command.transaction.transaction;
            dbcommand.CommandText = compiledQuery.Statement;
            dbcommand.CommandTimeout = Convert.ToInt32(command.CommandTimeout);

            foreach (var binding in compiledQuery.Bindings)
            {
                var p = dbcommand.CreateParameter();
                p.Direction = System.Data.ParameterDirection.Input;
                p.ParameterName = binding.Key;
                p.Value = binding.Value;
                dbcommand.Parameters.Add(p);
            }

            try
            {
                Rows result = new Rows();
                using (var reader = await dbcommand.ExecuteReaderAsync())
                {
                    int i = 0;
                    List<string> columns = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        if (i==0) 
                        {
                            for (int j = 0; j < reader.FieldCount; j++)
                            {
                                columns.Add(reader.GetName(j));
                            }
                        }
                        
                        Dictionary<string,object?> row = new Dictionary<string, object?>();

                        for (int j = 0; j < reader.FieldCount; j++)
                        {
                            string key = columns[j];
                            object? value = reader.IsDBNull(j) ? null : reader.GetValue(j);
                            row.Add(key, value);
                        }
                        
                        result.Add(row);
                        i++;
                    }
                }
                return result;
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        object? EnsureType(ICompilerContext context, string s, object o)
        {
            if (context.GetOutputKind() == OutputKind.RawValue)
                return o;
            var s1 = s.Split("/");
            EdmPath p = context.Aliases.TryGet(s1[0])!;
            return Helpers.TypeConverter(o, (p + s1[1]).GetTypeKind());
        }


        /// <summary>
        /// Remove "junk" info added during the builder. These info are not used here so they are removed. 
        /// In other contexts these info are used to recover some metadata.
        /// </summary>
        private IDictionary<string, object?> CleanOutput(ICompilerContext context, IDictionary<string, object?> x)
        {
            if (x.Keys.Any(y => y.EndsWith("/:key")))
            {
                x.Remove(x.Keys.First(y => y.EndsWith("/:key")));
            }

            if (x.Keys.Any(y => y.EndsWith("/:fkey")))
            {
                var y = x.Keys.Where(y => y.EndsWith("/:fkey")).ToList();
                y.ForEach(z => x.Remove(z));
            }

            return x.ToDictionary(y => y.Key.Substring(y.Key.LastIndexOf("/") + 1), ValueSelector);

            object? ValueSelector(KeyValuePair<string,object?> y)
            {
                if (y.Value is Dictionary<string, object?> d) 
                {
                    var subContext = context.GetSubContext(y.Key);
                    return CleanOutput(subContext, d);
                }
                else if (y.Value is IEnumerable<Dictionary<string, object?>> i) 
                {
                    var subContext = context.GetSubContext(y.Key);
                    return i.Select(e => CleanOutput(subContext, e)).ToList();
                } 
                else 
                {
                    //return y.Value;
                    return EnsureType(context, y.Key, y.Value);
                }
            }
        }
    }


    public class Rows : List<Dictionary<string, object?>>
    {

    }
}