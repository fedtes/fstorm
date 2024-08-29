using Microsoft.OData.Edm;

namespace FStorm
{
    public class TableOrSubQueryCompiler : Compiler<TableOrQuery>
    {
        private readonly SelectPropertyCompiler select;

        public TableOrSubQueryCompiler(FStormService fStormService, SelectPropertyCompiler select) : base(fStormService)
        {
            this.select = select;
        }

        public override CompilerContext<TableOrQuery> Compile(CompilerContext<TableOrQuery> context)
        {
            if (context.ContextData.IsTable)
            {
                context.Query.From(context.ContextData.Type!.Table + $" as {context.ContextData.Alias.ToString()}");

                ReferenceToProperty property = new ReferenceToProperty()
                {
                    path= context.ContextData.Alias + ":key",
                    property = (EdmStructuralProperty)context.ContextData.Type.Key().First()
                };

                select.Compile(context.CloneTo(property))
                    .CopyTo(context);
            } else
            { 
                throw new NotImplementedException();
            }

            return context;
        }
    }
}
