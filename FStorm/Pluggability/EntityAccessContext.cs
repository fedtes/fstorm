using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace FStorm;

internal class EntityAccessContext: IQueryBuilderContext, IEntityAccessContext
{
    public string Kind {get; set;} = IEntityAccessContext.TABLE_STRING;
    public Variable Me { get; internal set;} = null!;
    internal IQueryBuilder GetNestedQuery() => Kind == IEntityAccessContext.NEST_QUERY ? queryBuilderCtx.ActiveScope.Query ?? throw new ArgumentNullException("IQueryBuilder") : throw new ArgumentException("Invalid Kind. Expected 'NEST_QUERY'");
    public String GetTableString() => Kind == IEntityAccessContext.TABLE_STRING ? _tableString ?? InitialTableString : throw new ArgumentException("Invalid Kind. Expected 'TABLE_STRING'");
    public void SetTableString(string table)
    {
        this._tableString = table;
    }

    internal string InitialTableString = null!;
    public string Alias { get; internal set; } = null!;
    internal string? _tableString = null;
    protected readonly CompilerContextFactory contextFactory;
    protected readonly QueryBuilderContext queryBuilderCtx;
    public EntityAccessContext(CompilerContextFactory contextFactory)
    {
        this.contextFactory = contextFactory;
        this.queryBuilderCtx = (QueryBuilderContext)contextFactory.CreateNoPluginQueryBuilderContext();
    }

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

    public EdmPath AddFrom(IEdmEntityType edmEntityType, EdmPath edmPath)  => queryBuilderCtx.AddFrom(edmEntityType, edmPath);

    public EdmPath AddJoin(IEdmNavigationProperty rightNavigationProperty, EdmPath rightPath, EdmPath leftPath)  => queryBuilderCtx.AddJoin(rightNavigationProperty,rightPath, leftPath);

    public void AddSelect(EdmPath edmPath, IEdmStructuralProperty property, string? customName = null)   => queryBuilderCtx.AddSelect(edmPath, property, customName);

    public void AddSelectKey(EdmPath? path, IEdmEntityType? type)   => queryBuilderCtx.AddSelectKey(path, type);

    public void AddSelectAll(EdmPath? path, IEdmEntityType? type)   => queryBuilderCtx.AddSelectAll(path, type);

    public void AddCount(EdmPath edmPath, IEdmStructuralProperty edmStructuralProperty)  => queryBuilderCtx.AddCount(edmPath, edmStructuralProperty);

    public void AddFilter(BinaryFilter filter)   => queryBuilderCtx.AddFilter(filter);

    public void AddOrderBy(EdmPath edmPath, IEdmStructuralProperty property, OrderByDirection direction)   => queryBuilderCtx.AddOrderBy(edmPath, property, direction);

    public void WrapQuery(IOdataParserContext context, EdmPath resourcePath)   => queryBuilderCtx.WrapQuery(context, resourcePath);

    public void AddLimit(long top)  => queryBuilderCtx.AddLimit(top);

    public void AddOffset(long skip) => queryBuilderCtx.AddOffset(skip);

    public AliasStore Aliases => queryBuilderCtx.Aliases;

    public CompilerScope ActiveScope => queryBuilderCtx.ActiveScope;
}
