namespace FStorm
{
    public class SelectPropertyCompiler
    {
       
        public CompilerContext Compile(CompilerContext context, EdmPath path, EdmStructuralProperty property, string overridedName = "")
        {
            if (!context.Aliases.Contains(path))
            {
                // add missing join
            }
            context.Query.Select($"{path!}." + property.columnName + $" as {path! + (!String.IsNullOrEmpty(overridedName) ? overridedName : property.Name)}");
            return context;
        }
    }


}
