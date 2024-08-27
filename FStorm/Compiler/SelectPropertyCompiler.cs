namespace FStorm
{
    public class ReferenceToProperty
    {
        /// <summary>
        /// Path to the entity containing this property (property segment is excluded)
        /// </summary>
        public EdmPath? path;
        public EdmStructuralProperty? property;
    }

    public class SelectPropertyCompiler : Compiler<ReferenceToProperty>
    {
        public SelectPropertyCompiler(FStormService fStormService) : base(fStormService) { }

        public override CompilerContext<ReferenceToProperty> Compile(CompilerContext<ReferenceToProperty> context)
        {

            if (!context.Aliases.Contains(context.ContextData.path!))
            {
                // add missing join
            }
            var p = context.ContextData.property!;
            context.Query.Select($"{context.ContextData.path!}." + p.columnName + $" as {context.ContextData.path! + p.Name}");
            return context;
        }
    }


}
