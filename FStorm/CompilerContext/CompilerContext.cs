using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Extensions.DependencyInjection;

namespace FStorm
{
    /// <summary>
    /// Model passed between compilers.
    /// </summary>
    public partial class CompilerContext : ICompilerContext
    {
        /// <summary>
        /// True if in the current scope, the where clauses, are in OR relation each others.
        /// </summary>
        private bool IsOr { get => ActiveScope.ScopeType == CompilerScope.OR; }

        /// <summary>
        /// List of all aliases used in the From clause
        /// </summary>
        AliasStore ICompilerContext.Aliases { get => MainScope.Aliases; }
        private string _UriRequest;
        string ICompilerContext.UriRequest { get => _UriRequest; }

        private OutputKind outputKind;
        private EdmPath? resourcePath = null;
        private EdmEntityType? resourceEdmType = null;
        private readonly ODataService service;
        private ODataPath oDataPath;
        private FilterClause filter;
        private SelectExpandClause selectExpand;
        private readonly OrderByClause orderBy;
        private readonly PaginationClause pagination;
        private readonly string skipToken;

        private Dictionary<string, ICompilerContext> subcontextes = new Dictionary<string, ICompilerContext>();

        public CompilerContext(ODataService service,
                               string UriRequest,
                               ODataPath oDataPath,
                               FilterClause filter,
                               SelectExpandClause selectExpand,
                               OrderByClause orderBy,
                               PaginationClause pagination,
                               string skipToken)
        {
            this.service = service;
            _UriRequest = UriRequest;
            this.oDataPath = oDataPath;
            this.filter = filter;
            this.selectExpand = selectExpand;
            this.orderBy = orderBy;
            this.pagination = pagination;
            this.skipToken = skipToken;
            SetMainQuery(service.serviceProvider.GetService<IQueryBuilder>()!);
        }

        private void SetMainQuery(IQueryBuilder query, AliasStore? aliasStore = null)
        {
            scope.Clear();
            scope.Push(new CompilerScope(CompilerScope.ROOT, query, aliasStore));
        }
        SQLCompiledQuery ICompilerContext.Compile() => ActiveQuery.Compile();
        ODataPath ICompilerContext.GetOdataRequestPath() => oDataPath;
        FilterClause ICompilerContext.GetFilterClause() => filter;
        SelectExpandClause ICompilerContext.GetSelectAndExpand() => selectExpand;
        PaginationClause ICompilerContext.GetPaginationClause() => pagination;
        OrderByClause ICompilerContext.GetOrderByClause() => orderBy;
        string ICompilerContext.GetSkipToken() => skipToken;
        OutputKind ICompilerContext.GetOutputKind() => outputKind;
        void ICompilerContext.SetOutputKind(OutputKind OutputType) { outputKind = OutputType; }
        EdmPath? ICompilerContext.GetOutputPath() => resourcePath;
        void ICompilerContext.SetOutputPath(EdmPath ResourcePath)
        {
            resourcePath = ResourcePath;
            ((ICompilerContext)this).SetOutputType(ResourcePath.GetEdmEntityType());
        }

        bool ICompilerContext.HasFrom() => MainScope.HasFromClause;

        EdmEntityType? ICompilerContext.GetOutputType() => resourceEdmType;
        void ICompilerContext.SetOutputType(EdmEntityType? ResourceEdmType) { resourceEdmType = ResourceEdmType; }
        IQueryBuilder ICompilerContext.GetQuery() => ActiveQuery;

        List<Variable> ICompilerContext.GetVariablesInScope()
        {
            return scope.Where(x => x.ScopeType == CompilerScope.VARIABLE)
                .Select(x => x.Variable)
                .Where(x => x != null)
                .Cast<Variable>()
                .ToList();
        }

        Variable? ICompilerContext.GetCurrentVariableInScope()
        {
            return scope.FirstOrDefault(x => x.ScopeType == CompilerScope.VARIABLE)?.Variable;
        }

        ICompilerContext ICompilerContext.GetSubContext(string name)
        {
            if (subcontextes.ContainsKey(name))
                return subcontextes[name];
            else
                throw new ArgumentException($"Subcontext with name {name} not found", nameof(name));
        }

        bool ICompilerContext.HasSubContext() => subcontextes.Any();

        IDictionary<string, ICompilerContext> ICompilerContext.GetSubContextes() => subcontextes;


        #region "private"

        private (EdmPath, IEdmElement) CreateSubQuery(Variable _it)
        {
            var _var = ((ICompilerContext)this).GetCurrentVariableInScope()!;
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
                    EdmNavigationProperty type = (EdmNavigationProperty)_varElements[i].Item2;
                    ((ICompilerContext)this).AddFrom((EdmEntityType)type.Type.Definition.AsElementType(), _varElements[i].Item1);
                }
                else
                {
                    EdmNavigationProperty type = (EdmNavigationProperty)_varElements[i].Item2;
                    ((ICompilerContext)this).AddJoin(type, _varElements[i].Item1 - 1, _varElements[i].Item1);
                }
            }

            return _varElements[_it.ResourcePath.Count()];
        }

        #endregion
    }

    internal class CompilerScope
    {
        public const string ROOT = "main";
        public const string AND = "and";
        public const string NOT = "not";
        public const string OR = "or";
        public const string NO_SCOPE = "noscope";
        public const string VARIABLE = "var";
        public const string ANY = "any";
        public const string ALL = "all";
        public const string EXPAND = "expand";

        public readonly IQueryBuilder Query;
        public readonly string ScopeType;
        public readonly Variable? Variable;
        public readonly AliasStore Aliases;
        public bool HasFromClause = false;

        internal CompilerScope(string scopeType, IQueryBuilder query)
        {
            ScopeType = scopeType;
            Query = query;
            Aliases = new AliasStore();
        }

        internal CompilerScope(string scopeType, IQueryBuilder query, AliasStore? aliasStore)
        {
            ScopeType = scopeType;
            Query = query;
            Aliases = aliasStore ?? new AliasStore();
        }

        internal CompilerScope(string scopeType, IQueryBuilder query, Variable variable)
        {
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

    public enum OutputKind
    {
        Collection,
        Object,
        Property,
        RawValue
    }
    internal class AliasStore
    {
        private readonly string Prefix;

        private int index = 0;

        public AliasStore()
        {
            Prefix = "P";
        }

        public AliasStore(string Prefix)
        {
            this.Prefix = Prefix;
        }

        private Dictionary<EdmPath, string> aliases = new Dictionary<EdmPath, string>();

        private string ComputeNewAlias(EdmPath path)
        {
            index++;
            return $"{Prefix}{index}";
        }

        public string AddOrGet(EdmPath path)
        {
            if (!Contains(path))
            {
                var a = ComputeNewAlias(path);
                aliases.Add(path, a);
            }
            return aliases[path];
        }

        public bool Contains(EdmPath path)
        {
            return aliases.ContainsKey(path);
        }

        internal EdmPath? TryGet(string v)
        {
            return aliases.FirstOrDefault(x => x.Value == v).Key;
        }
    }
}
