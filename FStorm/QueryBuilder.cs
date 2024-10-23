using SqlKata;
using SqlKata.Compilers;

namespace FStorm;

public class DelegatedQueryBuilder : IQueryBuilder
{
    private readonly Query _query;
    private readonly FStormService service;

    public DelegatedQueryBuilder(FStormService service) {
        _query = new SqlKata.Query();
        this.service = service;
    }

    public DelegatedQueryBuilder(FStormService service, SqlKata.Query query) {
        this.service = service;
        this._query = query;
    }

    public IQueryBuilder AsCount(params string[] columns)
    {
        _query.AsCount(columns);
        return this;
    }

    public IQueryBuilder From(string table)
    {
        _query.From(table);
        return this;
    }
    
    public IQueryBuilder From(IQueryBuilder table, string alias)
    {
        _query.From(((DelegatedQueryBuilder)table)._query, alias);
        return this;
    }

    public IQueryBuilder Join(string table, string first, string second)
    {
        _query.Join(table, first,second);
        return this;
    }

    public IQueryBuilder LeftJoin(string table, string first, string second)
    {
        _query.LeftJoin(table,first,second);
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

    public IQueryBuilder OrderBy(params string[] columns)
    {
        _query.OrderBy(columns);
        return this;
    }

    public IQueryBuilder OrderByDesc(params string[] columns)
    {
        _query.OrderByDesc(columns);
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

    public IQueryBuilder Select(params string[] columns)
    {
        _query.Select(columns);
        return this;
    }

    public IQueryBuilder Where(string column, string op, object value)
    {
        _query.Where(column, op, value);
        return this;
    }

    public IQueryBuilder Where(Func<IQueryBuilder, IQueryBuilder> where)
    {
        _query.Where(q => ((DelegatedQueryBuilder)where(new DelegatedQueryBuilder(service, q)))._query);
        return this;
    }

    public IQueryBuilder WhereColumns(string first, string op, string second)
    {
        _query.WhereColumns(first,op, second);
        return this;
    }

    public IQueryBuilder WhereExists(IQueryBuilder query)
    {
        _query.WhereExists(((DelegatedQueryBuilder)query)._query);
        return this;
    }

    public IQueryBuilder WhereLike(string column, string value, bool caseSensitive)
    {
        _query.WhereLike(column, value,caseSensitive);
        return this;
    }

    public IQueryBuilder WhereNull(string column)
    {
        _query.WhereNull(column);
        return this;
    }

    public IQueryBuilder WhereNotNull(string column)
    {
        _query.WhereNotNull(column);
        return this;
    }

    public SQLCompiledQuery Compile(){

        Compiler compiler = service.options.SQLCompilerType switch
        {
            SQLCompilerType.MSSQL => new SqlServerCompiler(),
            SQLCompilerType.SQLLite => new SqliteCompiler(),
            _ => throw new ArgumentException("Unexpected compiler type value")
        };
        var _compilerOutput = compiler.Compile(_query);
        return new DelegatedSQLCompiledQuery(_compilerOutput.Sql, _compilerOutput.NamedBindings, compiler, _query);
    }

    IQueryBuilder IQueryBuilder.Include(string relationName, IQueryBuilder query, string foreignKey, string localKey, bool isMany)
    {
        _query.Include(relationName, ((DelegatedQueryBuilder)query)._query, foreignKey, localKey,isMany);
        return this;
    }
}


public class DelegatedSQLCompiledQuery : SQLCompiledQuery
{
    public DelegatedSQLCompiledQuery(string statement, Dictionary<string, object> bindings, Compiler compiler, Query query) : base(statement, bindings)
    {
        Compiler = compiler;
        Query = query;
    }

    public Compiler Compiler { get; }
    public Query Query { get; }
}