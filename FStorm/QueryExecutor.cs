using SqlKata.Execution;

namespace FStorm
{
    public class DelegateQueryExecutor : IQueryExecutor
    {
        public async Task<IEnumerable<IDictionary<string, object>>> Execute(Connection connection, CompilerContext context)
        {
            DelegatedSQLCompiledQuery compiledQuery = (DelegatedSQLCompiledQuery)context.Compile();
            /*
            Remove "junk" info added during the builder. These info are not used here so they are removed. In other contexts these info are used to recover some metadata.
            */
            static IDictionary<string, object> CleanOutput(IDictionary<string, object> x)
            {
                x.Remove(x.Keys.First(y => y.EndsWith("/:key")));
                return x.ToDictionary(y => y.Key.Substring(y.Key.LastIndexOf("/") + 1), y => y.Value);
            }

            var xquery = new QueryFactory(connection.DBConnection, compiledQuery.Compiler, connection.GetCommandTimeout()).FromQuery(compiledQuery.Query);
            return (await xquery.GetAsync(connection.transaction!.transaction))
                .Cast<IDictionary<string, object>>()
                .Select(CleanOutput).ToList();

        }
    }
}