namespace FStorm;

public interface IQueryExecutor
{
    Task<IEnumerable<IDictionary<string, object>>> Execute(Connection connection, CompilerContext context);
}
