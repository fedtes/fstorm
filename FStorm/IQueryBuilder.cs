namespace FStorm;

public interface IQueryBuilder
{
    IQueryBuilder From(string table);
    IQueryBuilder From(IQueryBuilder table, string alias);
    IQueryBuilder LeftJoin(string table, string first, string second);
    IQueryBuilder LeftJoin(string table, Func<IJoinCondition,IJoinCondition> joinCondition);
    IQueryBuilder LeftJoin(IQueryBuilder table, Func<IJoinCondition,IJoinCondition> joinCondition);
    IQueryBuilder LeftJoin(IQueryBuilder table, string first, string second);
    IQueryBuilder Join(string table, string first, string second);
    IQueryBuilder Select(params string[] columns);
    IQueryBuilder AsCount(params string[] columns);
    IQueryBuilder Or();
    IQueryBuilder Not();
    IQueryBuilder Where(string column, string op, object value);
    IQueryBuilder Where(Func<IQueryBuilder,IQueryBuilder> where);
    IQueryBuilder WhereColumns(string first, string op, string second);
    IQueryBuilder WhereExists(IQueryBuilder query);
    IQueryBuilder WhereLike(string column, string value, bool caseSensitive);
    IQueryBuilder OrderBy(params string[] columns);
    IQueryBuilder OrderByDesc(params string[] columns);
    SQLCompiledQuery Compile();
    IQueryBuilder Limit(long top);
    IQueryBuilder Offset(long skip);
    IQueryBuilder WhereNull(string v);
    IQueryBuilder WhereNotNull(string v);
    IQueryBuilder WhereIn(string v, object?[] objects);
}

public interface IJoinCondition
{
    IJoinCondition On(string first, string second, string op);
    IJoinCondition OrOn(string first, string second, string op);
}



public class SQLCompiledQuery
{
    public string Statement { get; }
    public Dictionary<string, object> Bindings { get; }
    public SQLCompiledQuery(string statement, Dictionary<string, object> bindings)
    {
        Statement = statement;
        Bindings = bindings;
    }
}