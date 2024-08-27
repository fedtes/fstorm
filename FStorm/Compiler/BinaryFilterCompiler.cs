namespace FStorm
{
    public class BinaryFilter
    {
        /// <summary>
        /// Path to the entity containing this property (property segment is excluded)
        /// </summary>
        public EdmPath? path;
        public EdmStructuralProperty? property;
        public string op;
        public object? value;
    }

    public class BinaryFilterCompiler : Compiler<BinaryFilter>
    {
        public BinaryFilterCompiler(FStormService fStormService) : base(fStormService) { }

        public override CompilerContext<BinaryFilter> Compile(CompilerContext<BinaryFilter> context)
        {
            if (!context.Aliases.Contains(context.ContextData.path!))
            {
                // add missing join
            }

            // vary 'where' method on BinaryFilter.op
            context.Query.Where($"{context.ContextData.path!.ToString()}.{context.ContextData.property!.columnName}", context.ContextData.value);
            return context;
        }
    }


}
