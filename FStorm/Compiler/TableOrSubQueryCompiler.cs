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

    public class TableOrSubQueryCompiler : Compiler<TableOrQuery>
    {
        private readonly SelectPropertyCompiler select;

        public TableOrSubQueryCompiler(FStormService fStormService, SelectPropertyCompiler select) : base(fStormService)
        {
            this.select = select;
        }

        public override CompilerContext<TableOrQuery> Compile(CompilerContext<TableOrQuery> context)
        {
            if (context.ContextData.IsTable)
            {
                if (!context.ContextData.IsJoin)
                {
                    context.Query.From(context.ContextData.Type!.Table + $" as {context.ContextData.Alias.ToString()}");
                    // ReferenceToProperty property = new ReferenceToProperty()
                    // {
                    //     path= context.ContextData.Alias,
                    //     overridedName=":key",
                    //     property = (EdmStructuralProperty)context.ContextData.Type.Key().First()
                    // };

                    // select.Compile(context.CloneTo(property))
                    //     .CopyTo(context);
                }
                else
                {
                    var type = (context.ContextData.NavProperty!.Type.Definition.AsElementType() as EdmEntityType)!;
                    var constraint = context.ContextData.NavProperty.ReferentialConstraint.PropertyPairs.First();
                    var sourceProperty = (EdmStructuralProperty)constraint.PrincipalProperty;
                    var targetProperty = (EdmStructuralProperty)constraint.DependentProperty;
                    context.Query.Join(type.Table + $" as {context.Resource.ResourcePath}", $"{context.Resource.ResourcePath - 1}.{sourceProperty.columnName}", $"{context.Resource.ResourcePath}.{targetProperty.columnName}");
                    // ReferenceToProperty property = new ReferenceToProperty()
                    // {
                    //     path= context.ContextData.Alias,
                    //     overridedName=":key",
                    //     property = (EdmStructuralProperty)type.Key().First()
                    // };

                    // select.Compile(context.CloneTo(property))
                    //     .CopyTo(context);
                }

                
            } else
            { 
                throw new NotImplementedException();
            }

            return context;
        }
    }
}
