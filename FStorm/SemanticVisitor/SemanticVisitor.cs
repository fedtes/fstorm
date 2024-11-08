using System;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace FStorm;

public class SemanticVisitor
{
    private readonly EdmPathFactory pathFactory;
    private readonly ODataService service;

    public SemanticVisitor(EdmPathFactory pathFactory, ODataService service){
        this.pathFactory = pathFactory;
        this.service = service;
    }

    /// <summary>
    /// Visit all nodes of the clauses requested in the odata request, process it, and save the result into <see cref="ICompilerContext"/>.
    /// </summary>
    /// <param name="context"></param>
    public void VisitContext(ICompilerContext context)
    {
        EdmPath? current = context.GetOutputPath();
        current = this.VisitPath(context, context.GetOdataRequestPath(), current);
        context.SetOutputPath(current);
        this.VisitFilterClause(context, context.GetFilterClause());
        this.VisitOrderByClause(context, context.GetOrderByClause());
        
        OutputKind outputKind = context.GetOdataRequestPath().LastSegment switch {
            EntitySetSegment _ => OutputKind.Collection,
            NavigationPropertySegment s => s.EdmType.TypeKind == EdmTypeKind.Collection ? OutputKind.Collection : OutputKind.Object,
            KeySegment _ => OutputKind.Object,
            PropertySegment _ => OutputKind.Property,
            CountSegment _ => OutputKind.RawValue,
            _ => OutputKind.Collection
        };

        context.SetOutputKind(outputKind);
        context.AddSelectKey(context.GetOutputPath(), context.GetOutputType());
        bool handled = this.VisitSelectAndExpand(context, context.GetSelectAndExpand());
        
        if (!handled) 
        {
            if (context.GetOutputKind() == OutputKind.Collection || context.GetOutputKind() == OutputKind.Object) {
                context.AddSelectAll(context.GetOutputPath(), context.GetOutputType());
            }
        }

        this.VisitPagination(context, context.GetPaginationClause());
    }

#region "Visit Path"
    protected EdmPath VisitPath(ICompilerContext context, ODataPath oDataPath, EdmPath? input) 
    {
        EdmPath current = input ?? pathFactory.CreatePath();
        for (int i = 0; i < oDataPath.Count; i++)
        {
            switch (oDataPath[i])
            {
                case EntitySetSegment segment:
                    current = VisitEntitySetSegment(context, segment);
                    break;
                case NavigationPropertySegment segment:
                    current = VisitNavigationPropertySegment(context, segment, current);
                    break;
                case KeySegment segment:
                    current = VisitKeySegment(context, segment, current);
                    break;
                case PropertySegment segment:
                    current = VisitPropertySegment(context, segment, current);
                    break;
                case CountSegment segment:
                    current = VisitCountSegment(context, segment, current);
                    break;
                case FilterSegment segment:
                    current = VisitFilterSegment(context, segment, current);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        return current;
    }

    protected EdmPath VisitEntitySetSegment(ICompilerContext context, EntitySetSegment entitySetSegment) 
    {
        EdmPath alias = context.AddFrom((EdmEntityType)entitySetSegment.EdmType.AsElementType(), pathFactory.ParseString(EdmPath.PATH_ROOT + "/" + entitySetSegment.Identifier));
        return alias;
    }

    protected EdmPath VisitNavigationPropertySegment(ICompilerContext context, NavigationPropertySegment navigationPropertySegment, EdmPath currentPath) 
    {
        if (!context.HasFrom())
        {
            EdmPath alias = context.AddFrom((EdmEntityType)navigationPropertySegment.EdmType.AsElementType(), currentPath);
            return alias;
        }
        else {
            EdmPath alias = context.AddJoin(
                (EdmNavigationProperty)navigationPropertySegment.NavigationProperty,
                currentPath,
                currentPath + navigationPropertySegment.NavigationProperty.Name
            );
            return alias;
        }
    }

    protected EdmPath VisitKeySegment(ICompilerContext context, KeySegment keySegment, EdmPath currentPath) 
    {
        var k = (keySegment.NavigationSource.Type.AsElementType() as EdmEntityType)!.GetEntityKey();
        BinaryFilter filter = new BinaryFilter() {
            PropertyReference = new PropertyReference() {
                ResourcePath = pathFactory.CreatePath(keySegment.NavigationSource.Path.PathSegments.ToArray()),
                Property = k
            },
            OperatorKind= BinaryOperatorKind.Equal,
            Value = keySegment.Keys.First().Value
        };
        context.AddFilter(filter);
        //context.SetOutputKind(OutputKind.Object);
        return currentPath;
    }

    protected EdmPath VisitPropertySegment(ICompilerContext context, PropertySegment propertySegment, EdmPath currentPath) 
    {
        context.AddSelect(currentPath , (EdmStructuralProperty)propertySegment.Property);
        //context.SetOutputKind(OutputKind.Property);
        return currentPath;
    }

    protected EdmPath VisitCountSegment(ICompilerContext context, CountSegment countSegment, EdmPath currentPath) 
    {
        var k =currentPath.AsEdmElements().Last().Item2.GetEntityType().GetEntityKey();
        context.AddCount(currentPath, k);
        //context.SetOutputKind(OutputKind.RawValue);
        return currentPath;
    }

    protected EdmPath VisitFilterSegment(ICompilerContext context, FilterSegment filterSegment, EdmPath currentPath)
    {
        
        context.WrapQuery(context, currentPath);
        var _it = new Variable() 
        {
            Name = filterSegment.RangeVariable.Name,
            ResourcePath = pathFactory.CreatePath((filterSegment.RangeVariable as ResourceRangeVariable)!.NavigationSource.Path.PathSegments.ToArray()),
            Type = filterSegment.RangeVariable.TypeReference.ToStructuredType().EnsureType(service)
        };
        context.OpenVariableScope(_it);
        VisitExpression(context, filterSegment.Expression, filterSegment.RangeVariable);
        context.CloseVariableScope();
        return currentPath;
    }

#endregion

#region "Visit Select Expand"
    protected void VisitExpandedItems(ICompilerContext context, ExpandedNavigationSelectItem i) {
        
        ICompilerContext expansionContext = context.OpenExpansionScope(i);
        VisitContext(expansionContext);
        context.CloseExpansionScope(expansionContext, i);
    }

    protected bool VisitSelectAndExpand(ICompilerContext context, SelectExpandClause selectExpandClause)
    {
        if (selectExpandClause != null )
        {
            if (selectExpandClause.AllSelected && selectExpandClause.SelectedItems.Count() == 0) 
            {
                context.AddSelectAll(context.GetOutputPath(), context.GetOutputType());
                return true;
            }
            else 
            {
                bool handled = false;
                foreach (var item in selectExpandClause.SelectedItems)
                {
                    switch (item)
                    {
                        case PathSelectItem i:
                            VisitPath(context, i.SelectedPath, context.GetOutputPath());
                            handled = true;
                            break;
                        case ExpandedNavigationSelectItem i:
                            VisitExpandedItems(context, i);
                            break;
                        case WildcardSelectItem i:
                            context.AddSelectAll(context.GetOutputPath(), context.GetOutputType());
                            handled = true;
                            break;
                    }
                }
                return handled;
            }
        }

        return false;
    }
#endregion

#region "Visit Filter"
    protected void VisitFilterClause(ICompilerContext context, FilterClause filterClause){
        if (filterClause != null) {
            var _it = new Variable() 
            {
                Name = filterClause.RangeVariable.Name,
                ResourcePath = pathFactory.CreatePath((filterClause.RangeVariable as ResourceRangeVariable)!.NavigationSource.Path.PathSegments.ToArray()),
                Type = filterClause.RangeVariable.TypeReference.ToStructuredType().EnsureType(service)
            };
            context.OpenVariableScope(_it);
            VisitExpression(context, filterClause.Expression, filterClause.RangeVariable);
            context.CloseVariableScope();
        }
    }

    protected VisitResult VisitExpression(ICompilerContext context, SingleValueNode singleValueNode, RangeVariable? variable = null) 
    {
        switch (singleValueNode)
        {
            case BinaryOperatorNode node:
                return VisitBinaryOperator(context, node);
            case ConstantNode node:
                return VisitConstantNode(context, node);
            case SingleValuePropertyAccessNode node:
                return VisitSingleValuePropertyAccessNode(context, node);
            case SingleNavigationNode node:
                return VisitSingleNavigationNode(context, node);
            case ResourceRangeVariableReferenceNode node:
                return VisitResourceRangeVariableReferenceNode(context, node);
            case ConvertNode node:
                return VisitExpression(context, node.Source, variable);
            case AnyNode node:
                return VisitAnyNode(context, node);
            case AllNode node:
                return VisitAllNode(context, node);
            case UnaryOperatorNode node:
                return VisitNotNode(context, node);
            case InNode node:
                throw new NotImplementedException();
            case SingleValueFunctionCallNode node:
                return VisitSingleValueFunctionCallNode(context, node);
            default:
                throw new NotImplementedException();
        }
    }

    protected VisitResult VisitNotNode(ICompilerContext context, UnaryOperatorNode node)
    {
        if (node.OperatorKind == UnaryOperatorKind.Not) {
            context.OpenNotScope();
            VisitExpression(context, node.Operand);
            context.CloseNotScope();
        } 
        else{
            throw new NotImplementedException();
        }
        return new VisitResult();
    }

    protected VisitResult VisitBinaryOperator(ICompilerContext context, BinaryOperatorNode node)
    {
        if (node.OperatorKind == BinaryOperatorKind.And) 
        {
            context.OpenAndScope();
            VisitExpression(context, node.Left);
            VisitExpression(context, node.Right);
            context.CloseAndScope();
        } 
        else if (node.OperatorKind == BinaryOperatorKind.Or) 
        {
            context.OpenOrScope();
            VisitExpression(context, node.Left);
            VisitExpression(context, node.Right);
            context.CloseOrScope();
        } 
        else 
        {
            BinaryFilter filter = new BinaryFilter() {
                PropertyReference = (PropertyReference)VisitExpression(context, node.Left),
                OperatorKind = node.OperatorKind,
                Value = (VisitExpression(context, node.Right) as ConstantValue)?.Value
            };
            context.AddFilter(filter);
        }
        return new VisitResult();
    }

    protected VisitResult VisitSingleValuePropertyAccessNode(ICompilerContext context, SingleValuePropertyAccessNode node) {
        var v = VisitExpression(context, node.Source);
        if (v is PathValue pathValue) {
            return new PropertyReference() {
                Property = (EdmStructuralProperty)node.Property,
                ResourcePath = pathValue.ResourcePath
            };
        }
        else {
            throw new InvalidOperationException($"Unexpected property access node for {node.Property.ToString()}.");
        }
        
    }

    protected VisitResult VisitSingleNavigationNode(ICompilerContext context, SingleNavigationNode node)
    {
        if (node.NavigationSource is IEdmContainedEntitySet)
        {
            var reolvedPath = pathFactory.FromNavigationSource((IEdmContainedEntitySet)node.NavigationSource);
            var segments = reolvedPath.AsEdmElements();

            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i].Item2 is IEdmNavigationProperty)
                {
                    context.AddJoin((EdmNavigationProperty)segments[i].Item2,
                        segments[i].Item1 - 1,
                        segments[i].Item1
                    );
                }
            }

            return new PathValue() {
                ResourcePath= reolvedPath
            };
        }

        throw new InvalidOperationException("Unexpected NavigationSource type.");
    }

    protected VisitResult VisitConstantNode(ICompilerContext context, ConstantNode node) {
        return new ConstantValue() { Value = node.Value} ;
    }

    protected VisitResult VisitAnyNode(ICompilerContext context, AnyNode node)
    {
        Variable v = new Variable() {
            Name = node.CurrentRangeVariable.Name,
            ResourcePath = pathFactory.CreatePath((node.CurrentRangeVariable as ResourceRangeVariable)!.NavigationSource.Path.PathSegments.ToArray()),
            Type = node.CurrentRangeVariable.TypeReference.ToStructuredType().EnsureType(service),
        };
        context.OpenVariableScope(v);
        context.OpenAnyScope();
        VisitExpression(context, node.Body);
        context.CloseAnyScope();
        context.CloseVariableScope();
        return new VisitResult();
    }

    protected VisitResult VisitAllNode(ICompilerContext context, AllNode node)
    {
        Variable v = new Variable() {
            Name = node.CurrentRangeVariable.Name,
            ResourcePath = pathFactory.CreatePath((node.CurrentRangeVariable as ResourceRangeVariable)!.NavigationSource.Path.PathSegments.ToArray()),
            Type = node.CurrentRangeVariable.TypeReference.ToStructuredType().EnsureType(service),
        };
        context.OpenVariableScope(v);
        context.OpenAllScope();
        context.OpenNotScope();
        VisitExpression(context, node.Body);
        context.CloseNotScope();
        context.CloseAllScope();
        context.CloseVariableScope();
        return new VisitResult();
    }

    protected VisitResult VisitResourceRangeVariableReferenceNode(ICompilerContext context, ResourceRangeVariableReferenceNode node)
    {
        return context.GetVariablesInScope().First(x => x.Name == node.Name);
    }

    protected VisitResult VisitSingleValueFunctionCallNode(ICompilerContext context, SingleValueFunctionCallNode node) {
        switch (node.Name.ToLowerInvariant())
        {
            case "contains":
                context.AddFilter(new BinaryFilter() {
                    PropertyReference = (PropertyReference)VisitExpression(context, (SingleValueNode)node.Parameters.First())!,
                    OperatorKind = (BinaryOperatorKind)16,
                    Value = (VisitExpression(context, (SingleValueNode)node.Parameters.Last())! as ConstantValue)?.Value
                });
                break;
            case "endswith":
                context.AddFilter(new BinaryFilter() {
                    PropertyReference = (PropertyReference)VisitExpression(context, (SingleValueNode)node.Parameters.First())!,
                    OperatorKind = (BinaryOperatorKind)15,
                    Value = (VisitExpression(context, (SingleValueNode)node.Parameters.Last()) as ConstantValue)?.Value
                });
                break;
            case "startswith":
                context.AddFilter(new BinaryFilter() {
                    PropertyReference = (PropertyReference)VisitExpression(context, (SingleValueNode)node.Parameters.First())!,
                    OperatorKind = (BinaryOperatorKind)14,
                    Value = (VisitExpression(context, (SingleValueNode)node.Parameters.Last()) as ConstantValue)?.Value
                });
                break;
            default:
                throw new NotImplementedException();
        }
        return new VisitResult();
    }

    // protected void VisitConvertNode(ICompilerContext context, ConvertNode node) {
    //     throw new NotImplementedException();
    // }
    protected void VisitLambdaNode(ICompilerContext context, LambdaNode node) {
        throw new NotImplementedException();
    }
    protected void VisitParameterAliasNode(ICompilerContext context, ParameterAliasNode node) {
        throw new NotImplementedException();
    }
    protected void VisitSearchTermNode(ICompilerContext context, SearchTermNode node) {
        throw new NotImplementedException();
    }
    protected void VisitSingleEntityNode(ICompilerContext context, SingleEntityNode node) {
        throw new NotImplementedException();
    }
    protected void VisitSingleValueCastNode(ICompilerContext context, SingleValueCastNode node) {
        throw new NotImplementedException();
    }
    
    protected void VisitSingleValueOpenPropertyAccessNode(ICompilerContext context, SingleValueOpenPropertyAccessNode node) {
        throw new NotImplementedException();
    }

    protected void VisitUnaryOperatorNode(ICompilerContext context, UnaryOperatorNode node) {
        throw new NotImplementedException();
    }

#endregion

#region "Visit OrderBy"

    protected void VisitOrderByClause(ICompilerContext context, OrderByClause orderByClause)
    {
        if (orderByClause is null) return;
        var _it = new Variable() 
            {
                Name = orderByClause.RangeVariable.Name,
                ResourcePath = pathFactory.CreatePath((orderByClause.RangeVariable as ResourceRangeVariable)!.NavigationSource.Path.PathSegments.ToArray()),
                Type = orderByClause.RangeVariable.TypeReference.ToStructuredType().EnsureType(service)
            };
        context.OpenVariableScope(_it);
        PropertyReference propertyRef = (PropertyReference)VisitExpression(context, orderByClause.Expression, orderByClause.RangeVariable);
        context.CloseVariableScope();

        context.AddOrderBy(propertyRef.ResourcePath,propertyRef.Property, orderByClause.Direction);

        if (!(orderByClause.ThenBy is null)){
            VisitOrderByClause(context, orderByClause.ThenBy);
        }
    }

#endregion

#region "Visit Pagination"

    protected void VisitPagination(ICompilerContext context, PaginationClause pagination)
    {
        if (pagination.Top != null)
        {
            context.AddLimit(pagination.Top.Value);
        }
        if (pagination.Skip != null)
        {
            context.AddOffset(pagination.Skip.Value);
        }
    }
#endregion

}



