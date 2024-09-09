using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace FStorm
{
    public class PathCompiler
    {
        private readonly Compiler compiler;
        protected readonly EdmPathFactory pathFactory;
        
        public PathCompiler(Compiler compiler, EdmPathFactory pathFactory)
        {
            this.compiler = compiler;
            this.pathFactory = pathFactory;
        }

        public CompilerContext Compile(CompilerContext context, ODataPath oDataPath)
        {
            bool isRoot = true;
            context.Resource.ResourcePath = pathFactory.CreateResourcePath();

            foreach (var segment in oDataPath)
            {
                switch (segment)
                {
                    case EntitySetSegment collection:
                        {
                            if (!isRoot)
                                throw new NotImplementedException("Should not pass here");
                            var _edmType = (collection.EdmType.AsElementType() as EdmEntityType)!;
                            compiler.AddResourceRoot(context, _edmType);
                            context.Resource.OutputType= OutputType.Collection;
                            context.Resource.ResourceEdmType = _edmType;
                            break;
                        }
                    case KeySegment single:
                        {
                            var key = single.Keys.First();
                            EdmStructuralProperty keyProperty = (EdmStructuralProperty)((EdmEntityType)single.EdmType).DeclaredKey.First(x => x.Name == key.Key);
                            compiler.AddBinaryFilter(context, context.Resource.ResourcePath, keyProperty, "eq",key.Value);
                            context.Resource.OutputType = OutputType.Object;
                            break;
                        }
                    case PropertySegment property:
                        {
                            compiler.AddSelectProperty(context,context.Resource.ResourcePath,(EdmStructuralProperty)property.Property);
                            context.Resource.OutputType = OutputType.Property;
                            break;
                        }
                    case NavigationPropertySegment navigationProperty when navigationProperty.EdmType.TypeKind == EdmTypeKind.Collection:
                        {
                            compiler.AddNavigationProperty(context, (EdmNavigationProperty)navigationProperty.NavigationProperty);
                            var _edmType = (navigationProperty.NavigationProperty.Type.Definition.AsElementType() as EdmEntityType)!;
                            context.Resource.ResourceEdmType = _edmType;
                            context.Resource.OutputType = OutputType.Collection;
                            break;
                        }
                    case NavigationPropertySegment navigationProperty when navigationProperty.EdmType.TypeKind == EdmTypeKind.Entity:
                        {
                            compiler.AddNavigationProperty(context, (EdmNavigationProperty)navigationProperty.NavigationProperty);
                            var _edmType = (navigationProperty.NavigationProperty.Type.Definition.AsElementType() as EdmEntityType)!;
                            context.Resource.OutputType = OutputType.Object;
                            context.Resource.ResourceEdmType = _edmType;
                            break;
                        }
                    case CountSegment count:
                    {
                        compiler.AddSelectProperty(context,context.Resource.ResourcePath, context.Resource.ResourceEdmType!.GetEntityKey());
                        compiler.AddCount(context);
                        context.Resource.OutputType = OutputType.RawValue;
                        break;
                    }
                    default:
                        throw new NotImplementedException("Should not pass here");
                }
                isRoot = false;
            }

            if (!new[] {OutputType.Property,OutputType.RawValue}.Contains(context.Resource.OutputType))
                context.Resource.ResourceEdmType=(EdmEntityType)oDataPath.LastSegment.EdmType.AsElementType();
            
            return context;
        }
    }


}
