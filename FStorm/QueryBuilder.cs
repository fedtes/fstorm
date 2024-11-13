using SqlKata;
using SqlKata.Compilers;

namespace FStorm;

public class SQLKataQueryBuilder : IQueryBuilder
{
    private readonly Query _query;
    private readonly ODataService service;

    public SQLKataQueryBuilder(ODataService service) {
        _query = new SqlKata.Query();
        this.service = service;
    }

    public SQLKataQueryBuilder(ODataService service, SqlKata.Query query) {
        this.service = service;
        this._query = query;
    }

    public IQueryBuilder AsCount(params (string alias, string column)[] columns)
    {
        _query.AsCount(columns.Select(x => $"{x.alias}.{x.column}").ToArray());
        return this;
    }

    public IQueryBuilder From(string table, string alias)
    {
        _query.From($"{table} as {alias}");
        return this;
    }
    
    public IQueryBuilder From(IQueryBuilder table, string alias)
    {
        _query.From(((SQLKataQueryBuilder)table)._query, alias);
        return this;
    }

    public IQueryBuilder Join(string table, string alias, string alias_1, string first, string alias_2, string second)
    {
        _query.Join($"{table} as {alias}", $"{alias_1}.{first}", $"{alias_2}.{second}");
        return this;
    }

    public IQueryBuilder LeftJoin(string table, string alias, string alias_1, string first, string alias_2, string second)
    {
        _query.LeftJoin($"{table} as {alias}", $"{alias_1}.{first}", $"{alias_2}.{second}");
        return this;
    }

    public IQueryBuilder LeftJoin(IQueryBuilder table, string alias, Func<IJoinCondition, IJoinCondition> joinCondition)
    {
        _query.LeftJoin(((SQLKataQueryBuilder)table)._query.As(alias), (j) => ((SQLKataJoinCondition)joinCondition(new SQLKataJoinCondition(j))).Join);
        return this;
    }

    public IQueryBuilder LeftJoin(string table, string alias, Func<IJoinCondition, IJoinCondition> joinCondition)
    {
        _query.LeftJoin($"{table} as {alias}", (j) => ((SQLKataJoinCondition)joinCondition(new SQLKataJoinCondition(j))).Join);
        return this;
    }

    public IQueryBuilder LeftJoin(IQueryBuilder table, string alias, string alias_1, string first, string alias_2, string second)
    {
        _query.LeftJoin(((SQLKataQueryBuilder)table)._query.As(alias), j => j.WhereColumns( $"{alias_1}.{first}", "=", $"{alias_2}.{second}"));
        return this;
    }

    public IQueryBuilder Not()
    {
        _query.Not();
        return this;
    }

    public IQueryBuilder Or()
    {
        _query.Or();
        return this;
    }

    public IQueryBuilder OrderBy(params (string alias, string column)[] columns)
    {
        _query.OrderBy(columns.Select(x => $"{x.alias}.{x.column}").ToArray());
        return this;
    }

    public IQueryBuilder OrderByDesc(params (string alias, string column)[] columns)
    {
        _query.OrderByDesc(columns.Select(x => $"{x.alias}.{x.column}").ToArray());
        return this;
    }

    IQueryBuilder IQueryBuilder.Limit(long top)
    {
        _query.Limit(Convert.ToInt32(top));
        return this;
    }

    IQueryBuilder IQueryBuilder.Offset(long skip)
    {
        _query.Offset(skip);
        return this;
    }

    public IQueryBuilder Select(params (string alias, string column, string? as_alias)[] columns)
    {
        _query.Select(columns.Select(x => $"{x.alias}.{x.column}{(string.IsNullOrEmpty(x.as_alias)? "": " as " + x.as_alias)}").ToArray());
        return this;
    }

    public IQueryBuilder Where(string alias, string column, string op, object value)
    {
        _query.Where($"{alias}.{column}", op, value);
        return this;
    }

    public IQueryBuilder Where(Func<IQueryBuilder, IQueryBuilder> where)
    {
        _query.Where(q => ((SQLKataQueryBuilder)where(new SQLKataQueryBuilder(service, q)))._query);
        return this;
    }

    public IQueryBuilder WhereColumns(string alias_1, string first, string op, string alias_2, string second)
    {
        _query.WhereColumns($"{alias_1}.{first}", op, $"{alias_2}.{second}");
        return this;
    }

    public IQueryBuilder WhereExists(IQueryBuilder query)
    {
        _query.WhereExists(((SQLKataQueryBuilder)query)._query);
        return this;
    }

    public IQueryBuilder WhereLike(string alias, string column, string value, bool caseSensitive)
    {
        _query.WhereLike($"{alias}.{column}", value, caseSensitive);
        return this;
    }

    public IQueryBuilder WhereNull(string alias, string column)
    {
        _query.WhereNull($"{alias}.{column}");
        return this;
    }

    public IQueryBuilder WhereNotNull(string alias, string column)
    {
        _query.WhereNotNull($"{alias}.{column}");
        return this;
    }

    public SQLCompiledQuery Compile()
    {
        Compiler compiler = service.options.SQLCompilerType switch
        {
            SQLCompilerType.MSSQL => new SqlServerCompiler(),
            SQLCompilerType.SQLLite => new SqliteCompiler(),
            _ => throw new ArgumentException("Unexpected compiler type value")
        };
        var _compilerOutput = compiler.Compile(_query);
        return new SQLCompiledQuery(_compilerOutput.Sql, _compilerOutput.NamedBindings);
    }

    IQueryBuilder IQueryBuilder.WhereIn(string alias, string column, object?[] objects)
    {
        _query.WhereIn($"{alias}.{column}", objects);
        return this;
    }


}


public class SQLKataJoinCondition : IJoinCondition
{
    public SqlKata.Join Join;

    public SQLKataJoinCondition(SqlKata.Join join) {
        this.Join = join;
    }
    public IJoinCondition On(string first, string second, string op)
    {
        Join.On(first, second, op);
        return this;
    }

    public IJoinCondition OrOn(string first, string second, string op)
    {
        Join.OrOn(first, second, op);
        return this;
    }
}
