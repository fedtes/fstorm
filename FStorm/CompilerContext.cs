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
        private AliasStore Aliases;
        private OutputKind outputKind;
        private EdmPath resourcePath = null!;
        private EdmEntityType? resourceEdmType;
        private ODataPath oDataPath;
        private FilterClause filter;

        public CompilerContext(ODataPath oDataPath, FilterClause filter) {
            this.Aliases = new AliasStore();
            this.Query =  new SqlKata.Query();
            this.oDataPath= oDataPath;
            this.filter = filter;
        }

        public CompilerContext(ODataPath oDataPath, FilterClause filter, SqlKata.Query query) {
            this.Aliases = new AliasStore();
            this.Query =  query;
            this.oDataPath= oDataPath;
            this.filter = filter;
        }

        internal SqlKata.Query GetQuery() => Query;
        internal ODataPath GetOdataRequestPath() => oDataPath;
        internal FilterClause GetFilterClause() => filter;
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

        internal void AddSelectAuto() {
            if (this.GetOutputKind() == OutputKind.Collection || this.GetOutputKind() == OutputKind.Object || this.GetOutputKind() == OutputKind.Property) {
                    this.AddSelect(this.GetOutputPath(), this.GetOutputType()!.GetEntityKey(), ":key");
            }

            if (this.GetOutputKind() == OutputKind.Collection || this.GetOutputKind() == OutputKind.Object)
            {
                foreach (var property in this.GetOutputType().DeclaredStructuralProperties())
                {
                    this.AddSelect(this.GetOutputPath(), (EdmStructuralProperty)property);
                }
            }
        }

        internal void AddCount(EdmPath edmPath, EdmStructuralProperty edmStructuralProperty)
        {
            var p = Aliases.AddOrGet(edmPath);
            Query.AsCount(new string[] {$"{p}.{edmStructuralProperty.columnName}"});
        }

        internal void AddFilter(BinaryFilter filter)
        {
            var p = Aliases.AddOrGet(filter.PropertyReference.ResourcePath);
            switch (filter.OperatorKind)
            {
                case BinaryOperatorKind.Or:
                    throw new NotImplementedException();
                    break;
                case BinaryOperatorKind.And:
                    throw new NotImplementedException();
                    break;
                case BinaryOperatorKind.Equal:
                    Query.Where($"{p}.{filter.PropertyReference.Property.columnName}","=", filter.Value);
                    break;
                case BinaryOperatorKind.NotEqual:
                    Query.Where($"{p}.{filter.PropertyReference.Property.columnName}","<>", filter.Value);
                    break;
                case BinaryOperatorKind.GreaterThan:
                    Query.Where($"{p}.{filter.PropertyReference.Property.columnName}",">", filter.Value);
                    break;
                case BinaryOperatorKind.GreaterThanOrEqual:
                    Query.Where($"{p}.{filter.PropertyReference.Property.columnName}",">=", filter.Value);
                    break;
                case BinaryOperatorKind.LessThan:
                    Query.Where($"{p}.{filter.PropertyReference.Property.columnName}","<", filter.Value);
                    break;
                case BinaryOperatorKind.LessThanOrEqual:
                    Query.Where($"{p}.{filter.PropertyReference.Property.columnName}","<=", filter.Value);
                    break;
                case BinaryOperatorKind.Add:
                    throw new NotImplementedException();
                    break;
                case BinaryOperatorKind.Subtract:
                    throw new NotImplementedException();
                    break;
                case BinaryOperatorKind.Multiply:
                    throw new NotImplementedException();
                    break;
                case BinaryOperatorKind.Divide:
                    throw new NotImplementedException();
                    break;
                case BinaryOperatorKind.Modulo:
                    throw new NotImplementedException();
                    break;
                case BinaryOperatorKind.Has:
                    throw new NotImplementedException();
                    break;
            }
        }

        internal void WrapQuery(EdmPath resourcePath)
        {
            this.AddSelectAuto();
            CompilerContext tmpctx = new CompilerContext(this.GetOdataRequestPath(), filter);
            var p = tmpctx.Aliases.AddOrGet(resourcePath);
            this.Query = tmpctx.Query.From(this.Query, p);
            this.Aliases = tmpctx.Aliases;
        }

        // internal CompilerContext GetSubContext()
        // {
        //     return new CompilerContext(this.GetOdataRequestPath(), this.GetFilterClause(), new SqlKata.Query());
        // }

        internal void AddAndFilter()
        {
            // this.Query.Where()

            // var subContext = new CompilerContext(this.GetOdataRequestPath(), this.GetFilterClause());
            // Query.Where(q => {
            //     subContext.Query = q;
            //     return q;
            // });
            // return subContext;
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
