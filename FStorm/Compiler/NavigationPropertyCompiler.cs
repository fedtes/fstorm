using Microsoft.OData.Edm;

namespace FStorm
{
    public class NavigationPropertyCompiler : Compiler<EdmNavigationProperty>
    {
        public NavigationPropertyCompiler(FStormService fStormService) : base(fStormService) { }

        public override CompilerContext<EdmNavigationProperty> Compile(CompilerContext<EdmNavigationProperty> context)
        {
            context.Resource.ResourcePath += context.ContextData.Name;
            context.Aliases.Add(context.Resource.ResourcePath);
            var type = (context.ContextData.Type.Definition.AsElementType() as EdmEntityType)!;
            var constraint = context.ContextData.ReferentialConstraint.PropertyPairs.First();
            var sourceProperty = (EdmStructuralProperty)constraint.PrincipalProperty;
            var targetProperty = (EdmStructuralProperty)constraint.DependentProperty;
            context.Query.Join(type.Table + $" as {context.Resource.ResourcePath}", $"{context.Resource.ResourcePath - 1}.{sourceProperty.columnName}", $"{context.Resource.ResourcePath}.{targetProperty.columnName}");
            return context;
        }
    }


}
