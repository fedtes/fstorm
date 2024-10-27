using Microsoft.Extensions.DependencyInjection;
using SqlKata;
using SqlKata.Execution;

namespace FStorm
{
    // public class DelegateQueryExecutor : IQueryExecutor
    // {
    //     public async Task<IEnumerable<IDictionary<string, object?>>> Execute(Connection connection,Transaction transaction, CompilerContext context)
    //     {
    //         DelegatedSQLCompiledQuery compiledQuery = (DelegatedSQLCompiledQuery)context.Compile();
    //         var QueryFactory = new QueryFactory(connection.DBConnection, compiledQuery.Compiler, connection.GetCommandTimeout());
    //         var xquery = ConvertQuery(QueryFactory, compiledQuery.Query);
    //         return (await xquery.GetAsync(connection.transaction!.transaction))
    //             .Cast<IDictionary<string, object>>()
    //             .Select(x => CleanOutput(context, x)).ToList();
    //     }

    //     private Query ConvertQuery(QueryFactory factory, Query query)
    //     {
    //         var subQueries = query.Includes.ToList();
    //         query.Includes.Clear();
    //         var xquery = factory.FromQuery(query);
    //          foreach (var subquery in subQueries)
    //         {
    //             xquery.Include(subquery.Name, ConvertQuery(factory, subquery.Query), subquery.ForeignKey, subquery.LocalKey,subquery.IsMany);
    //         }
    //         return xquery;
    //     }


    //     /// <summary>
    //     /// Remove "junk" info added during the builder. These info are not used here so they are removed. 
    //     /// In other contexts these info are used to recover some metadata.
    //     /// </summary>
    //     private IDictionary<string, object?> CleanOutput(CompilerContext context, IDictionary<string, object> x)
    //     {
    //         if (x.Keys.Any(y => y.EndsWith("/:key")))
    //         {
    //             x.Remove(x.Keys.First(y => y.EndsWith("/:key")));
    //         }

    //         if (x.Keys.Any(y => y.EndsWith("/:fkey")))
    //         {
    //             var y = x.Keys.Where(y => y.EndsWith("/:fkey")).ToList();
    //             y.ForEach(z => x.Remove(z));
    //         }

    //         return x.ToDictionary(y => y.Key.Substring(y.Key.LastIndexOf("/") + 1), ValueSelector);

    //         object? ValueSelector(KeyValuePair<string,object> y)
    //         {
    //             if (y.Value is Dictionary<string, object> d) 
    //             {
    //                 var subContext = context.GetSubContext(y.Key);
    //                 return CleanOutput(subContext, d);
    //             }
    //             else if (y.Value is IEnumerable<Dictionary<string, object>> i) 
    //             {
    //                 var subContext = context.GetSubContext(y.Key);
    //                 return i.Select(e => CleanOutput(subContext, e)).ToList();
    //             } 
    //             else 
    //             {
    //                 return EnsureType(context, y.Key, y.Value);
    //             }
    //         }
    //     }


    //     object? EnsureType(CompilerContext context, string s,object o)
    //     {
    //         if (context.GetOutputKind() == OutputKind.RawValue)
    //             return o;
    //         var s1 = s.Split("/");
    //         EdmPath p = context.Aliases.TryGet(s1[0])!;
    //         return Helpers.TypeConverter(o, (p + s1[1]).GetTypeKind());
    //     }
    // }

    public class DBCommandQueryExecutor : IQueryExecutor
    {
        private readonly FStormService service;

        public DBCommandQueryExecutor(FStormService service)
        {
            this.service = service;
        }

        public async Task<IEnumerable<IDictionary<string, object?>>> Execute(Connection connection, Transaction transaction, ICompilerContext context)
        {
            var compiledQuery = context.GetQuery().Compile();
            return (await InnerExecute(connection, transaction, context, compiledQuery)).Select(x=> CleanOutput(context, x)).ToList();
        }

        public async Task<Rows> InnerExecute(Connection connection, Transaction transaction, ICompilerContext context, SQLCompiledQuery compiledQuery)
        {
            var result = await LocalExecute(connection, transaction, context, compiledQuery);
            if (!result.Any()) return result;
            if (!context.HasSubContext()) return result;

            foreach (var sc in context.GetSubContextes())
            {
                var q = service.serviceProvider.GetService<IQueryBuilder>()!;
                
                var foreignKey = sc.Key+"/:fkey";
                SQLCompiledQuery cq = q.From(sc.Value.GetQuery(), "E")
                    .WhereIn(foreignKey, result.Select(x => x[foreignKey]).ToArray())
                    .Compile();

                var r = await InnerExecute(connection, transaction, sc.Value, cq);
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

        public async Task<Rows> LocalExecute(Connection connection, Transaction transaction, ICompilerContext context, SQLCompiledQuery compiledQuery)
        {
            System.Data.Common.DbCommand command = connection.DBConnection.CreateCommand();
            command.Transaction = transaction.transaction;
            command.CommandText = compiledQuery.Statement;

            foreach (var binding in compiledQuery.Bindings)
            {
                var p = command.CreateParameter();
                p.Direction = System.Data.ParameterDirection.Input;
                p.ParameterName = binding.Key;
                p.Value = binding.Value;
                command.Parameters.Add(p);
            }

            try
            {
                Rows result = new Rows();
                using (var reader = await command.ExecuteReaderAsync())
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