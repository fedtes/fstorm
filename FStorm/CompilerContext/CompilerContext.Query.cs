using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Extensions.DependencyInjection;


namespace FStorm;

public partial class CompilerContext
{
    public EdmPath AddFrom(EdmEntityType edmEntityType, EdmPath edmPath)
    {
        var p = ((ICompilerContext)this).Aliases.AddOrGet(edmPath);
        this.MainQuery.From(edmEntityType.Table + " as " + p.ToString());
        this.MainScope.HasFromClause = true;
        return edmPath;
    }

    public EdmPath AddJoin(EdmNavigationProperty rightNavigationProperty, EdmPath rightPath, EdmPath leftPath)
    {
        if (((ICompilerContext)this).Aliases.Contains(leftPath)) return leftPath;
        var r = ((ICompilerContext)this).Aliases.AddOrGet(rightPath);
        var l = ((ICompilerContext)this).Aliases.AddOrGet(leftPath);
        var (sourceProperty, targetProperty) = rightNavigationProperty.GetRelationProperties();
        this.MainQuery.LeftJoin((rightNavigationProperty.Type.Definition.AsElementType() as EdmEntityType)!.Table + " as " + l, $"{l}.{targetProperty.columnName}", $"{r}.{sourceProperty.columnName}");
        return leftPath;
    }

    public void AddSelect(EdmPath edmPath, EdmStructuralProperty property, string? customName = null)
    {
        var p = ((ICompilerContext)this).Aliases.AddOrGet(edmPath);
        ActiveQuery.Select($"{p}.{property.columnName} as {customName ?? p + "/" + property.Name}");
    }

    public void AddSelectKey(EdmPath? path, EdmEntityType? type)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(type);
        var p = ((ICompilerContext)this).Aliases.AddOrGet(path);
        ((ICompilerContext)this).AddSelect(path, type.GetEntityKey(), $"{p}/:key");
    }

    public void AddSelectAll(EdmPath? path, EdmEntityType? type)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(type);
        foreach (var property in type.DeclaredStructuralProperties())
        {
            ((ICompilerContext)this).AddSelect(path, (EdmStructuralProperty)property);
        }
    }

    public void AddCount(EdmPath edmPath, EdmStructuralProperty edmStructuralProperty)
    {
        var p = ((ICompilerContext)this).Aliases.AddOrGet(edmPath);
        ActiveQuery.AsCount(new string[] { $"{p}.{edmStructuralProperty.columnName}" });
    }

    public void AddFilter(BinaryFilter filter)
    {
        var p = ((ICompilerContext)this).Aliases.AddOrGet(filter.PropertyReference.ResourcePath);
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
        var p = ((ICompilerContext)this).Aliases.AddOrGet(edmPath);
        if (direction == OrderByDirection.Ascending)
        {

            ActiveQuery.OrderBy($"{p}.{property.columnName}");
        }
        else
        {
            ActiveQuery.OrderByDesc($"{p}.{property.columnName}");
        }
    }
    public void WrapQuery(EdmPath resourcePath)
    {
        if (scope.Count > 1 || scope.Peek().ScopeType != CompilerScope.ROOT)
        {
            throw new Exception("Cannot wrap query while a sub compiler scope is open or the current scopo is not the root");
        }

        foreach (var property in resourcePath.GetEdmEntityType().DeclaredStructuralProperties())
        {
            var p = (EdmStructuralProperty)property;
            ((ICompilerContext)this).AddSelect(resourcePath, p, p.columnName);
        }
        ICompilerContext tmpctx = service.serviceProvider.GetService<CompilerContextFactory>()!.CreateContext(this._UriRequest, ((ICompilerContext)this).GetOdataRequestPath(), filter, selectExpand, orderBy, pagination, skipToken);
        var a = tmpctx.Aliases.AddOrGet(resourcePath);
        SetMainQuery(((CompilerContext)tmpctx).ActiveQuery.From(this.ActiveQuery, a), tmpctx.Aliases);
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