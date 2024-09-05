namespace FStorm
{
    public class ResourceRootCompiler
    {
        private readonly Compiler compiler;

        public ResourceRootCompiler(Compiler compiler)
        {
            this.compiler = compiler;
        }

        public CompilerContext Compile(CompilerContext context, EdmEntityType edmEntityType)
        {
            context.Resource.ResourcePath += edmEntityType.Name;
            context.Aliases.Add(context.Resource.ResourcePath);
            compiler.AddTableOrSubQuery(context, new TableOrQuery { Alias = context.Resource.ResourcePath, IsTable = true, Type = edmEntityType });
            return context;
        }
    }


}
