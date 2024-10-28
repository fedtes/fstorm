namespace FStorm;

public interface IQueryExecutor
{
    Task<IEnumerable<IDictionary<string, object?>>> Execute(Command command);
}

