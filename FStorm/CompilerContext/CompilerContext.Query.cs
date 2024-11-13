using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Extensions.DependencyInjection;


namespace FStorm;

public partial class QueryBuilderContext : IQueryBuilderContext
{
    private readonly ODataService service;

    protected virtual string AliasPrefix {get => "P"; }

    internal QueryBuilderContext(ODataService service)
    {
        this.service = service;
        SetMainQuery(service.serviceProvider.GetService<IQueryBuilder>()!, new AliasStore(AliasPrefix));
    }

    /// <summary>
    /// True if in the current scope, the where clauses, are in OR relation each others.
    /// </summary>
    private bool IsOr { get => ActiveScope.ScopeType == CompilerScope.OR; }

    /// <summary>
    /// List of all aliases used in the From clause
    /// </summary>
    public AliasStore Aliases { get => MainScope.Aliases; }

    public bool HasFrom() => MainScope.HasFromClause;
    public IQueryBuilder GetQuery() => ActiveQuery;

    internal virtual List<IOnEntityAccess> GetOnEntityAccessPlugins(IEdmEntityType edmEntityType) 
    {
        return service.serviceProvider.GetServices<IOnEntityAccess>().Where(x => x.EntityName == edmEntityType.FullName()).ToList();
    }

    internal virtual List<IOnPropertyNavigation> GetOnPropertyNavigationPlugins(IEdmNavigationProperty rightNavigationProperty) 
    {
        return service.serviceProvider.GetServices<IOnPropertyNavigation>().Where(x => x.EntityName == ((IEdmEntityType)rightNavigationProperty.DeclaringType).FullName() && x.PropertyName == rightNavigationProperty.Name).ToList();
    }

    public EdmPath AddFrom(IEdmEntityType edmEntityType, EdmPath edmPath)
    {
        var _alias = this.Aliases.AddOrGet(edmPath);
        var plugins = GetOnEntityAccessPlugins(edmEntityType);
        var _table = ((EdmEntityType)edmEntityType).Table;

        if (!plugins.Any()) 
        {
            MainQuery.From(_table + " as " + _alias.ToString());
        }
        else 
        {
            EntityAccessContext _EntityAccessContext =(EntityAccessContext)service.serviceProvider.GetService<IEntityAccessContext>()!;
            _EntityAccessContext.Kind = IEntityAccessContext.TABLE_STRING;
            _EntityAccessContext.InitialTableString = _table;
            _EntityAccessContext.Alias = _alias;
            _EntityAccessContext.Me = new Variable() { Name= "$me", ResourcePath = edmPath, Type = edmEntityType};
        
            foreach (var plugin in plugins)
            {
                plugin.OnAccess(_EntityAccessContext);
            }

            if (_EntityAccessContext.Kind == IEntityAccessContext.TABLE_STRING) 
            {
                this.MainQuery.From(_EntityAccessContext.GetTableString() + " as " + _alias.ToString());
            }
            else 
            {
                this.MainQuery.From(_EntityAccessContext.GetNestedQuery(), _alias.ToString());
            }
        }
        this.MainScope.HasFromClause = true;
        return edmPath;
    }

    public EdmPath AddJoin(IEdmNavigationProperty rightNavigationProperty, EdmPath rightPath, EdmPath leftPath)
    {
        if (this.Aliases.Contains(leftPath)) return leftPath;
        var r = this.Aliases.AddOrGet(rightPath);
        var l = this.Aliases.AddOrGet(leftPath);
        var (sourceProperty, targetProperty) = ((EdmNavigationProperty)rightNavigationProperty).GetRelationProperties();
        var plugins = GetOnPropertyNavigationPlugins(rightNavigationProperty);
        var leftType = (rightNavigationProperty.Type.Definition.AsElementType() as EdmEntityType)!;
        var leftTable = leftType.Table;

        if (!plugins.Any()) 
        {
            this.MainQuery.LeftJoin(leftTable + " as " + l, $"{l}.{targetProperty.columnName}", $"{r}.{sourceProperty.columnName}");
        }
        else 
        {
            OnPropertyNavigationContext _OnPropertyNavigationContext = (OnPropertyNavigationContext)service.serviceProvider.GetService<IOnPropertyNavigationContext>()!;
            _OnPropertyNavigationContext.RightAlias = r;
            _OnPropertyNavigationContext.LeftAlias = l;
            _OnPropertyNavigationContext.Right = new Variable() { Name= "$right", ResourcePath = rightPath, Type = (IEdmEntityType)rightNavigationProperty.DeclaringType };
            _OnPropertyNavigationContext.Left = new Variable() { Name= "$left", ResourcePath = leftPath, Type = leftType };
            _OnPropertyNavigationContext._accessContext.Kind = IEntityAccessContext.TABLE_STRING;
            _OnPropertyNavigationContext._accessContext.InitialTableString = leftTable;
            _OnPropertyNavigationContext._accessContext.Alias = l;
            _OnPropertyNavigationContext._accessContext.Me = new Variable() { Name= "$me", ResourcePath = leftPath, Type = leftType };

            foreach (var plugin in plugins)
            {
                plugin.OnNavigation(_OnPropertyNavigationContext);
            }

            if (_OnPropertyNavigationContext.AccessContext.Kind == IEntityAccessContext.TABLE_STRING) 
            {
                if (_OnPropertyNavigationContext.CustomizedJoin)
                {
                    this.MainQuery.LeftJoin(_OnPropertyNavigationContext.AccessContext.GetTableString() + " as " + l, _OnPropertyNavigationContext.GetJoinCondition());
                }
                else 
                {
                    this.MainQuery.LeftJoin(_OnPropertyNavigationContext.AccessContext.GetTableString() + " as " + l, $"{l}.{targetProperty.columnName}", $"{r}.{sourceProperty.columnName}");
                }
            }
            else 
            {
                if (_OnPropertyNavigationContext.CustomizedJoin)
                {
                    this.MainQuery.LeftJoin(_OnPropertyNavigationContext.GetNestedQuery(), _OnPropertyNavigationContext.GetJoinCondition());
                }
                else 
                {
                    this.MainQuery.LeftJoin(_OnPropertyNavigationContext.GetNestedQuery(), $"{l}.{targetProperty.columnName}", $"{r}.{sourceProperty.columnName}");
                }
            }
        }

        return leftPath;
    }

    public virtual void AddSelect(EdmPath edmPath, IEdmStructuralProperty property, string? customName = null)
    {
        var a = this.Aliases.AddOrGet(edmPath);
        var p = (EdmStructuralProperty)property;
        ActiveQuery.Select($"{a}.{p.columnName} as {customName ?? a + "/" + p.Name}");
    }

    public void AddSelectKey(EdmPath? path, IEdmEntityType? type)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(type);
        var p = this.Aliases.AddOrGet(path);
        this.AddSelect(path, type.GetEntityKey(), $"{p}/:key");
    }

    public void AddSelectAll(EdmPath? path, IEdmEntityType? type)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(type);
        foreach (var property in type.DeclaredStructuralProperties())
        {
            this.AddSelect(path, (EdmStructuralProperty)property);
        }
    }

    public void AddCount(EdmPath edmPath, IEdmStructuralProperty edmStructuralProperty)
    {
        var a = this.Aliases.AddOrGet(edmPath);
        var p =  (EdmStructuralProperty)edmStructuralProperty;
        ActiveQuery.AsCount(new string[] { $"{a}.{p.columnName}" });
    }

    public void AddFilter(BinaryFilter filter)
    {
        var a = this.Aliases.AddOrGet(filter.PropertyReference.ResourcePath);
        var p = (EdmStructuralProperty)filter.PropertyReference.Property;
        switch (filter.OperatorKind)
        {
            case FilterOperatorKind.Equal:

                if (filter.Value is null)
                {
                    (IsOr ? ActiveQuery.Or() : ActiveQuery).WhereNull($"{a}.{p.columnName}");
                }
                else
                {
                    (IsOr ? ActiveQuery.Or() : ActiveQuery).Where($"{a}.{p.columnName}", "=", filter.Value);
                }
                break;

            case FilterOperatorKind.NotEqual:

                if (filter.Value is null)
                {
                    (IsOr ? ActiveQuery.Or() : ActiveQuery).WhereNotNull($"{a}.{p.columnName}");
                }
                else
                {
                    (IsOr ? ActiveQuery.Or() : ActiveQuery).Where($"{a}.{p.columnName}", "<>", filter.Value);
                }
                break;

            case FilterOperatorKind.GreaterThan:

                ArgumentNullException.ThrowIfNull(filter.Value, nameof(filter.Value));
                (IsOr ? ActiveQuery.Or() : ActiveQuery).Where($"{a}.{p.columnName}", ">", filter.Value);
                break;

            case FilterOperatorKind.GreaterThanOrEqual:

                ArgumentNullException.ThrowIfNull(filter.Value, nameof(filter.Value));
                (IsOr ? ActiveQuery.Or() : ActiveQuery).Where($"{a}.{p.columnName}", ">=", filter.Value);
                break;

            case FilterOperatorKind.LessThan:

                ArgumentNullException.ThrowIfNull(filter.Value, nameof(filter.Value));
                (IsOr ? ActiveQuery.Or() : ActiveQuery).Where($"{a}.{p.columnName}", "<", filter.Value);
                break;

            case FilterOperatorKind.LessThanOrEqual:

                ArgumentNullException.ThrowIfNull(filter.Value, nameof(filter.Value));
                (IsOr ? ActiveQuery.Or() : ActiveQuery).Where($"{a}.{p.columnName}", "<=", filter.Value);
                break;

            case FilterOperatorKind.Has:
                throw new NotImplementedException();

            case FilterOperatorKind.StartsWith: //startswith

                (IsOr ? ActiveQuery.Or() : ActiveQuery).WhereLike($"{a}.{p.columnName}", $"{filter.Value}%", true);
                break;

            case FilterOperatorKind.EndsWith: //endswith

                (IsOr ? ActiveQuery.Or() : ActiveQuery).WhereLike($"{a}.{p.columnName}", $"%{filter.Value}", true);
                break;

            case FilterOperatorKind.Contains: //contains

                (IsOr ? ActiveQuery.Or() : ActiveQuery).WhereLike($"{a}.{p.columnName}", $"%{filter.Value}%", true);
                break;
        }
    }

    public void AddOrderBy(EdmPath edmPath, IEdmStructuralProperty property, OrderByDirection direction)
    {
        var a = this.Aliases.AddOrGet(edmPath);
        var p =  (EdmStructuralProperty)property;
        if (direction == OrderByDirection.Ascending)
        {

            ActiveQuery.OrderBy($"{a}.{p.columnName}");
        }
        else
        {
            ActiveQuery.OrderByDesc($"{a}.{p.columnName}");
        }
    }
    public void WrapQuery(IOdataParserContext parentContext,EdmPath resourcePath)
    {
        if (scopes.Count > 1 || scopes.Peek().ScopeType != CompilerScope.ROOT)
        {
            throw new Exception("Cannot wrap query while a sub compiler scope is open or the current scopo is not the root");
        }

        foreach (var property in resourcePath.GetEdmEntityType().DeclaredStructuralProperties())
        {
            var p = (EdmStructuralProperty)property;
            this.AddSelect(resourcePath, p, p.columnName);
        }
        ICompilerContext tmpctx = service.serviceProvider.GetService<CompilerContextFactory>()!.CreateContext(parentContext);
        var a = tmpctx.Aliases.AddOrGet(resourcePath);
        SetMainQuery((tmpctx.ActiveScope.Query is null ? throw new ArgumentNullException("tmpctx.ActiveScope.Query") : tmpctx.ActiveScope.Query).From(this.ActiveQuery, a), tmpctx.Aliases);
    }


    private void SetMainQuery(IQueryBuilder query, AliasStore? aliasStore = null)
    {
        scopes.Clear();
        scopes.Push(new CompilerScope(CompilerScope.ROOT, query, aliasStore));
    }

    public void AddLimit(long top)
    {
        ActiveQuery.Limit(top);
    }

    public void AddOffset(long skip)
    {
        ActiveQuery.Offset(skip);
    }
}




public class NoPluginQueryBuilderContext : QueryBuilderContext
{
    internal NoPluginQueryBuilderContext(ODataService service) : base(service)
    {    }

    protected override string AliasPrefix => "X";

    /// <summary>
    /// Override to prevent fire of plugin from plugin execution to prevent loops. Could be done better by filtering only the current invoked entity.
    /// </summary>
    internal override List<IOnEntityAccess> GetOnEntityAccessPlugins(IEdmEntityType edmEntityType) => new List<IOnEntityAccess>();

    /// <summary>
    /// Avoid computing aliases on output columns used in the query. This allow to see the nested query as a "table" by letting the columns name explicitly.
    /// </summary>
    public override void AddSelect(EdmPath edmPath, IEdmStructuralProperty property, string? customName = null)
    {
        var a = this.Aliases.AddOrGet(edmPath);
        var p = (EdmStructuralProperty)property;
        ActiveQuery.Select($"{a}.{p.columnName}{(!String.IsNullOrEmpty(customName) ? "as " + customName : "" )}");
    }
}