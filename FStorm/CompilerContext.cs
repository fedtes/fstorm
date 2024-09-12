using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace FStorm
{

    public enum OutputType
    {
        Collection,
        Object,
        Property,
        RawValue
    }


    

    /// <summary>
    /// Model passed between compilers.
    /// </summary>
    public class CompilerContext
    {
        public class AliasStore
        {

            private List<EdmPath> Aliases = new List<EdmPath>();

            public string AddOrGet(EdmPath path) {
                if (!Contains(path))
                    Aliases.Add(path);
                return path.ToString();
            }

            public bool Contains(EdmPath path) {
               return Aliases.Contains(path);
            }


        }


        /// <summary>
        /// Define metadata of the resource requested via request path.
        /// </summary>
        public class OutputData
        {
            public OutputType OutputType;
            public EdmPath ResourcePath = null!;
            public EdmEntityType? ResourceEdmType;
            public ODataPath ODataPath = null!;
        }

        /// <summary>
        /// Query model result of the compilation
        /// </summary>
        public SqlKata.Query Query = new SqlKata.Query();

        /// <summary>
        /// List of all aliases used in the From clausole
        /// </summary>
        public readonly AliasStore Aliases = new AliasStore();

        internal OutputData Output = new OutputData();


        internal void SetOutputKind(OutputType OutputType) {
            Output.OutputType = OutputType;
        }

        internal OutputType GetOutputKind() => Output.OutputType;

        internal void SetOutputPath(EdmPath ResourcePath) {
            Output.ResourcePath = ResourcePath;
        }

        internal EdmPath GetOutputPath() => Output.ResourcePath;

        internal void SetOutputType(EdmEntityType? ResourceEdmType) {
            Output.ResourceEdmType = ResourceEdmType;
        }

        internal EdmEntityType? GetOutputType() => Output.ResourceEdmType;

        internal EdmPath AddFrom(EdmEntityType edmEntityType, EdmPath edmPath)
        {
            var p = Aliases.AddOrGet(edmPath);
            Query.From(edmEntityType.Table + " as " + p.ToString());
            return edmPath;
        }

        internal EdmPath AddJoin(EdmNavigationProperty rightNavigationProperty, EdmPath rightPath, EdmPath leftPath)
        {
            var r = Aliases.AddOrGet(rightPath);
            var l = Aliases.AddOrGet(leftPath);
            var constraint = rightNavigationProperty.ReferentialConstraint.PropertyPairs.First();
            var sourceProperty = (EdmStructuralProperty)constraint.PrincipalProperty;
            var targetProperty = (EdmStructuralProperty)constraint.DependentProperty;
            Query.Join((rightNavigationProperty.Type.Definition.AsElementType() as EdmEntityType)!.Table + " as " + l, $"{l}.{targetProperty.columnName}", $"{r}.{sourceProperty.columnName}");
            return leftPath;
        }

        internal void AddSelect(EdmPath edmPath, EdmStructuralProperty property, string? customName = null)
        {
            var p = Aliases.AddOrGet(edmPath);
            Query.Select($"{p}.{property.columnName} as {p}/{(customName ?? property.Name)}");
        }

        internal void AddCount(EdmPath edmPath, EdmStructuralProperty edmStructuralProperty)
        {
            var p = Aliases.AddOrGet(edmPath);
            Query.AsCount(new string[] {$"{p}.{edmStructuralProperty.columnName}"});
        }

        internal void AddWhere(EdmResourcePath edmResourcePath, EdmStructuralProperty k, object value)
        {
            var p = Aliases.AddOrGet(edmResourcePath);
            Query.Where($"{p}.{k.columnName}",value);
        }

        internal void WrapQuery(EdmPath resourcePath)
        {
            throw new NotImplementedException();
        }
    }

}
