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
            context.Output.ResourcePath = pathFactory.CreateResourcePath();

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
                            context.Output.OutputType= OutputType.Collection;
                            context.Output.ResourceEdmType = _edmType;
                            break;
                        }
                    case KeySegment single:
                        {
                            var key = single.Keys.First();
                            EdmStructuralProperty keyProperty = (EdmStructuralProperty)((EdmEntityType)single.EdmType).DeclaredKey.First(x => x.Name == key.Key);
                            compiler.AddBinaryFilter(context, context.Output.ResourcePath, keyProperty, "eq",key.Value);
                            context.Output.OutputType = OutputType.Object;
                            break;
                        }
                    case PropertySegment property:
                        {
                            compiler.AddSelectProperty(context,context.Output.ResourcePath,(EdmStructuralProperty)property.Property);
                            context.Output.OutputType = OutputType.Property;
                            break;
                        }
                    case NavigationPropertySegment navigationProperty when navigationProperty.EdmType.TypeKind == EdmTypeKind.Collection:
                        {
                            compiler.AddNavigationProperty(context, (EdmNavigationProperty)navigationProperty.NavigationProperty);
                            var _edmType = (navigationProperty.NavigationProperty.Type.Definition.AsElementType() as EdmEntityType)!;
                            context.Output.ResourceEdmType = _edmType;
                            context.Output.OutputType = OutputType.Collection;
                            break;
                        }
                    case NavigationPropertySegment navigationProperty when navigationProperty.EdmType.TypeKind == EdmTypeKind.Entity:
                        {
                            compiler.AddNavigationProperty(context, (EdmNavigationProperty)navigationProperty.NavigationProperty);
                            var _edmType = (navigationProperty.NavigationProperty.Type.Definition.AsElementType() as EdmEntityType)!;
                            context.Output.OutputType = OutputType.Object;
                            context.Output.ResourceEdmType = _edmType;
                            break;
                        }
                    case CountSegment count:
                    {
                        compiler.AddSelectProperty(context,context.Output.ResourcePath, context.Output.ResourceEdmType!.GetEntityKey());
                        compiler.AddCount(context);
                        context.Output.OutputType = OutputType.RawValue;
                        break;
                    }
                    default:
                        throw new NotImplementedException("Should not pass here");
                }
                isRoot = false;
            }

            if (!new[] {OutputType.Property,OutputType.RawValue}.Contains(context.Output.OutputType))
                context.Output.ResourceEdmType=(EdmEntityType)oDataPath.LastSegment.EdmType.AsElementType();
            
            return context;
        }
    }


}
