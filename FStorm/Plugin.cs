using System;
using Microsoft.OData.UriParser;

namespace FStorm;


internal class EntityAccessContext: IEntityAccessContext
{
   
    private readonly CompilerContextFactory contextFactory;

    private readonly QueryBuilderContext queryBuilderCtx;
    public string Kind {get; set;} = IEntityAccessContext.TABLE_STRING;

    public EntityAccessContext(CompilerContextFactory contextFactory)
    {
        this.contextFactory = contextFactory;
        this.queryBuilderCtx = (QueryBuilderContext)contextFactory.CreateNoPluginQueryBuilderContext();
    }

    internal string InitialTableString;
    public string Alias { get; internal set; } 
    internal string? _tableString;
    
    internal IQueryBuilder GetNestedQuery() => Kind == IEntityAccessContext.NEST_QUERY ? queryBuilderCtx.ActiveScope.Query ?? throw new ArgumentNullException("IQueryBuilder") : throw new ArgumentException("Invalid Kind. Expected 'NEST_QUERY'");

    public String GetTableString() => Kind == IEntityAccessContext.TABLE_STRING ? _tableString ?? InitialTableString : throw new ArgumentException("Invalid Kind. Expected 'TABLE_STRING'");

    public bool HasFrom() => queryBuilderCtx.HasFrom();

    public IQueryBuilder GetQuery() => queryBuilderCtx.GetQuery();

    public List<Variable> GetVariablesInScope()  => queryBuilderCtx.GetVariablesInScope();

    public Variable? GetCurrentVariableInScope()  => queryBuilderCtx.GetCurrentVariableInScope();

    public void Push(CompilerScope scope)  => queryBuilderCtx.Push(scope);

    public CompilerScope Pop() => queryBuilderCtx.Pop();

    public void OpenAndScope()  => queryBuilderCtx.OpenAndScope();

    public void CloseAndScope()  => queryBuilderCtx.CloseAndScope();

    public void OpenOrScope()  => queryBuilderCtx.OpenOrScope();

    public void CloseOrScope()  => queryBuilderCtx.CloseOrScope();

    public void OpenNotScope() => queryBuilderCtx.OpenNotScope();

    public void CloseNotScope()  => queryBuilderCtx.CloseNotScope();

    public void OpenVariableScope(Variable variable)  => queryBuilderCtx.OpenVariableScope(variable);

    public void CloseVariableScope()  => queryBuilderCtx.CloseVariableScope();

    public void OpenAnyScope()  => queryBuilderCtx.OpenAnyScope();

    public void CloseAnyScope()  => queryBuilderCtx.CloseAnyScope();

    public void OpenAllScope()  => queryBuilderCtx.OpenAllScope();

    public void CloseAllScope()  => queryBuilderCtx.CloseAllScope();

    public EdmPath AddFrom(EdmEntityType edmEntityType, EdmPath edmPath)  => queryBuilderCtx.AddFrom(edmEntityType, edmPath);

    public EdmPath AddJoin(EdmNavigationProperty rightNavigationProperty, EdmPath rightPath, EdmPath leftPath)  => queryBuilderCtx.AddJoin(rightNavigationProperty,rightPath, leftPath);

    public void AddSelect(EdmPath edmPath, EdmStructuralProperty property, string? customName = null)   => queryBuilderCtx.AddSelect(edmPath, property, customName);

    public void AddSelectKey(EdmPath? path, EdmEntityType? type)   => queryBuilderCtx.AddSelectKey(path, type);

    public void AddSelectAll(EdmPath? path, EdmEntityType? type)   => queryBuilderCtx.AddSelectAll(path, type);

    public void AddCount(EdmPath edmPath, EdmStructuralProperty edmStructuralProperty)  => queryBuilderCtx.AddCount(edmPath, edmStructuralProperty);

    public void AddFilter(BinaryFilter filter)   => queryBuilderCtx.AddFilter(filter);

    public void AddOrderBy(EdmPath edmPath, EdmStructuralProperty property, OrderByDirection direction)   => queryBuilderCtx.AddOrderBy(edmPath, property, direction);

    public void WrapQuery(IOdataParserContext context, EdmPath resourcePath)   => queryBuilderCtx.WrapQuery(context, resourcePath);

    public void AddLimit(long top)  => queryBuilderCtx.AddLimit(top);

    public void AddOffset(long skip) => queryBuilderCtx.AddOffset(skip);

    public Variable Me { get; internal set;}

    public AliasStore Aliases => throw new NotImplementedException();

    public CompilerScope ActiveScope => throw new NotImplementedException();
}

public interface IEntityAccessContext : IQueryBuilderContext
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

    
    string Kind {get; set;}

    public const string TABLE_STRING = "TABLE_STRING";
    public const string NEST_QUERY = "NEST_QUERY";
}


public interface IOnEntityAccess
{
    /// <summary>
    /// fully namespaced name of the entity where this plugin should execute on.
    /// </summary>
    string EntityName { get; }

    void OnAccess(IEntityAccessContext accessContext);

}
