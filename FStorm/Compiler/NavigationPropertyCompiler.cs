using Microsoft.OData.Edm;

namespace FStorm
{
    public class NavigationPropertyCompiler : Compiler<EdmNavigationProperty>
    {
        private readonly TableOrSubQueryCompiler tableOrSubQueryCompiler;

        public NavigationPropertyCompiler(FStormService fStormService, TableOrSubQueryCompiler tableOrSubQueryCompiler) : base(fStormService)
        {
            this.tableOrSubQueryCompiler = tableOrSubQueryCompiler;
        }

        public override CompilerContext<EdmNavigationProperty> Compile(CompilerContext<EdmNavigationProperty> context)
        {
            context.Resource.ResourcePath += context.ContextData.Name;
            context.Aliases.Add(context.Resource.ResourcePath);

            TableOrQuery ctx = new TableOrQuery()
            {
                Alias = context.Resource.ResourcePath,
                IsTable = true,
                IsJoin = true,
                NavProperty = context.ContextData
            };

            tableOrSubQueryCompiler.Compile(context.CloneTo(ctx))
                .CopyTo(context);

            return context;
        }
    }


}
