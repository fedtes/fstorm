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
            context.Output.ResourcePath += navigationProperty.Name;
            context.Aliases.AddOrGet(context.Output.ResourcePath);

            TableOrQuery config = new TableOrQuery()
            {
                Alias = context.Output.ResourcePath,
                IsTable = true,
                IsJoin = true,
                NavProperty = navigationProperty
            };

            compiler.AddTableOrSubQuery(context, config);
            return context;
        }
    }


}
