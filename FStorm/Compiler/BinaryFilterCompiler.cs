namespace FStorm
{

    public class BinaryFilterCompiler
    {
        public CompilerContext Compile(CompilerContext context, EdmPath path, EdmStructuralProperty property, string op, object? value)
        {
            if (!context.Aliases.Contains(path))
            {
                // add missing join
            }

            // vary 'where' method on BinaryFilter.op
            context.Query.Where($"{path.ToString()}.{property.columnName}", value);
            return context;
        }
    }


}
