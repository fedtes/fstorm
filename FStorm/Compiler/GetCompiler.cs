using Microsoft.OData.UriParser;

namespace FStorm
{
    public class GetCompiler : Compiler<GetConfiguration>
    {
        private readonly PathCompiler pathCompiler;

        public GetCompiler(FStormService fStormService, PathCompiler pathCompiler) : base(fStormService)
        {
            this.pathCompiler = pathCompiler;
        }

        public override CompilerContext<GetConfiguration> Compile(CompilerContext<GetConfiguration> context)
        {
            Uri resourceUri = new Uri(fStormService.ServiceRoot, context.ContextData.ResourcePath);
            ODataUriParser parser = new ODataUriParser(fStormService.Model, fStormService.ServiceRoot, resourceUri);
            ODataPath path = parser.ParsePath();
            pathCompiler.Compile(context.CloneTo(path)).CopyTo(context);
            return context;
        }
    }


}
