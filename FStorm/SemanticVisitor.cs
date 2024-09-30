using System;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace FStorm;

public class SemanticVisitor
{
    private readonly EdmPathFactory pathFactory;
    private readonly FStormService service;

    public SemanticVisitor(EdmPathFactory pathFactory, FStormService service){
        this.pathFactory = pathFactory;
        this.service = service;
    }

    public void VisitPath(CompilerContext context, ODataPath oDataPath) 
    {
        for (int i = 0; i < oDataPath.Count; i++)
        {
            switch (oDataPath[i])
            {
                case EntitySetSegment segment:
                    VisitEntitySetSegment(context, segment);
                    break;
                case NavigationPropertySegment segment:
                    VisitNavigationPropertySegment(context, segment);
                    break;
                case KeySegment segment:
                    VisitKeySegment(context, segment);
                    break;
                case PropertySegment segment:
                    VisitPropertySegment(context, segment);
                    break;
                case CountSegment segment:
                    VisitCountSegment(context, segment);
                    break;
                case FilterSegment segment:
                    VisitFilterSegment(context, segment);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public void VisitEntitySetSegment(CompilerContext context, EntitySetSegment entitySetSegment) 
    {
        EdmPath alias = context.AddFrom((EdmEntityType)entitySetSegment.EdmType.AsElementType(), pathFactory.ParseString(EdmPath.PATH_ROOT + "/" + entitySetSegment.Identifier));
        context.SetOutputKind(OutputKind.Collection);
        context.SetOutputType((EdmEntityType)entitySetSegment.EdmType.AsElementType());
        context.SetOutputPath(alias);
    }

    public void VisitNavigationPropertySegment(CompilerContext context, NavigationPropertySegment navigationPropertySegment) 
    {

        EdmPath alias = context.AddJoin(
            (EdmNavigationProperty)navigationPropertySegment.NavigationProperty,
            context.GetOutputPath(),
            context.GetOutputPath() + navigationPropertySegment.NavigationProperty.Name
        );

        context.SetOutputKind(navigationPropertySegment.EdmType.TypeKind == EdmTypeKind.Collection ? OutputKind.Collection : OutputKind.Object);
        context.SetOutputType((EdmEntityType)navigationPropertySegment.EdmType.AsElementType());
        context.SetOutputPath(alias);
    }

    public void VisitKeySegment(CompilerContext context, KeySegment keySegment) 
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
        context.SetOutputKind(OutputKind.Object);
    }

    public void VisitPropertySegment(CompilerContext context, PropertySegment propertySegment) 
    {
        context.AddSelect(context.GetOutputPath() , (EdmStructuralProperty)propertySegment.Property);
        context.SetOutputKind(OutputKind.Property);
    }

    public void VisitCountSegment(CompilerContext context, CountSegment countSegment) 
    {
        context.AddCount(context.GetOutputPath(), context.GetOutputType()!.GetEntityKey());
        context.SetOutputKind(OutputKind.RawValue);
    }

    public void VisitFilterSegment(CompilerContext context, FilterSegment filterSegment)
    {
        
        context.WrapQuery(context.GetOutputPath());
        var _it = new Variable() 
        {
            Name = filterSegment.RangeVariable.Name,
            ResourcePath = pathFactory.CreatePath((filterSegment.RangeVariable as ResourceRangeVariable)!.NavigationSource.Path.PathSegments.ToArray()),
            Type = filterSegment.RangeVariable.TypeReference.ToStructuredType().EnsureType(service)
        };
        context.OpenVariableScope(_it);
        VisitExpression(context, filterSegment.Expression, filterSegment.RangeVariable);
        context.CloseVariableScope();
    }

    public void VisitFilterClause(CompilerContext context, FilterClause filterClause){
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


    public ExpressionValue? VisitExpression(CompilerContext context, SingleValueNode singleValueNode, RangeVariable? variable = null) 
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
            default:
                throw new NotImplementedException();
        }
    }

    public ExpressionValue? VisitBinaryOperator(CompilerContext context, BinaryOperatorNode node)
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
                Value = (VisitExpression(context, node.Right) as ConstantValue).Value
            };
            context.AddFilter(filter);
        }
        return new ExpressionValue();
    }

    public ExpressionValue? VisitSingleValuePropertyAccessNode(CompilerContext context, SingleValuePropertyAccessNode node) {
        return new PropertyReference() {
            Property = (EdmStructuralProperty)node.Property,
            ResourcePath = (VisitExpression(context, node.Source) as Variable).ResourcePath
        };
    }

    private Variable VisitSingleNavigationNode(CompilerContext context, SingleNavigationNode node)
    {
        if (node.NavigationSource is IEdmContainedEntitySet)
        {
            var reolvedPath = pathFactory.FromNavigationSource((IEdmContainedEntitySet)node.NavigationSource);
            var segments = reolvedPath.AsEdmElements();

            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i].Item2 is IEdmNavigationProperty)
                {
                    List<string> s = new List<string>();
                    for (int j = 0; j <= i; j++)
                    {
                        s.Add(segments[j].Item1.ToString());
                    }
                    context.AddJoin((EdmNavigationProperty)segments[i].Item2,
                        pathFactory.CreatePath(s.ToArray()) - 1,
                        pathFactory.CreatePath(s.ToArray())
                    );
                }
            }

            return new Variable() {
                Name = reolvedPath.Last().ToString(),
                Type = reolvedPath.GetContainerType()!,
                ResourcePath = reolvedPath
            };
        }

        throw new InvalidOperationException("Unexpected NavigationSource type.");
    }

    public ExpressionValue? VisitConstantNode(CompilerContext context, ConstantNode node) {
        return new ConstantValue() { Value = node.Value} ;
    }

    private ExpressionValue? VisitAnyNode(CompilerContext context, AnyNode node)
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
        return new ExpressionValue();
    }


    public void VisitConvertNode(CompilerContext context, ConvertNode node) {
        throw new NotImplementedException();
    }
    public void VisitLambdaNode(CompilerContext context, LambdaNode node) {
        throw new NotImplementedException();
    }
    public void VisitParameterAliasNode(CompilerContext context, ParameterAliasNode node) {
        throw new NotImplementedException();
    }
    public void VisitSearchTermNode(CompilerContext context, SearchTermNode node) {
        throw new NotImplementedException();
    }
    public void VisitSingleEntityNode(CompilerContext context, SingleEntityNode node) {
        throw new NotImplementedException();
    }
    public void VisitSingleValueCastNode(CompilerContext context, SingleValueCastNode node) {
        throw new NotImplementedException();
    }
    public void VisitSingleValueFunctionCallNode(CompilerContext context, SingleValueFunctionCallNode node) {
        throw new NotImplementedException();
    }
    public void VisitSingleValueOpenPropertyAccessNode(CompilerContext context, SingleValueOpenPropertyAccessNode node) {
        throw new NotImplementedException();
    }

    public void VisitUnaryOperatorNode(CompilerContext context, UnaryOperatorNode node) {
        throw new NotImplementedException();
    }

    // public ExpressionValue? VisitSingleResourceNode(CompilerContext context, Microsoft.OData.UriParser.SingleValueNode node) {
    //     return node switch {
    //         Microsoft.OData.UriParser.ResourceRangeVariableReferenceNode n => VisitResourceRangeVariableReferenceNode(context, n),
    //         _ => throw new NotImplementedException()
    //     };
    // }

    private ExpressionValue? VisitResourceRangeVariableReferenceNode(CompilerContext context, ResourceRangeVariableReferenceNode node)
    {
        return context.GetVariablesInScope().First(x => x.Name == node.Name);
    }


    /* 
    Microsoft.OData.Core.UriParser.Semantic.BinaryOperatorNode
            Microsoft.OData.Core.UriParser.Semantic.ConstantNode
            Microsoft.OData.Core.UriParser.Semantic.ConvertNode
            Microsoft.OData.Core.UriParser.Semantic.LambdaNode
            Microsoft.OData.Core.UriParser.Semantic.NonentityRangeVariableReferenceNode
            Microsoft.OData.Core.UriParser.Semantic.ParameterAliasNode
            Microsoft.OData.Core.UriParser.Semantic.SearchTermNode
            Microsoft.OData.Core.UriParser.Semantic.SingleEntityNode
            Microsoft.OData.Core.UriParser.Semantic.SingleValueCastNode
            Microsoft.OData.Core.UriParser.Semantic.SingleValueFunctionCallNode
            Microsoft.OData.Core.UriParser.Semantic.SingleValueOpenPropertyAccessNode
            Microsoft.OData.Core.UriParser.Semantic.SingleValuePropertyAccessNode
            Microsoft.OData.Core.UriParser.Semantic.UnaryOperatorNode


    Microsoft.OData.Core.UriParser.Semantic.ODataPath:
      derived:
        Microsoft.OData.Core.UriParser.Semantic.ODataExpandPath
      fields: 
        Microsoft.OData.Core.UriParser.Semantic.ODataPathSegment[]
     

    Microsoft.OData.Core.UriParser.Semantic.ODataPathSegment:
      derived:
        Microsoft.OData.Core.UriParser.Semantic.EntitySetSegment
        Microsoft.OData.Core.UriParser.Semantic.KeySegment
        Microsoft.OData.Core.UriParser.Semantic.NavigationPropertySegment
        Microsoft.OData.Core.UriParser.Semantic.PropertySegment
        Microsoft.OData.Core.UriParser.Semantic.CountSegment
        -------------------------------------------------------------------
        Microsoft.OData.Core.UriParser.Semantic.BatchReferenceSegment
        Microsoft.OData.Core.UriParser.Semantic.BatchSegment
        Microsoft.OData.Core.UriParser.Semantic.MetadataSegment
        Microsoft.OData.Core.UriParser.Semantic.NavigationPropertyLinkSegment
        Microsoft.OData.Core.UriParser.Semantic.OpenPropertySegment
        Microsoft.OData.Core.UriParser.Semantic.OperationImportSegment
        Microsoft.OData.Core.UriParser.Semantic.OperationSegment
        Microsoft.OData.Core.UriParser.Semantic.SingletonSegment
        Microsoft.OData.Core.UriParser.Semantic.TypeSegment
        Microsoft.OData.Core.UriParser.Semantic.ValueSegment

    Microsoft.OData.Core.UriParser.Semantic.QueryNode:
      derived:
        Microsoft.OData.Core.UriParser.Semantic.SingleValueNode:
          derived:
            Microsoft.OData.Core.UriParser.Semantic.BinaryOperatorNode
            Microsoft.OData.Core.UriParser.Semantic.ConstantNode
            Microsoft.OData.Core.UriParser.Semantic.ConvertNode
            Microsoft.OData.Core.UriParser.Semantic.LambdaNode
            Microsoft.OData.Core.UriParser.Semantic.NonentityRangeVariableReferenceNode
            Microsoft.OData.Core.UriParser.Semantic.ParameterAliasNode
            Microsoft.OData.Core.UriParser.Semantic.SearchTermNode
            Microsoft.OData.Core.UriParser.Semantic.SingleEntityNode
            Microsoft.OData.Core.UriParser.Semantic.SingleValueCastNode
            Microsoft.OData.Core.UriParser.Semantic.SingleValueFunctionCallNode
            Microsoft.OData.Core.UriParser.Semantic.SingleValueOpenPropertyAccessNode
            Microsoft.OData.Core.UriParser.Semantic.SingleValuePropertyAccessNode
            Microsoft.OData.Core.UriParser.Semantic.UnaryOperatorNode
    */
}


public class ExpressionValue
{

}

public class ConstantValue : ExpressionValue
{
    public object? Value {get; set;}
}

public class PropertyReference : ExpressionValue
{
    public EdmPath ResourcePath {get; set;} = null!;

    public EdmStructuralProperty Property {get; set;} = null!;
}

public class Variable : ExpressionValue
{
    public EdmPath ResourcePath {get; set;} = null!;

    public EdmEntityType Type {get; set;} = null!;

    public String Name {get; set;} = null!;
}


public class Filter : ExpressionValue
{

}


public class BinaryFilter : Filter
{
    public PropertyReference PropertyReference {get; set;} = null!;
    public BinaryOperatorKind OperatorKind = BinaryOperatorKind.Equal;
    public object Value = null!;
}

public class AndFilter : Filter
{
    public List<Filter> Filters {get; set;} = new List<Filter>();
}