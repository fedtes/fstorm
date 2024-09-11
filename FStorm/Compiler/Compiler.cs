using FStorm;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace FStorm
{
    public class Compiler
    {
        protected readonly FStormService fStormService;
        private readonly BinaryFilterCompiler BinaryFilterCompiler;
        private readonly GetCompiler GetCompiler;
        private readonly NavigationPropertyCompiler NavigationPropertyCompiler;
        private readonly PathCompiler PathCompiler;
        private readonly ResourceRootCompiler ResourceRootCompiler;
        private readonly SelectPropertyCompiler SelectPropertyCompiler;
        private readonly TableOrSubQueryCompiler TableOrSubQueryCompiler;
        private readonly CountCompiler CountCompiler;

        public Compiler(FStormService fStormService,EdmPathFactory pathFactory)
        {
            this.fStormService = fStormService;
            this.BinaryFilterCompiler = new BinaryFilterCompiler();
            this.GetCompiler = new GetCompiler(fStormService, this);
            this.NavigationPropertyCompiler = new NavigationPropertyCompiler(this);
            this.PathCompiler = new PathCompiler(this, pathFactory);
            this.ResourceRootCompiler = new ResourceRootCompiler(this);
            this.SelectPropertyCompiler = new SelectPropertyCompiler();
            this.TableOrSubQueryCompiler = new TableOrSubQueryCompiler();
            this.CountCompiler = new CountCompiler();
        }

        public CompilerContext AddBinaryFilter(CompilerContext context, EdmPath path, EdmStructuralProperty property, string op, object? value) => BinaryFilterCompiler.Compile(context,path, property, op, value);

        public CompilerContext AddGet(CompilerContext context, GetRequest getRequest) => GetCompiler.Compile(context, getRequest);

        public CompilerContext AddNavigationProperty(CompilerContext context, EdmNavigationProperty navigationProperty) => NavigationPropertyCompiler.Compile(context, navigationProperty);

        public CompilerContext AddPath(CompilerContext context, ODataPath oDataPath) => PathCompiler.Compile(context, oDataPath);

        public CompilerContext AddResourceRoot(CompilerContext context, EdmEntityType edmEntityType) => ResourceRootCompiler.Compile(context, edmEntityType);

        public CompilerContext AddSelectProperty(CompilerContext context, EdmPath path, EdmStructuralProperty property, string overridedName = "") => SelectPropertyCompiler.Compile(context, path, property, overridedName);

        public CompilerContext AddTableOrSubQuery(CompilerContext context, TableOrQuery tableOrQuery) => TableOrSubQueryCompiler.Compile(context, tableOrQuery);

        public CompilerContext AddCount(CompilerContext context) => CountCompiler.Compile(context);
    }
}


public class CountCompiler
{
    public CompilerContext Compile(CompilerContext context) {
        context.Query.AsCount(new[] {context.Output.ResourceEdmType!.GetEntityKey().columnName});
        return context;
    }
}