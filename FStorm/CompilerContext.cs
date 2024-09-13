using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace FStorm
{
    /// <summary>
    /// Model passed between compilers.
    /// </summary>
    public class CompilerContext
    {
        /// <summary>
        /// Query model result of the compilation
        /// </summary>
        private SqlKata.Query Query;

        /// <summary>
        /// List of all aliases used in the From clausole
        /// </summary>
        private readonly AliasStore Aliases;
        private OutputKind outputKind;
        private EdmPath resourcePath = null!;
        private EdmEntityType? resourceEdmType;
        private ODataPath oDataPath = null!;


        public CompilerContext(ODataPath oDataPath) {
            this.Aliases = new AliasStore();
            this.Query =  new SqlKata.Query();
            this.oDataPath= oDataPath;
        }

        internal SqlKata.Query GetQuery() => Query;
        internal ODataPath GetOdataRequestPath() => oDataPath;
        internal OutputKind GetOutputKind() => outputKind;
        internal void SetOutputKind(OutputKind OutputType) { outputKind = OutputType; }
        internal EdmPath GetOutputPath() => resourcePath;
        internal void SetOutputPath(EdmPath ResourcePath) { resourcePath = ResourcePath; }
        internal EdmEntityType? GetOutputType() => resourceEdmType;
        internal void SetOutputType(EdmEntityType? ResourceEdmType) { resourceEdmType = ResourceEdmType; }

#region "query manipulation"
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
#endregion

    }

    public enum OutputKind
    {
        Collection,
        Object,
        Property,
        RawValue
    }

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

}
