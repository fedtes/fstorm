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
            context.Output.ResourcePath += edmEntityType.Name;
            context.Aliases.AddOrGet(context.Output.ResourcePath);
            compiler.AddTableOrSubQuery(context, new TableOrQuery { Alias = context.Output.ResourcePath, IsTable = true, Type = edmEntityType });
            return context;
        }
    }


}
