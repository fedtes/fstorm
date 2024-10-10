

namespace FStorm
{
    public abstract class Command
    {

        public readonly string CommandId;

        internal Connection? connection;
        internal Transaction? transaction;

        protected readonly IServiceProvider serviceProvider;
        protected readonly FStormService fsService;

        public Command(IServiceProvider serviceProvider, FStormService fStormService) 
        {
            CommandId = Guid.NewGuid().ToString();
            this.serviceProvider = serviceProvider;
            this.fsService = fStormService;
        }

        public abstract SQLCompiledQuery ToSQL();

        protected virtual SQLCompiledQuery Compile(CompilerContext context) 
        {
            return context.GetQuery();
        }
    }

    public class SQLCompiledQuery
    {
        public CompilerContext Context { get; }
        public string Statement { get; }
        public Dictionary<string, object> Bindings { get; }
        public SQLCompiledQuery(CompilerContext context, string statement, Dictionary<string, object> bindings)
        {
            Context = context;
            Statement = statement;
            Bindings = bindings;
        }
    }



}