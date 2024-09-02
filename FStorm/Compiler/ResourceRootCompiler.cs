namespace FStorm
{
    public class ResourceRootCompiler : Compiler<EdmEntityType>
    {
        private readonly TableOrSubQueryCompiler tableOrSubQueryCompiler;

        public ResourceRootCompiler(FStormService fStormService, TableOrSubQueryCompiler tableOrSubQueryCompiler) : base(fStormService)
        {
            this.tableOrSubQueryCompiler = tableOrSubQueryCompiler;
        }

        public override CompilerContext<EdmEntityType> Compile(CompilerContext<EdmEntityType> context)
        {
            context.Resource.ResourcePath += context.ContextData.Name;
            context.Aliases.Add(context.Resource.ResourcePath);
            tableOrSubQueryCompiler.Compile(context.CloneTo(new TableOrQuery { Alias = context.Resource.ResourcePath, IsTable = true, Type = context.ContextData }))
                .CopyTo(context);
            return context;
        }
    }


}
