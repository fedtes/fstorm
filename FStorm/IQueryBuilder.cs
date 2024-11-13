namespace FStorm;

public interface IQueryBuilder
{
    IQueryBuilder From(string table, string alias);
    IQueryBuilder From(IQueryBuilder table, string alias);
    IQueryBuilder LeftJoin(string table, string alias, string alias_1, string first, string alias_2, string second);
    IQueryBuilder LeftJoin(string table, string alias, Func<IJoinCondition, IJoinCondition> joinCondition);
    IQueryBuilder LeftJoin(IQueryBuilder table, string alias, Func<IJoinCondition, IJoinCondition> joinCondition);
    IQueryBuilder LeftJoin(IQueryBuilder table, string alias, string alias_1, string first, string alias_2, string second);
    IQueryBuilder Join(string table, string alias, string alias_1, string first, string alias_2, string second);
    IQueryBuilder Select(params (string alias, string column, string? as_alias)[] columns);
    IQueryBuilder AsCount(params (string alias, string column)[] columns);
    IQueryBuilder Or();
    IQueryBuilder Not();
    IQueryBuilder Where(string alias, string column, string op, object value);
    IQueryBuilder Where(Func<IQueryBuilder,IQueryBuilder> where);
    IQueryBuilder WhereColumns(string alias_1, string first, string op, string alias_2, string second);
    IQueryBuilder WhereExists(IQueryBuilder query);
    IQueryBuilder WhereLike(string alias, string column, string value, bool caseSensitive);
    IQueryBuilder OrderBy(params (string alias, string column)[] columns);
    IQueryBuilder OrderByDesc(params (string alias, string column)[] columns);
    SQLCompiledQuery Compile();
    IQueryBuilder Limit(long top);
    IQueryBuilder Offset(long skip);
    IQueryBuilder WhereNull(string alias, string column);
    IQueryBuilder WhereNotNull(string alias, string column);
    IQueryBuilder WhereIn(string alias, string column, object?[] objects);
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