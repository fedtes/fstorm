using Microsoft.OData.Edm;

namespace FStorm
{

    public class TableOrQuery
    {
        public bool IsTable = true;
        public EdmPath Alias = null!;
        public EdmEntityType? Type;
        public EdmNavigationProperty? NavProperty;
        public bool IsJoin=false;
    }

    public class TableOrSubQueryCompiler
    {
        public CompilerContext Compile(CompilerContext context, TableOrQuery tableOrQuery)
        {
            if (tableOrQuery.IsTable)
            {
                if (!tableOrQuery.IsJoin)
                {
                    context.Query.From(tableOrQuery.Type!.Table + $" as {tableOrQuery.Alias.ToString()}");
                }
                else
                {
                    var type = (tableOrQuery.NavProperty!.Type.Definition.AsElementType() as EdmEntityType)!;
                    var constraint = tableOrQuery.NavProperty.ReferentialConstraint.PropertyPairs.First();
                    var sourceProperty = (EdmStructuralProperty)constraint.PrincipalProperty;
                    var targetProperty = (EdmStructuralProperty)constraint.DependentProperty;
                    context.Query.Join(type.Table + $" as {context.Resource.ResourcePath}", $"{context.Resource.ResourcePath - 1}.{sourceProperty.columnName}", $"{context.Resource.ResourcePath}.{targetProperty.columnName}");
                }
            } 
            else
            { 
                throw new NotImplementedException();
            }

            return context;
        }
    }
}
