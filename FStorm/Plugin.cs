using System;

namespace FStorm;


internal class EntityAccessContext: IEntityAccessContext
{
    internal const string TABLE_STRING = "TABLE_STRING";
    internal const string NEST_QUERY = "NEST_QUERY";
    internal string kind = TABLE_STRING;

    internal EntityAccessContext(string tableString, string alias, Variable me) {
        this._initialTableString = tableString;
        this.alias = alias;
        Me = me;
    }

    string _initialTableString;
    private readonly string alias;
    string? _tableString;

    public String GetTableString() => _tableString ?? _initialTableString;

    IQueryBuilder? queryBuilder = null;

    public Variable Me { get; }

    internal IQueryBuilder GetNestedQuery() => queryBuilder ?? throw new ArgumentNullException();
}

public interface IEntityAccessContext
{
    /// <summary>
    /// Current variable in this evaulation. Rappresent the entity that are going to be acessed.
    /// </summary>
    Variable Me { get; }

    /// <summary>
    /// Get current access table only if the access direct to the table.
    /// </summary>
    /// <returns></returns>
    String GetTableString();
}


public interface IOnEntityAccess
{
    void OnAccess(IEntityAccessContext accessContext);

}
