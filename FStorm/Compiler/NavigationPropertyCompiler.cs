using Microsoft.OData.Edm;

namespace FStorm
{
    public class NavigationPropertyCompiler
    {
        private readonly Compiler compiler;

        public NavigationPropertyCompiler(Compiler compiler) 
        {
            this.compiler = compiler;
        }

        public CompilerContext Compile(CompilerContext context, EdmNavigationProperty navigationProperty)
        {
            context.Resource.ResourcePath += navigationProperty.Name;
            context.Aliases.Add(context.Resource.ResourcePath);

            TableOrQuery config = new TableOrQuery()
            {
                Alias = context.Resource.ResourcePath,
                IsTable = true,
                IsJoin = true,
                NavProperty = navigationProperty
            };

            compiler.AddTableOrSubQuery(context, config);
            return context;
        }
    }


}
