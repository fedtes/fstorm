using SqlKata;
using SqlKata.Execution;

namespace FStorm
{
    public class DelegateQueryExecutor : IQueryExecutor
    {
        public async Task<IEnumerable<IDictionary<string, object?>>> Execute(Connection connection, CompilerContext context)
        {
            DelegatedSQLCompiledQuery compiledQuery = (DelegatedSQLCompiledQuery)context.Compile();
            var QueryFactory = new QueryFactory(connection.DBConnection, compiledQuery.Compiler, connection.GetCommandTimeout());
            var xquery = ConvertQuery(QueryFactory, compiledQuery.Query);
            return (await xquery.GetAsync(connection.transaction!.transaction))
                .Cast<IDictionary<string, object>>()
                .Select(x => CleanOutput(context, x)).ToList();
        }

        private Query ConvertQuery(QueryFactory factory, Query query)
        {
            var subQueries = query.Includes.ToList();
            query.Includes.Clear();
            var xquery = factory.FromQuery(query);
             foreach (var subquery in subQueries)
            {
                xquery.Include(subquery.Name, ConvertQuery(factory, subquery.Query), subquery.ForeignKey, subquery.LocalKey,subquery.IsMany);
            }
            return xquery;
        }


        /// <summary>
        /// Remove "junk" info added during the builder. These info are not used here so they are removed. 
        /// In other contexts these info are used to recover some metadata.
        /// </summary>
        private IDictionary<string, object?> CleanOutput(CompilerContext context, IDictionary<string, object> x)
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

            object? ValueSelector(KeyValuePair<string,object> y)
            {
                if (y.Value is Dictionary<string, object> d) 
                {
                    var subContext = context.GetSubContext(y.Key);
                    return CleanOutput(subContext, d);
                }
                else if (y.Value is IEnumerable<Dictionary<string, object>> i) 
                {
                    var subContext = context.GetSubContext(y.Key);
                    return i.Select(e => CleanOutput(subContext, e)).ToList();
                } 
                else 
                {
                    return EnsureType(context, y.Key, y.Value);
                }
            }
        }


        object? EnsureType(CompilerContext context, string s,object o)
        {
            if (context.GetOutputKind() == OutputKind.RawValue)
                return o;
            var s1 = s.Split("/");
            EdmPath p = context.Aliases.TryGet(s1[0])!;
            return Helpers.TypeConverter(o, (p + s1[1]).GetTypeKind());
        }
    }
}