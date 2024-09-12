using System;
using Microsoft.OData.Edm;

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
        context.SetOutputKind(OutputType.Collection);
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

        context.SetOutputKind(navigationPropertySegment.EdmType.TypeKind == EdmTypeKind.Collection ? OutputType.Collection : OutputType.Object);
        context.SetOutputType((EdmEntityType)navigationPropertySegment.EdmType.AsElementType());
        context.SetOutputPath(alias);
    }

    public void VisitKeySegment(CompilerContext context, Microsoft.OData.UriParser.KeySegment keySegment) 
    {
        var k = (keySegment.NavigationSource.Type.AsElementType() as EdmEntityType)!.GetEntityKey();
        context.AddWhere(pathFactory.CreateResourcePath(keySegment.NavigationSource.Path.PathSegments.ToArray()), k, keySegment.Keys.First().Value);
        context.SetOutputKind(OutputType.Object);
    }

    public void VisitPropertySegment(CompilerContext context, Microsoft.OData.UriParser.PropertySegment propertySegment) 
    {
        context.AddSelect(context.GetOutputPath() , (EdmStructuralProperty)propertySegment.Property);
        context.SetOutputKind(OutputType.Property);
    }

    public void VisitCountSegment(CompilerContext context, Microsoft.OData.UriParser.CountSegment countSegment) 
    {
        context.AddCount(context.GetOutputPath(), context.GetOutputType()!.GetEntityKey());
        context.SetOutputKind(OutputType.RawValue);
    }

    public void VisitFilterSegment(CompilerContext context, Microsoft.OData.UriParser.FilterSegment filterSegment)
    {
        
        VisitExpression(context, filterSegment.RangeVariable, filterSegment.Expression);
        context.WrapQuery(context.GetOutputPath());
    }


    public void VisitExpression(CompilerContext context,Microsoft.OData.UriParser.RangeVariable variable ,Microsoft.OData.UriParser.SingleValueNode singleValueNode) 
    {
        throw new NotImplementedException();
    }
    /* 
    
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
