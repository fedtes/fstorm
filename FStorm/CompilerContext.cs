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
        /// True if in the current scope, the where clauses, are in OR relation each others.
        /// </summary>
        private bool IsOr {get => ActiveScope.ScopeType == CompilerScope.OR;}

        /// <summary>
        /// List of all aliases used in the From clause
        /// </summary>
        private AliasStore Aliases {get => MainScope.Aliases; }
        private OutputKind outputKind;
        private EdmPath resourcePath = null!;
        private EdmEntityType? resourceEdmType;
        private ODataPath oDataPath;
        private FilterClause filter;

        public CompilerContext(ODataPath oDataPath, FilterClause filter) {
            this.oDataPath= oDataPath;
            this.filter = filter;
            SetMainQuery(new SqlKata.Query());
        }

        public CompilerContext(ODataPath oDataPath, FilterClause filter, SqlKata.Query query) {
            this.oDataPath= oDataPath;
            this.filter = filter;
            SetMainQuery(query);
        }

        private void SetMainQuery(SqlKata.Query query, AliasStore? aliasStore = null) {
            scope.Clear();
            scope.Push(new CompilerScope(CompilerScope.ROOT, query, aliasStore));
        }
        internal SqlKata.Query GetQuery() => ActiveQuery;
        internal ODataPath GetOdataRequestPath() => oDataPath;
        internal FilterClause GetFilterClause() => filter;
        internal OutputKind GetOutputKind() => outputKind;
        internal void SetOutputKind(OutputKind OutputType) { outputKind = OutputType; }
        internal EdmPath GetOutputPath() => resourcePath;
        internal void SetOutputPath(EdmPath ResourcePath) { resourcePath = ResourcePath; }
        internal EdmEntityType? GetOutputType() => resourceEdmType;
        internal void SetOutputType(EdmEntityType? ResourceEdmType) { resourceEdmType = ResourceEdmType; }

        internal List<Variable> GetVariablesInScope() {
            return scope.Where(x => x.ScopeType == CompilerScope.VARIABLE)
                .Select(x => x.Variable)
                .Where(x => x != null)
                .Cast<Variable>()
                .ToList();
        }

        internal Variable? GetCurrentVariableInScope() {
            return scope.FirstOrDefault(x => x.ScopeType == CompilerScope.VARIABLE)?.Variable;
        }

#region "scope manipulation"

        /// <summary>
        /// Stack tracking various types of <see cref="CompilerScope"/> during the semantic parsing done by <see cref="SemanticVisitor"/>.
        /// </summary>
        /// <remarks>
        /// Each scope may contains various information about variables, subquery, boolean operations, lambdas etc..
        /// </remarks>
        private Stack<CompilerScope> scope = new Stack<CompilerScope>();

        /// <summary>
        /// Return the first "meaningfull" scope of the stack. This could be anything and do not rely on it to access any from/join clasue.
        /// </summary>
        private CompilerScope ActiveScope {get => scope.First(x => x.ScopeType != CompilerScope.NO_SCOPE); }

        /// <summary>
        /// Return the first "query builder" scope containing a from clasue. This may be the ROOT or a suquery actually processing.
        /// </summary>
        private CompilerScope MainScope {get => scope.First(x => x.ScopeType == CompilerScope.ROOT || x.ScopeType == CompilerScope.ANY); }

        /// <summary>
        /// Return the first MAIN 
        /// </summary>
        private CompilerScope RootScope {get => scope.First(x => x.ScopeType == CompilerScope.ROOT); }

        /// <summary>
        /// Shortcut to access the underlyng query builder of the <see cref="MainScope"/>.
        /// </summary>
        private SqlKata.Query MainQuery { get => MainScope.Query; }

        /// <summary>
        /// Shortcut to access the underlyng query builder of the <see cref="ActiveScope"/>.
        /// </summary>
        private SqlKata.Query ActiveQuery { get => ActiveScope.Query;}

        private SqlKata.Query RootQuery { get => RootScope.Query;}

        /// <summary>
        /// Open an "AND" scope. While in that the where clauses are in AND relation each others 
        /// </summary>
        internal void OpenAndScope() {
            if (ActiveScope.ScopeType != CompilerScope.AND)
            {
                scope.Push(new CompilerScope(CompilerScope.AND,new SqlKata.Query()));
            }
            else {
                scope.Push(new CompilerScope(CompilerScope.NO_SCOPE, ActiveQuery));
            }
        }

        /// <summary>
        /// Close the current AND scope
        /// </summary>
        internal void CloseAndScope() {
            if (scope.Peek().ScopeType == CompilerScope.AND)
            {
                var s = scope.Pop();
                (IsOr ? ActiveQuery.Or() : ActiveQuery).Where(_q => s.Query);
            }
            else 
            {
                scope.Pop();
            }
        }

        /// <summary>
        /// Open an "OR" scope. While in that the where clauses are in OR relation each others 
        /// </summary>
        internal void OpenOrScope() {
            if (ActiveScope.ScopeType != CompilerScope.OR)
            {
                scope.Push(new CompilerScope(CompilerScope.OR,new SqlKata.Query()));
            }
            else 
            {
                scope.Push(new CompilerScope(CompilerScope.NO_SCOPE, ActiveQuery));
            }
        }

        /// <summary>
        /// Close the current OR scope
        /// </summary>
        internal void CloseOrScope() {
            if (scope.Peek().ScopeType == CompilerScope.OR)
            {
                var s = scope.Pop();
                (IsOr ? ActiveQuery.Or() : ActiveQuery).Where(_q => s.Query);
            }
            else 
            {
                scope.Pop();
            }
        }

        /// <summary>
        /// Open a variable scope by pushing a variable into the current context. Variable are visibile from all "children" scope opened from here.
        /// </summary>
        /// <param name="variable"></param>
        internal void OpenVariableScope(Variable variable) {
            var s = new CompilerScope(CompilerScope.VARIABLE, ActiveQuery, variable);
            scope.Push(s);
        }

        /// <summary>
        /// Close current variable scope and pop the variable out of the visibility, destroying it.
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        internal void CloseVariableScope() {
            var s = scope.Pop();
            if (s.ScopeType != CompilerScope.VARIABLE) 
                throw new ApplicationException("Should not pass here!!");
        }

        /// <summary>
        /// Open a scope where handling the ANY operator. This open a sub-query where all operations are perfomed until the scope is closed.
        /// </summary>
        internal void OpenAnyScope()
        {
            var anyQ = new SqlKata.Query();
            var s = new CompilerScope(CompilerScope.ANY, anyQ, new AliasStore("ANY"));
            scope.Push(s);
            var _it = GetVariablesInScope().First(x => x.Name =="$it");
            var _var = this.GetCurrentVariableInScope()!;
            var _varElements = _var.ResourcePath.AsEdmElements();
            var _tail = _var.ResourcePath - _it.ResourcePath;
            /*
                ~/t_0/t_1/t_2/t_3/t_4/t_5/t_6
                |------------var-------------|
                |--$it---|
                         |-------tail--------|
            */

            for (int i = _it.ResourcePath.Count(); i < _varElements.Count(); i++)
            {
                if (i == _it.ResourcePath.Count()) 
                {
                    EdmNavigationProperty type =(EdmNavigationProperty)_varElements[i].Item2;
                    AddFrom((EdmEntityType)type.Type.Definition.AsElementType(), _varElements[i].Item1);
                }
                else
                {
                    EdmNavigationProperty type =(EdmNavigationProperty)_varElements[i].Item2;
                    AddJoin(type, _varElements[i].Item1 - 1, _varElements[i].Item1);
                }
            }

            if (_varElements[_it.ResourcePath.Count()].Item2 is EdmNavigationProperty navigationProperty) 
            {
                var (sourceProperty, targetProperty) = navigationProperty.GetRelationProperties();
                var alias_var = Aliases.AddOrGet(_varElements[_it.ResourcePath.Count()].Item1);
                var alias_it = RootScope.Aliases.AddOrGet(_it.ResourcePath);
                ActiveQuery.WhereColumns($"{alias_var}.{targetProperty.columnName}","=", $"{alias_it}.{sourceProperty.columnName}");
            }
            else 
            {
                throw new ApplicationException("should not pass here!!");
            }
        }

        /// <summary>
        /// Close the current ANY scope and link the resutl to the main query.
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        internal void CloseAnyScope()
        {
            var s = scope.Pop();
            if (s.ScopeType != CompilerScope.ANY)  throw new ApplicationException("Should not pass here!!");
            (IsOr ? ActiveQuery.Or() : ActiveQuery).WhereExists(s.Query);
        }

#endregion

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
            var (sourceProperty, targetProperty) = rightNavigationProperty.GetRelationProperties();
            this.MainQuery.LeftJoin((rightNavigationProperty.Type.Definition.AsElementType() as EdmEntityType)!.Table + " as " + l, $"{l}.{targetProperty.columnName}", $"{r}.{sourceProperty.columnName}");
            return leftPath;
        }

        internal void AddSelect(EdmPath edmPath, EdmStructuralProperty property, string? customName = null)
        {
            var p = Aliases.AddOrGet(edmPath);
            ActiveQuery.Select($"{p}.{property.columnName} as {p}/{(customName ?? property.Name)}");
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
            ActiveQuery.AsCount(new string[] {$"{p}.{edmStructuralProperty.columnName}"});
        }

        internal void AddFilter(BinaryFilter filter)
        {
            var p = Aliases.AddOrGet(filter.PropertyReference.ResourcePath);
            switch (filter.OperatorKind)
            {
                case BinaryOperatorKind.Or:
                    throw new NotImplementedException("should not pass here!");
                case BinaryOperatorKind.And:
                    throw new NotImplementedException("should not pass here!");
                case BinaryOperatorKind.Equal:
                    (IsOr ? ActiveQuery.Or() : ActiveQuery).Where($"{p}.{filter.PropertyReference.Property.columnName}","=", filter.Value);
                    break;
                case BinaryOperatorKind.NotEqual:
                    (IsOr ? ActiveQuery.Or() : ActiveQuery).Where($"{p}.{filter.PropertyReference.Property.columnName}","<>", filter.Value);
                    break;
                case BinaryOperatorKind.GreaterThan:
                    (IsOr ? ActiveQuery.Or() : ActiveQuery).Where($"{p}.{filter.PropertyReference.Property.columnName}",">", filter.Value);
                    break;
                case BinaryOperatorKind.GreaterThanOrEqual:
                    (IsOr ? ActiveQuery.Or() : ActiveQuery).Where($"{p}.{filter.PropertyReference.Property.columnName}",">=", filter.Value);
                    break;
                case BinaryOperatorKind.LessThan:
                    (IsOr ? ActiveQuery.Or() : ActiveQuery).Where($"{p}.{filter.PropertyReference.Property.columnName}","<", filter.Value);
                    break;
                case BinaryOperatorKind.LessThanOrEqual:
                    (IsOr ? ActiveQuery.Or() : ActiveQuery).Where($"{p}.{filter.PropertyReference.Property.columnName}","<=", filter.Value);
                    break;
                case BinaryOperatorKind.Add:
                    throw new NotImplementedException();
                case BinaryOperatorKind.Subtract:
                    throw new NotImplementedException();
                case BinaryOperatorKind.Multiply:
                    throw new NotImplementedException();
                case BinaryOperatorKind.Divide:
                    throw new NotImplementedException();
                case BinaryOperatorKind.Modulo:
                    throw new NotImplementedException();
                case BinaryOperatorKind.Has:
                    throw new NotImplementedException();
            }
        }


        internal void WrapQuery(EdmPath resourcePath)
        {
            if (scope.Count > 1 || scope.Peek().ScopeType != CompilerScope.ROOT) {
                throw new Exception("Cannot wrap query while a sub compiler scope is open or the current scopo is not the main");
            }

            this.AddSelectAuto();
            CompilerContext tmpctx = new CompilerContext(this.GetOdataRequestPath(), filter);
            var p = tmpctx.Aliases.AddOrGet(resourcePath);
            SetMainQuery(tmpctx.ActiveQuery.From(this.ActiveQuery, p), tmpctx.Aliases);
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
        private readonly string Prefix;

        private int index = 0;

        public AliasStore() {
            Prefix = "P";
        }

        public AliasStore(string Prefix) {
            this.Prefix = Prefix;
        }

        private Dictionary<EdmPath, string> aliases = new Dictionary<EdmPath, string>();

        private string ComputeNewAlias(EdmPath path) {
            index++;
            return $"{Prefix}{index}";
        }

        public string AddOrGet(EdmPath path) {
            if (!Contains(path)) {
                var a = ComputeNewAlias(path);
                aliases.Add(path, a);
            }
            return aliases[path];
        }

        public bool Contains(EdmPath path) {
            return aliases.ContainsKey(path);
        }
    }

    public class CompilerScope
    {
        public const string ROOT = "main";
        public const string AND = "and";
        public const string OR = "or";
        public const string NO_SCOPE = "noscope";
        public const string VARIABLE = "var";
        public const string ANY = "any";

        public readonly SqlKata.Query Query;
        public readonly string ScopeType;
        public readonly Variable? Variable;

        public readonly AliasStore Aliases;

        internal CompilerScope(string scopeType, SqlKata.Query query){
            ScopeType = scopeType;
            Query = query;
            Aliases = new AliasStore();
        }

        internal CompilerScope(string scopeType, SqlKata.Query query, AliasStore? aliasStore){
            ScopeType = scopeType;
            Query = query;
            Aliases = aliasStore ?? new AliasStore();
        }

        internal CompilerScope(string scopeType, SqlKata.Query query, Variable variable){
            ScopeType = scopeType;
            Query = query;
            this.Variable = variable;
            Aliases = new AliasStore();
        }

        public override string ToString()
        {
            return ScopeType;
        }
    }

}
