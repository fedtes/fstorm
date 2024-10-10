using System;
using SqlKata;
using SqlKata.Compilers;

namespace FStorm;

public class DelegatedQueryBuilder : IQueryBuilder
{
    private readonly Query _query;

    public DelegatedQueryBuilder() {
        _query = new SqlKata.Query();
    }

    public DelegatedQueryBuilder(SqlKata.Query query) {
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
        _query.Where(q => ((DelegatedQueryBuilder)where(new DelegatedQueryBuilder(q)))._query);
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

    public (string statement, Dictionary<string, object> bindings) Compile(SQLCompilerType compilerType){

        Compiler compiler = compilerType switch
        {
            SQLCompilerType.MSSQL => new SqlServerCompiler(),
            SQLCompilerType.SQLLite => new SqliteCompiler(),
            _ => throw new ArgumentException("Unexpected compiler type value")
        };
        var _compilerOutput = compiler.Compile(_query);
        return (_compilerOutput.Sql, _compilerOutput.NamedBindings);
    }


}
