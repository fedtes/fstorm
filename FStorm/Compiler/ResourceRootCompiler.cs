namespace FStorm
{
    public class ResourceRootCompiler : Compiler<EdmEntityType>
    {
        public ResourceRootCompiler(FStormService fStormService) : base(fStormService)
        {
        }

        public override CompilerContext<EdmEntityType> Compile(CompilerContext<EdmEntityType> context)
        {
            context.Resource.ResourcePath += context.ContextData.Name;
            context.Aliases.Add(context.Resource.ResourcePath);
            context.Query.From(context.ContextData.Table + $" as {context.Resource.ResourcePath.ToString()}");
            return context;
        }
    }


}
