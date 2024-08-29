using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace FStorm
{
    public class PathCompiler : Compiler<ODataPath>
    {
        protected readonly EdmPathFactory pathFactory;
        private readonly ResourceRootCompiler fromCompiler;
        private readonly NavigationPropertyCompiler navigationPropertyCompiler;
        private readonly SelectPropertyCompiler selectPropertyCompiler;
        private readonly BinaryFilterCompiler binaryFilterCompiler;

        public PathCompiler(
            FStormService fStormService,
            EdmPathFactory pathFactory,
            ResourceRootCompiler fromCompiler,
            NavigationPropertyCompiler navigationPropertyCompiler,
            SelectPropertyCompiler selectPropertyCompiler,
            BinaryFilterCompiler binaryFilterCompiler) : base(fStormService)
        {
            this.pathFactory = pathFactory;
            this.fromCompiler = fromCompiler;
            this.navigationPropertyCompiler = navigationPropertyCompiler;
            this.selectPropertyCompiler = selectPropertyCompiler;
            this.binaryFilterCompiler = binaryFilterCompiler;
        }

        public override CompilerContext<ODataPath> Compile(CompilerContext<ODataPath> context)
        {
            bool isRoot = true;
            context.Resource.ResourcePath = pathFactory.CreateResourcePath();

            foreach (var segment in context.ContextData)
            {
                switch (segment)
                {
                    case EntitySetSegment collection:
                        {
                            if (!isRoot)
                                throw new NotImplementedException("Should not pass here");
                            var _edmType = (collection.EdmType.AsElementType() as EdmEntityType)!;
                            fromCompiler
                                .Compile(context.CloneTo(_edmType)!)
                                .CopyTo(context);
                            context.Resource.ResourceType= ResourceType.Collection;
                            context.Resource.ResourceEdmType = _edmType;
                            break;
                        }
                    case KeySegment single:
                        {
                            var key = single.Keys.First();
                            EdmStructuralProperty keyProperty = (EdmStructuralProperty)((EdmEntityType)single.EdmType).DeclaredKey.First(x => x.Name == key.Key);
                            binaryFilterCompiler
                                .Compile(context.CloneTo(new BinaryFilter() { path = context.Resource.ResourcePath, property = keyProperty, value = key.Value }))
                                .CopyTo(context);
                            context.Resource.ResourceType = ResourceType.Object;
                            break;
                        }
                    case PropertySegment property:
                        {
                            selectPropertyCompiler
                                .Compile(context.CloneTo(new ReferenceToProperty() { path = context.Resource.ResourcePath, property = (EdmStructuralProperty)property.Property }))
                                .CopyTo(context);
                            context.Resource.ResourceType = ResourceType.Property;
                            break;
                        }
                    case NavigationPropertySegment navigationProperty when navigationProperty.EdmType.TypeKind == EdmTypeKind.Collection:
                        {
                            navigationPropertyCompiler
                                .Compile(context.CloneTo(navigationProperty.NavigationProperty as EdmNavigationProperty)!)
                                .CopyTo(context);

                            var _edmType = (navigationProperty.NavigationProperty.Type.Definition.AsElementType() as EdmEntityType)!;
                            context.Resource.ResourceEdmType = _edmType;
                            context.Resource.ResourceType = ResourceType.Collection;
                            break;
                        }
                    case NavigationPropertySegment navigationProperty when navigationProperty.EdmType.TypeKind == EdmTypeKind.Entity:
                        {
                            navigationPropertyCompiler
                                .Compile(context.CloneTo(navigationProperty.NavigationProperty as EdmNavigationProperty)!)
                                .CopyTo(context);

                            var _edmType = (navigationProperty.NavigationProperty.Type.Definition.AsElementType() as EdmEntityType)!;
                            context.Resource.ResourceType = ResourceType.Object;
                            context.Resource.ResourceEdmType = _edmType;
                            break;
                        }
                    default:
                        throw new NotImplementedException("Should not pass here");
                }
                isRoot = false;
            }

            if (context.Resource.ResourceType != ResourceType.Property)
                context.Resource.ResourceEdmType=(EdmEntityType)context.ContextData.LastSegment.EdmType.AsElementType();
            
            return context;
        }
    }


}
