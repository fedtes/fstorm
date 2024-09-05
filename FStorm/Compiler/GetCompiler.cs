using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace FStorm
{
    public class GetCompiler
    {
        private readonly FStormService fStormService;
        private readonly Compiler compiler;
        public GetCompiler(FStormService fStormService, Compiler compiler)
        {
            this.fStormService = fStormService;
            this.compiler = compiler;
        }

        public CompilerContext Compile(CompilerContext context, GetRequest getRequest)
        {
            Uri resourceUri = new Uri(fStormService.ServiceRoot, getRequest.ResourcePath);
            ODataUriParser parser = new ODataUriParser(fStormService.Model, fStormService.ServiceRoot, resourceUri);
            ODataPath path = parser.ParsePath();
            compiler.AddPath(context, path);
            //Add entity resource key
            compiler.AddSelectProperty(context, context.Resource.ResourcePath, (EdmStructuralProperty)context.Resource.ResourceEdmType.Key().First(), ":key");
            //Add output columns if not already specified
            if (context.Resource.OutputType == OutputType.Object || context.Resource.OutputType == OutputType.Collection)
            {
                foreach (EdmStructuralProperty p in (context.Resource.ResourceEdmType.AsElementType() as EdmEntityType)!.StructuralProperties().Cast<EdmStructuralProperty>())
                {
                    compiler.AddSelectProperty(context, context.Resource.ResourcePath, p);
                }
            }
            return context;
        }
    }


}
