using System;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace FStorm;

public class SemanticVisitor
{
    private readonly EdmPathFactory pathFactory;

    public SemanticVisitor(EdmPathFactory pathFactory){
        this.pathFactory = pathFactory;
    }

    public void VisitPath(CompilerContext context, Microsoft.OData.UriParser.ODataPath oDataPath) 
    {
        for (int i = 0; i < oDataPath.Count; i++)
        {
            switch (oDataPath[i])
            {
                case Microsoft.OData.UriParser.EntitySetSegment segment:
                    VisitEntitySetSegment(context, segment);
                    break;
                case Microsoft.OData.UriParser.NavigationPropertySegment segment:
                    VisitNavigationPropertySegment(context, segment);
                    break;
                case Microsoft.OData.UriParser.KeySegment segment:
                    VisitKeySegment(context, segment);
                    break;
                case Microsoft.OData.UriParser.PropertySegment segment:
                    VisitPropertySegment(context, segment);
                    break;
                case Microsoft.OData.UriParser.CountSegment segment:
                    VisitCountSegment(context, segment);
                    break;
                case Microsoft.OData.UriParser.FilterSegment segment:
                    VisitFilterSegment(context, segment);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

    }

    public void VisitEntitySetSegment(CompilerContext context, Microsoft.OData.UriParser.EntitySetSegment entitySetSegment) 
    {
        EdmPath alias = context.AddFrom((EdmEntityType)entitySetSegment.EdmType.AsElementType(), pathFactory.Parse(EdmPath.PATH_ROOT + "/" + entitySetSegment.Identifier));
        context.SetOutputKind(OutputKind.Collection);
        context.SetOutputType((EdmEntityType)entitySetSegment.EdmType.AsElementType());
        context.SetOutputPath(alias);
    }

    public void VisitNavigationPropertySegment(CompilerContext context, Microsoft.OData.UriParser.NavigationPropertySegment navigationPropertySegment) 
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

    public void VisitKeySegment(CompilerContext context, Microsoft.OData.UriParser.KeySegment keySegment) 
    {
        var k = (keySegment.NavigationSource.Type.AsElementType() as EdmEntityType)!.GetEntityKey();
        BinaryFilter filter = new BinaryFilter() {
            PropertyReference = new PropertyReference() {
                ResourcePath = pathFactory.CreateResourcePath(keySegment.NavigationSource.Path.PathSegments.ToArray()),
                Property = k
            },
            OperatorKind= BinaryOperatorKind.Equal,
            Value = keySegment.Keys.First().Value
        };
        context.AddFilter(filter);
        context.SetOutputKind(OutputKind.Object);
    }

    public void VisitPropertySegment(CompilerContext context, Microsoft.OData.UriParser.PropertySegment propertySegment) 
    {
        context.AddSelect(context.GetOutputPath() , (EdmStructuralProperty)propertySegment.Property);
        context.SetOutputKind(OutputKind.Property);
    }

    public void VisitCountSegment(CompilerContext context, Microsoft.OData.UriParser.CountSegment countSegment) 
    {
        context.AddCount(context.GetOutputPath(), context.GetOutputType()!.GetEntityKey());
        context.SetOutputKind(OutputKind.RawValue);
    }

    public void VisitFilterSegment(CompilerContext context, Microsoft.OData.UriParser.FilterSegment filterSegment)
    {
        
        context.WrapQuery(context.GetOutputPath());
        VisitExpression(context, filterSegment.Expression, filterSegment.RangeVariable);
    }

    public void VisitFilterClause(CompilerContext context, FilterClause filterClause){
        if (filterClause != null) {
            VisitExpression(context, filterClause.Expression, filterClause.RangeVariable);
        }
    }


    public ExpressionValue? VisitExpression(CompilerContext context,Microsoft.OData.UriParser.SingleValueNode singleValueNode, Microsoft.OData.UriParser.RangeVariable? variable = null) 
    {
        switch (singleValueNode)
        {
            case Microsoft.OData.UriParser.BinaryOperatorNode node:
                return VisitBinaryOperator(context, node);
            case Microsoft.OData.UriParser.ConstantNode node:
                return VisitConstantNode(context, node);
            case Microsoft.OData.UriParser.SingleValuePropertyAccessNode node:
                return VisitSingleValuePropertyAccessNode(context, node);
            case Microsoft.OData.UriParser.ResourceRangeVariableReferenceNode node:
                return VisitResourceRangeVariableReferenceNode(context, node);
            case Microsoft.OData.UriParser.ConvertNode node:
                return VisitExpression(context, node.Source, variable);
            default:
                throw new NotImplementedException();
        }
    }

    public ExpressionValue? VisitBinaryOperator(CompilerContext context, BinaryOperatorNode node)
    {
        if (node.OperatorKind == BinaryOperatorKind.And) {
            VisitExpression(context, node.Left);
            VisitExpression(context, node.Right);
        } else if (node.OperatorKind == BinaryOperatorKind.Or) {

        } else {
            BinaryFilter filter = new BinaryFilter() {
                PropertyReference = (PropertyReference)VisitExpression(context, node.Left),
                OperatorKind = node.OperatorKind,
                Value = (VisitExpression(context, node.Right) as ConstantValue).Value
            };
            context.AddFilter(filter);
        }
        return new ExpressionValue();
    }

    public ExpressionValue? VisitSingleValuePropertyAccessNode(CompilerContext context, Microsoft.OData.UriParser.SingleValuePropertyAccessNode node) {
        //throw new NotImplementedException();
        return new PropertyReference() {
            Property = (EdmStructuralProperty)node.Property,
            ResourcePath = (VisitExpression(context, node.Source) as Variable).ResourcePath
        };
    }

    public ExpressionValue? VisitConstantNode(CompilerContext context, Microsoft.OData.UriParser.ConstantNode node) {
        return new ConstantValue() { Value = node.Value} ;
    }
    public void VisitConvertNode(CompilerContext context, Microsoft.OData.UriParser.ConvertNode node) {
        throw new NotImplementedException();
    }
    public void VisitLambdaNode(CompilerContext context, Microsoft.OData.UriParser.LambdaNode node) {
        throw new NotImplementedException();
    }
    public void VisitParameterAliasNode(CompilerContext context, Microsoft.OData.UriParser.ParameterAliasNode node) {
        throw new NotImplementedException();
    }
    public void VisitSearchTermNode(CompilerContext context, Microsoft.OData.UriParser.SearchTermNode node) {
        throw new NotImplementedException();
    }
    public void VisitSingleEntityNode(CompilerContext context, Microsoft.OData.UriParser.SingleEntityNode node) {
        throw new NotImplementedException();
    }
    public void VisitSingleValueCastNode(CompilerContext context, Microsoft.OData.UriParser.SingleValueCastNode node) {
        throw new NotImplementedException();
    }
    public void VisitSingleValueFunctionCallNode(CompilerContext context, Microsoft.OData.UriParser.SingleValueFunctionCallNode node) {
        throw new NotImplementedException();
    }
    public void VisitSingleValueOpenPropertyAccessNode(CompilerContext context, Microsoft.OData.UriParser.SingleValueOpenPropertyAccessNode node) {
        throw new NotImplementedException();
    }

    public void VisitUnaryOperatorNode(CompilerContext context, Microsoft.OData.UriParser.UnaryOperatorNode node) {
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
        return new Variable() {
            ResourcePath = pathFactory.CreateResourcePath(node.RangeVariable.NavigationSource.Path.PathSegments.ToArray()),
            Type = (EdmEntityType)node.RangeVariable.StructuredTypeReference.Definition.AsElementType(),
            Name = node.RangeVariable.Name
        };
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
    public EdmResourcePath ResourcePath {get; set;} = null!;

    public EdmStructuralProperty Property {get; set;} = null!;
}

public class Variable : ExpressionValue
{
    public EdmResourcePath ResourcePath {get; set;} = null!;

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