using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using SqlKata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FStorm
{

    public enum ResourceType
    {
        Collection,
        Object,
        Property
    }

    public class CompilerContext<T>
    {
        public SqlKata.Query Query = new SqlKata.Query();
        public List<EdmPath> Aliases = new List<EdmPath>();
        public ResourceType ResourceType;
        public EdmPath ResourcePath;
        public EdmEntityType? ResourceEdmType;
        public T ContextData = default!;

        public CompilerContext<T1> CloneTo<T1>(T1 ContextData)
        {
            CompilerContext<T1> instance = (CompilerContext<T1>)Activator.CreateInstance(typeof(CompilerContext<T1>))!;
            instance = this.CopyTo(instance);
            instance.ContextData = ContextData;
            return instance;
        }

        public CompilerContext<T1> CopyTo<T1>(CompilerContext<T1> newContext)
        {
            newContext.Query = this.Query;
            newContext.Aliases = this.Aliases;
            newContext.ResourceType = this.ResourceType;
            newContext.ResourcePath = this.ResourcePath;
            newContext.ResourceEdmType = this.ResourceEdmType;
            return newContext;
        }
    }


    public abstract class Compiler<T>
    {
        protected readonly FStormService fStormService;

        public Compiler(FStormService fStormService)
        {
            this.fStormService = fStormService;
        }

        public abstract CompilerContext<T> Compile(CompilerContext<T> context);
    }

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
            context.ResourcePath = pathFactory.CreateResourcePath();

            foreach (var segment in context.ContextData)
            {
                switch (segment)
                {
                    case EntitySetSegment collection:
                        {
                            if (!isRoot)
                                throw new NotImplementedException("Should not pass here");
                            
                            fromCompiler
                                .Compile(context.CloneTo(collection.EdmType.AsElementType() as EdmEntityType)!)
                                .CopyTo(context);
                            context.ResourceType= ResourceType.Collection;
                            break;
                        }
                    case KeySegment single:
                        {
                            var key = single.Keys.First();
                            EdmStructuralProperty keyProperty = (EdmStructuralProperty)((EdmEntityType)single.EdmType).DeclaredKey.First(x => x.Name == key.Key);
                            binaryFilterCompiler
                                .Compile(context.CloneTo(new BinaryFilter() { path = context.ResourcePath, property = keyProperty, value = key.Value }))
                                .CopyTo(context);
                            context.ResourceType = ResourceType.Object;
                            break;
                        }
                    case PropertySegment property:
                        {
                            selectPropertyCompiler
                                .Compile(context.CloneTo(new ReferenceToProperty() { path = context.ResourcePath, property = (EdmStructuralProperty)property.Property }))
                                .CopyTo(context);
                            context.ResourceType = ResourceType.Property;
                            break;
                        }
                    case NavigationPropertySegment navigationProperty when navigationProperty.EdmType.TypeKind == EdmTypeKind.Collection:
                        {
                            navigationPropertyCompiler
                                .Compile(context.CloneTo(navigationProperty.NavigationProperty as EdmNavigationProperty)!)
                                .CopyTo(context);
                            context.ResourceType = ResourceType.Collection;
                            break;
                        }
                    case NavigationPropertySegment navigationProperty when navigationProperty.EdmType.TypeKind == EdmTypeKind.Entity:
                        {
                            navigationPropertyCompiler
                                .Compile(context.CloneTo(navigationProperty.NavigationProperty as EdmNavigationProperty)!)
                                .CopyTo(context);
                            context.ResourceType = ResourceType.Object;
                            break;
                        }
                    default:
                        throw new NotImplementedException("Should not pass here");
                }
                isRoot = false;
            }

            if (context.ResourceType != ResourceType.Property)
                context.ResourceEdmType=(EdmEntityType)context.ContextData.LastSegment.EdmType.AsElementType();
            
            return context;
        }
    }

    public class ResourceRootCompiler : Compiler<EdmEntityType>
    {
        public ResourceRootCompiler(FStormService fStormService) : base(fStormService)
        {
        }

        public override CompilerContext<EdmEntityType> Compile(CompilerContext<EdmEntityType> context)
        {
            context.ResourcePath += context.ContextData.Name;
            context.Aliases.Add(context.ResourcePath);
            context.Query.From(context.ContextData.Table + $" as {context.ResourcePath.ToString()}");
            return context;
        }
    }


    public class NavigationPropertyCompiler : Compiler<EdmNavigationProperty>
    {
        public NavigationPropertyCompiler(FStormService fStormService) : base(fStormService) { }

        public override CompilerContext<EdmNavigationProperty> Compile(CompilerContext<EdmNavigationProperty> context)
        {
            context.ResourcePath += context.ContextData.Name;
            context.Aliases.Add(context.ResourcePath);
            var type = (context.ContextData.Type.Definition.AsElementType() as EdmEntityType)!;
            var constraint = context.ContextData.ReferentialConstraint.PropertyPairs.First();
            var sourceProperty = (EdmStructuralProperty)constraint.PrincipalProperty;
            var targetProperty = (EdmStructuralProperty)constraint.DependentProperty;
            context.Query.Join(type.Table + $" as {context.ResourcePath}", $"{context.ResourcePath - 1}.{sourceProperty.columnName}", $"{context.ResourcePath}.{targetProperty.columnName}");
            return context;
        }
    }

    public class ReferenceToProperty
    {
        /// <summary>
        /// Path to the entity containing this property (property segment is excluded)
        /// </summary>
        public EdmPath? path;
        public EdmStructuralProperty? property;
    }

    public class SelectPropertyCompiler : Compiler<ReferenceToProperty>
    {
        public SelectPropertyCompiler(FStormService fStormService) : base(fStormService) { }

        public override CompilerContext<ReferenceToProperty> Compile(CompilerContext<ReferenceToProperty> context)
        {

            if (!context.Aliases.Contains(context.ContextData.path!))
            {
                // add missing join
            }
            var p = context.ContextData.property!;
            context.Query.Select($"{context.ContextData.path!}." + p.columnName + $" as {context.ContextData.path! + p.Name}");
            return context;
        }
    }

    public class BinaryFilter
    {
        /// <summary>
        /// Path to the entity containing this property (property segment is excluded)
        /// </summary>
        public EdmPath? path;
        public EdmStructuralProperty? property;
        public string op;
        public object? value;
    }

    public class BinaryFilterCompiler : Compiler<BinaryFilter>
    {
        public BinaryFilterCompiler(FStormService fStormService) : base(fStormService) { }

        public override CompilerContext<BinaryFilter> Compile(CompilerContext<BinaryFilter> context)
        {
            if (!context.Aliases.Contains(context.ContextData.path!))
            {
                // add missing join
            }

            // vary 'where' method on BinaryFilter.op
            context.Query.Where($"{context.ContextData.path!.ToString()}.{context.ContextData.property!.columnName}", context.ContextData.value);
            return context;
        }
    }


}
