

using Microsoft.OData.UriParser;

namespace FStorm
{
    public abstract class Command
    {
        public readonly string CommandId;

        internal Connection? connection;
        internal Transaction? transaction;
        protected readonly IServiceProvider serviceProvider;
        protected readonly FStormService fsService;
        protected readonly SemanticVisitor visitor;
        protected readonly EdmPathFactory pathFactory;

        internal GetRequest Configuration { get; set; } = null!;

        public Command(IServiceProvider serviceProvider, FStormService fStormService, SemanticVisitor visitor, EdmPathFactory pathFactory) 
        {
            CommandId = Guid.NewGuid().ToString();
            this.serviceProvider = serviceProvider;
            this.fsService = fStormService;
            this.visitor = visitor;
            this.pathFactory = pathFactory;
        }

        public SQLCompiledQuery ToSQL()
        {
            ODataUriParser parser = new ODataUriParser(fsService.Model, fsService.ServiceRoot, new Uri(Configuration.RequestPath, UriKind.Relative));
            var context = new CompilerContext(fsService, parser.ParsePath(), parser.ParseFilter(), parser.ParseSelectAndExpand(), parser.ParseOrderBy(), new PaginationClause(parser.ParseTop(), parser.ParseSkip()));
            visitor.VisitContext(context);
            return Compile(context);
        }

        protected virtual SQLCompiledQuery Compile(CompilerContext context) 
        {
            return context.GetQuery();
        }
    
        public abstract Task<CommandResult<CompilerContext>> ToListAsync();
    
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