using Microsoft.OData.UriParser;

namespace FStorm
{
    public class Command
    {
        public readonly string CommandId;

        internal Connection? connection;
        internal Transaction? transaction;
        protected readonly FStormService fsService;
        protected readonly SemanticVisitor visitor;
        private readonly IQueryExecutor executor;

        private CompilerContext context = null!;

        internal GetRequest Configuration { get; set; } = null!;

        public Command(FStormService fStormService, SemanticVisitor visitor, IQueryExecutor executor) 
        {
            CommandId = Guid.NewGuid().ToString();
            this.fsService = fStormService;
            this.visitor = visitor;
            this.executor = executor;
        }

        public SQLCompiledQuery ToSQL()
        {
            CreateContext();
            return Compile(context);
        }

        private void CreateContext()
        {
            ODataUriParser parser = new ODataUriParser(fsService.Model, fsService.ServiceRoot, new Uri(Configuration.RequestPath, UriKind.Relative));
            context = new CompilerContext(fsService, parser.ParsePath(), parser.ParseFilter(), parser.ParseSelectAndExpand(), parser.ParseOrderBy(), new PaginationClause(parser.ParseTop(), parser.ParseSkip()));
            visitor.VisitContext(context);
        }

        protected virtual SQLCompiledQuery Compile(CompilerContext context) =>  context.Compile();

        public Task<IEnumerable<IDictionary<string, object>>> ToListAsync()
        {
            if (connection == null || this.transaction == null)
            {
                throw new ArgumentNullException("Either connection or transaction are null. Cannot execute query.");
            }
            CreateContext();
            return this.executor.Execute(this.connection, this.context);
        }
    
    }

}