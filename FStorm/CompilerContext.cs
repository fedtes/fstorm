using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System.Collections.Generic;
using System.Net.NetworkInformation;

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
        private Stack<CompilerScope> scope = new Stack<CompilerScope>();
        private CompilerScope ActiveScope {get => scope.First(x => x.ScopeType != CompilerScope.NO_SCOPE); }
        private SqlKata.Query MainQuery { get => scope.First(x => x.ScopeType == CompilerScope.MAIN).Query; }
        private SqlKata.Query Query { get => ActiveScope.Query;}
        private bool IsOr {get => ActiveScope.ScopeType == CompilerScope.OR;}

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
            this.oDataPath= oDataPath;
            this.filter = filter;
            SetMainQuery(new SqlKata.Query());
        }

        public CompilerContext(ODataPath oDataPath, FilterClause filter, SqlKata.Query query) {
            this.Aliases = new AliasStore();
            this.oDataPath= oDataPath;
            this.filter = filter;
            SetMainQuery(query);
        }

        private void SetMainQuery(SqlKata.Query query) {
            scope.Clear();
            scope.Push(new CompilerScope(CompilerScope.MAIN, query));
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

        internal void OpenAndScope() {
            if (ActiveScope.ScopeType != CompilerScope.AND)
            {
                scope.Push(new CompilerScope(CompilerScope.AND,new SqlKata.Query()));
            }
            else {
                scope.Push(new CompilerScope(CompilerScope.NO_SCOPE, Query));
            }
        }

        internal void CloseAndScope() {
            if (scope.Peek().ScopeType == CompilerScope.AND)
            {
                var s = scope.Pop();
                (IsOr ? Query.Or() : Query).Where(_q => s.Query);
            }
            else 
            {
                scope.Pop();
            }
        }

        internal void OpenOrScope() {
            if (ActiveScope.ScopeType != CompilerScope.OR)
            {
                scope.Push(new CompilerScope(CompilerScope.OR,new SqlKata.Query()));
            }
            else 
            {
                scope.Push(new CompilerScope(CompilerScope.NO_SCOPE, Query));
            }
        }

        internal void CloseOrScope() {
            if (scope.Peek().ScopeType == CompilerScope.OR)
            {
                var s = scope.Pop();
                (IsOr ? Query.Or() : Query).Where(_q => s.Query);
            }
            else 
            {
                scope.Pop();
            }
        }

#region "query manipulation"
        internal EdmPath AddFrom(EdmEntityType edmEntityType, EdmPath edmPath)
        {
            var p = Aliases.AddOrGet(edmPath);
            this.MainQuery.From(edmEntityType.Table + " as " + p.ToString());
            return edmPath;
        }

        internal EdmPath AddJoin(EdmNavigationProperty rightNavigationProperty, EdmPath rightPath, EdmPath leftPath)
        {
            if (Aliases.Contains(leftPath)) return leftPath;
            var r = Aliases.AddOrGet(rightPath);
            var l = Aliases.AddOrGet(leftPath);
            var constraint = rightNavigationProperty.ReferentialConstraint.PropertyPairs.First();
            var sourceProperty = (EdmStructuralProperty)constraint.PrincipalProperty;
            var targetProperty = (EdmStructuralProperty)constraint.DependentProperty;
            this.MainQuery.Join((rightNavigationProperty.Type.Definition.AsElementType() as EdmEntityType)!.Table + " as " + l, $"{l}.{targetProperty.columnName}", $"{r}.{sourceProperty.columnName}");
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
                    (IsOr ? Query.Or() : Query).Where($"{p}.{filter.PropertyReference.Property.columnName}","=", filter.Value);
                    break;
                case BinaryOperatorKind.NotEqual:
                    (IsOr ? Query.Or() : Query).Where($"{p}.{filter.PropertyReference.Property.columnName}","<>", filter.Value);
                    break;
                case BinaryOperatorKind.GreaterThan:
                    (IsOr ? Query.Or() : Query).Where($"{p}.{filter.PropertyReference.Property.columnName}",">", filter.Value);
                    break;
                case BinaryOperatorKind.GreaterThanOrEqual:
                    (IsOr ? Query.Or() : Query).Where($"{p}.{filter.PropertyReference.Property.columnName}",">=", filter.Value);
                    break;
                case BinaryOperatorKind.LessThan:
                    (IsOr ? Query.Or() : Query).Where($"{p}.{filter.PropertyReference.Property.columnName}","<", filter.Value);
                    break;
                case BinaryOperatorKind.LessThanOrEqual:
                    (IsOr ? Query.Or() : Query).Where($"{p}.{filter.PropertyReference.Property.columnName}","<=", filter.Value);
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
            if (scope.Count > 1 || scope.Peek().ScopeType != CompilerScope.MAIN) {
                throw new Exception("Cannot wrap query while a sub compiler scope is open or the current scopo is not the main");
            }

            this.AddSelectAuto();
            CompilerContext tmpctx = new CompilerContext(this.GetOdataRequestPath(), filter);
            var p = tmpctx.Aliases.AddOrGet(resourcePath);
            SetMainQuery(tmpctx.Query.From(this.Query, p));
            this.Aliases = tmpctx.Aliases;
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

    public class CompilerScope
    {
        public const string MAIN = "main";
        public const string AND = "and";
        public const string OR = "or";
        public const string NO_SCOPE = "noscope";
        public readonly SqlKata.Query Query;

        public readonly string ScopeType;

        internal CompilerScope(string scopeType, SqlKata.Query query){
            ScopeType = scopeType;
            Query = query;
        }

        public override string ToString()
        {
            return ScopeType;
        }
    }

}
