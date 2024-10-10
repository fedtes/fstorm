using System;

namespace FStorm;

public interface IQueryBuilder
{
    IQueryBuilder From(string table);
    IQueryBuilder From(IQueryBuilder table, string alias);
    IQueryBuilder LeftJoin(string table, string first, string second);
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
    (string statement, Dictionary<string, object> bindings) Compile(SQLCompilerType compilerType);
}
