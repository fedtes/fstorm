using System.Text;
using Microsoft.OData.UriParser;

namespace FStorm
{
    public class Command
    {
        public readonly string CommandId;

        internal Connection connection = null!;
        internal Transaction transaction = null!;
        internal ICompilerContext context = null!;
        protected readonly ODataService service;
        protected readonly SemanticVisitor visitor;
        private readonly IQueryExecutor executor;
        private readonly Writer writer;
        private readonly CompilerContextFactory contextFactory;
        private readonly DeltaTokenService deltaTokenService;

        /// <summary>
        /// Default command execution timeout
        /// </summary>
        internal uint CommandTimeout { get; set; } =  30;

        /// <summary>
        /// Default $top value if not speficied in the request
        /// </summary>
        internal uint DefaultTopRequest {get; set;} = 100;

        /// <summary>
        /// If true ignore the <see cref="DefaultTopRequest"/> parameters. If $top is specified in the request then it is NOT ignored. 
        /// </summary>
        internal bool BypassDefaultTopRequest {get; set;} = false;

        internal string UriRequest { get; set; } = null!;

        public Command(ODataService fStormService, SemanticVisitor visitor, IQueryExecutor executor, Writer writer, CompilerContextFactory contextFactory, DeltaTokenService deltaTokenService) 
        {
            CommandId = Guid.NewGuid().ToString();
            this.service = fStormService;
            this.visitor = visitor;
            this.executor = executor;
            this.writer = writer;
            this.contextFactory = contextFactory;
            this.deltaTokenService = deltaTokenService;
        }

        public SQLCompiledQuery ToSQL()
        {
            CreateContext();
            return Compile(context);
        }

        private void CreateContext()
        {
            context = contextFactory.CreateContext(UriRequest);
            if (!String.IsNullOrEmpty(context.GetSkipToken()))
            {
                var nextUriRequest = deltaTokenService.DecodeSkipToken(context);
                context = contextFactory.CreateContext(nextUriRequest);
            }
            visitor.VisitContext(context);
        }

        protected virtual SQLCompiledQuery Compile(ICompilerContext context) =>  context.Compile();

        public Task<IEnumerable<IDictionary<string, object?>>> ToListAsync()
        {
            if (connection == null || this.transaction == null)
            {
                throw new ArgumentNullException("Either connection or transaction are null. Cannot execute query.");
            }
            CreateContext();
            return this.executor.Execute(this);
        }

        public async Task<string> ToODataString() 
        {   using (var s = new MemoryStream())
            {
                await ToODataResponse(s);
                StreamReader reader = new StreamReader(s);
                return reader.ReadToEnd();
            }
        }
        public async Task ToODataResponse(Stream s) 
        {   
            var data = await ToListAsync();
            writer.WriteResult(context, data, s);
        }
    
    }

}