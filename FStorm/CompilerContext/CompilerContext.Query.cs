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

    internal virtual List<IOnEntityAccess> GetOnEntityAccessPlugins(EdmEntityType edmEntityType) 
    {
        return service.serviceProvider.GetServices<IOnEntityAccess>().Where(x => x.EntityName == edmEntityType.FullName()).ToList();
    }

    public EdmPath AddFrom(EdmEntityType edmEntityType, EdmPath edmPath)
    {
        var _alias = this.Aliases.AddOrGet(edmPath);
        var plugins = GetOnEntityAccessPlugins(edmEntityType);

        if (!plugins.Any()) 
        {
            MainQuery.From(edmEntityType.Table + " as " + _alias.ToString());
        }
        else 
        {
            EntityAccessContext _EntityAccessContext =(EntityAccessContext)service.serviceProvider.GetService<IEntityAccessContext>()!;
            _EntityAccessContext.Kind = IEntityAccessContext.TABLE_STRING;
            _EntityAccessContext.InitialTableString = edmEntityType.Table;
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

    public EdmPath AddJoin(EdmNavigationProperty rightNavigationProperty, EdmPath rightPath, EdmPath leftPath)
    {
        if (this.Aliases.Contains(leftPath)) return leftPath;
        var r = this.Aliases.AddOrGet(rightPath);
        var l = this.Aliases.AddOrGet(leftPath);
        var (sourceProperty, targetProperty) = rightNavigationProperty.GetRelationProperties();
        this.MainQuery.LeftJoin((rightNavigationProperty.Type.Definition.AsElementType() as EdmEntityType)!.Table + " as " + l, $"{l}.{targetProperty.columnName}", $"{r}.{sourceProperty.columnName}");
        return leftPath;
    }

    public virtual void AddSelect(EdmPath edmPath, EdmStructuralProperty property, string? customName = null)
    {
        var p = this.Aliases.AddOrGet(edmPath);
        ActiveQuery.Select($"{p}.{property.columnName} as {customName ?? p + "/" + property.Name}");
    }

    public void AddSelectKey(EdmPath? path, EdmEntityType? type)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(type);
        var p = this.Aliases.AddOrGet(path);
        this.AddSelect(path, type.GetEntityKey(), $"{p}/:key");
    }

    public void AddSelectAll(EdmPath? path, EdmEntityType? type)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(type);
        foreach (var property in type.DeclaredStructuralProperties())
        {
            this.AddSelect(path, (EdmStructuralProperty)property);
        }
    }

    public void AddCount(EdmPath edmPath, EdmStructuralProperty edmStructuralProperty)
    {
        var p = this.Aliases.AddOrGet(edmPath);
        ActiveQuery.AsCount(new string[] { $"{p}.{edmStructuralProperty.columnName}" });
    }

    public void AddFilter(BinaryFilter filter)
    {
        var p = this.Aliases.AddOrGet(filter.PropertyReference.ResourcePath);
        switch (filter.OperatorKind)
        {
            case BinaryOperatorKind.Or:
                throw new NotImplementedException("should not pass here!");
            case BinaryOperatorKind.And:
                throw new NotImplementedException("should not pass here!");
            case BinaryOperatorKind.Equal:
                if (filter.Value is null)
                {
                    (IsOr ? ActiveQuery.Or() : ActiveQuery).WhereNull($"{p}.{filter.PropertyReference.Property.columnName}");
                }
                else
                {
                    (IsOr ? ActiveQuery.Or() : ActiveQuery).Where($"{p}.{filter.PropertyReference.Property.columnName}", "=", filter.Value);
                }
                break;
            case BinaryOperatorKind.NotEqual:
                if (filter.Value is null)
                {
                    (IsOr ? ActiveQuery.Or() : ActiveQuery).WhereNotNull($"{p}.{filter.PropertyReference.Property.columnName}");
                }
                else
                {
                    (IsOr ? ActiveQuery.Or() : ActiveQuery).Where($"{p}.{filter.PropertyReference.Property.columnName}", "<>", filter.Value);
                }
                break;
            case BinaryOperatorKind.GreaterThan:
                ArgumentNullException.ThrowIfNull(filter.Value, nameof(filter.Value));
                (IsOr ? ActiveQuery.Or() : ActiveQuery).Where($"{p}.{filter.PropertyReference.Property.columnName}", ">", filter.Value);
                break;
            case BinaryOperatorKind.GreaterThanOrEqual:
                ArgumentNullException.ThrowIfNull(filter.Value, nameof(filter.Value));
                (IsOr ? ActiveQuery.Or() : ActiveQuery).Where($"{p}.{filter.PropertyReference.Property.columnName}", ">=", filter.Value);
                break;
            case BinaryOperatorKind.LessThan:
                ArgumentNullException.ThrowIfNull(filter.Value, nameof(filter.Value));
                (IsOr ? ActiveQuery.Or() : ActiveQuery).Where($"{p}.{filter.PropertyReference.Property.columnName}", "<", filter.Value);
                break;
            case BinaryOperatorKind.LessThanOrEqual:
                ArgumentNullException.ThrowIfNull(filter.Value, nameof(filter.Value));
                (IsOr ? ActiveQuery.Or() : ActiveQuery).Where($"{p}.{filter.PropertyReference.Property.columnName}", "<=", filter.Value);
                break;
            case BinaryOperatorKind.Add:
                throw new NotImplementedException();
            case BinaryOperatorKind.Subtract:
                throw new NotImplementedException();
            case BinaryOperatorKind.Multiply:
                throw new NotImplementedException();
            case BinaryOperatorKind.Divide:
                throw new NotImplementedException();
            case BinaryOperatorKind.Modulo:
                throw new NotImplementedException();
            case BinaryOperatorKind.Has:
                throw new NotImplementedException();
            case (BinaryOperatorKind)14: //startswith
                (IsOr ? ActiveQuery.Or() : ActiveQuery).WhereLike($"{p}.{filter.PropertyReference.Property.columnName}", $"{filter.Value}%", true);
                break;
            case (BinaryOperatorKind)15: //endswith
                (IsOr ? ActiveQuery.Or() : ActiveQuery).WhereLike($"{p}.{filter.PropertyReference.Property.columnName}", $"%{filter.Value}", true);
                break;
            case (BinaryOperatorKind)16: //contains
                (IsOr ? ActiveQuery.Or() : ActiveQuery).WhereLike($"{p}.{filter.PropertyReference.Property.columnName}", $"%{filter.Value}%", true);
                break;
        }
    }

    public void AddOrderBy(EdmPath edmPath, EdmStructuralProperty property, OrderByDirection direction)
    {
        var p = this.Aliases.AddOrGet(edmPath);
        if (direction == OrderByDirection.Ascending)
        {

            ActiveQuery.OrderBy($"{p}.{property.columnName}");
        }
        else
        {
            ActiveQuery.OrderByDesc($"{p}.{property.columnName}");
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
    internal override List<IOnEntityAccess> GetOnEntityAccessPlugins(EdmEntityType edmEntityType) => new List<IOnEntityAccess>();

    /// <summary>
    /// Avoid computing aliases on output columns used in the query. This allow to see the nested query as a "table" by letting the columns name explicitly.
    /// </summary>
    public override void AddSelect(EdmPath edmPath, EdmStructuralProperty property, string? customName = null)
    {
        var p = this.Aliases.AddOrGet(edmPath);
        ActiveQuery.Select($"{p}.{property.columnName}{(!String.IsNullOrEmpty(customName) ? "as " + customName : "" )}");
    }
}