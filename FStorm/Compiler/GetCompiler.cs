using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace FStorm
{
    public class GetCompiler : Compiler<GetRequest>
    {
        private readonly PathCompiler pathCompiler;
        private readonly SelectPropertyCompiler selectPropertyCompiler;

        public GetCompiler(FStormService fStormService, PathCompiler pathCompiler, SelectPropertyCompiler selectPropertyCompiler) : base(fStormService)
        {
            this.pathCompiler = pathCompiler;
            this.selectPropertyCompiler = selectPropertyCompiler;
        }

        public override CompilerContext<GetRequest> Compile(CompilerContext<GetRequest> context)
        {
            Uri resourceUri = new Uri(fStormService.ServiceRoot, context.ContextData.ResourcePath);
            ODataUriParser parser = new ODataUriParser(fStormService.Model, fStormService.ServiceRoot, resourceUri);
            ODataPath path = parser.ParsePath();
            pathCompiler.Compile(context.CloneTo(path)).CopyTo(context);

            //Add entity resource key
            ReferenceToProperty property = new ReferenceToProperty()
            {
                path= context.Resource.ResourcePath,
                overridedName=":key",
                property = (EdmStructuralProperty)context.Resource.ResourceEdmType.Key().First()
            };

            selectPropertyCompiler.Compile(context.CloneTo(property)).CopyTo(context);

            //Add output columns if not already specified
            if (context.Resource.OutputType == OutputType.Object || context.Resource.OutputType == OutputType.Collection)
            {
                foreach (EdmStructuralProperty p in (context.Resource.ResourceEdmType.AsElementType() as EdmEntityType)!.StructuralProperties().Cast<EdmStructuralProperty>())
                {
                    selectPropertyCompiler
                        .Compile(context.CloneTo(new ReferenceToProperty() { property = p, path = context.Resource.ResourcePath }))
                        .CopyTo(context);
                }
            }

            


            return context;
        }
    }


}
