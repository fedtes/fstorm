using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Extensions.DependencyInjection;

namespace FStorm
{
    /// <summary>
    /// Model passed between compilers.
    /// </summary>
    public class CompilerContext : ICompilerContext
    {
        private readonly ISubContextSupportContext subContextSupportContext;
        private readonly IOutputContext outputContext;
        private readonly IOdataParserContext odataParserContext;
        private readonly IQueryBuilderContext queryBuilderContext;
        private readonly ODataService service;

        internal CompilerContext(ODataService service, string UriRequest, ODataPath oDataPath, FilterClause filter, SelectExpandClause selectExpand, OrderByClause orderBy, PaginationClause pagination, string skipToken)
        {
            this.service = service;
            this.odataParserContext = new OdataParserContext(UriRequest,oDataPath,filter,selectExpand,orderBy,pagination,skipToken);
            this.subContextSupportContext = new SubContextSupportContext(this, service, this.Push, this.Pop);
            this.queryBuilderContext = new QueryBuilderContext(service);
            this.outputContext = new OutputContext();
        }

        public SQLCompiledQuery Compile() => ActiveScope.Query is null? throw new ArgumentNullException("ActiveScope.Query") : ActiveScope.Query.Compile();
        public List<Variable> GetVariablesInScope() => queryBuilderContext.GetVariablesInScope();

        public Variable? GetCurrentVariableInScope() => queryBuilderContext.GetCurrentVariableInScope();

#region "SubContextSupportContext"

        public ICompilerContext GetSubContext(string name) => subContextSupportContext.GetSubContext(name);

        public bool HasSubContext() => subContextSupportContext.HasSubContext();

        public IDictionary<string, ICompilerContext> GetSubContextes() => subContextSupportContext.GetSubContextes();

        public ICompilerContext OpenExpansionScope(ExpandedNavigationSelectItem i) => subContextSupportContext.OpenExpansionScope(i);

        public void CloseExpansionScope(ICompilerContext expansionContext, ExpandedNavigationSelectItem i) => subContextSupportContext.CloseExpansionScope(expansionContext, i);
#endregion

        public void SetOutputKind(OutputKind OutputType) => outputContext.SetOutputKind(OutputType);

        public OutputKind GetOutputKind() => outputContext.GetOutputKind();

        public void SetOutputPath(EdmPath ResourcePath) => outputContext.SetOutputPath(ResourcePath);

        public EdmPath? GetOutputPath() => outputContext.GetOutputPath();

        public EdmEntityType? GetOutputType() => outputContext.GetOutputType();

        public void SetOutputType(EdmEntityType? ResourceEdmType) => outputContext.SetOutputType(ResourceEdmType);

        public string UriRequest => odataParserContext.UriRequest;

        public AliasStore Aliases => queryBuilderContext.Aliases;

        public CompilerScope ActiveScope => queryBuilderContext.ActiveScope;

        public ODataPath GetOdataRequestPath() => odataParserContext.GetOdataRequestPath();

        public FilterClause GetFilterClause()  => odataParserContext.GetFilterClause();

        public SelectExpandClause GetSelectAndExpand()  => odataParserContext.GetSelectAndExpand();

        public PaginationClause GetPaginationClause() => odataParserContext.GetPaginationClause();

        public OrderByClause GetOrderByClause()  => odataParserContext.GetOrderByClause();

        public string GetSkipToken() => odataParserContext.GetSkipToken();

        public bool HasFrom() => queryBuilderContext.HasFrom();
        public IQueryBuilder GetQuery() => queryBuilderContext.GetQuery();

        public void Push(CompilerScope scope) => queryBuilderContext.Push(scope);

        public CompilerScope Pop() => queryBuilderContext.Pop();

        public void OpenAndScope() => queryBuilderContext.OpenAndScope();

        public void CloseAndScope() => queryBuilderContext.CloseAndScope();
        public void OpenOrScope() => queryBuilderContext.OpenOrScope();

        public void CloseOrScope() => queryBuilderContext.CloseOrScope();

        public void OpenNotScope() => queryBuilderContext.OpenNotScope();

        public void CloseNotScope() => queryBuilderContext.CloseNotScope();

        public void OpenVariableScope(Variable variable) => queryBuilderContext.OpenVariableScope(variable);

        public void CloseVariableScope() => queryBuilderContext.CloseVariableScope();

        public void OpenAnyScope() => queryBuilderContext.OpenAnyScope();
        public void CloseAnyScope() => queryBuilderContext.CloseAnyScope();

        public void OpenAllScope() => queryBuilderContext.OpenAllScope();

        public void CloseAllScope() => queryBuilderContext.CloseAllScope();

        public EdmPath AddFrom(EdmEntityType edmEntityType, EdmPath edmPath) => queryBuilderContext.AddFrom(edmEntityType,edmPath);
        public EdmPath AddJoin(EdmNavigationProperty rightNavigationProperty, EdmPath rightPath, EdmPath leftPath) => queryBuilderContext.AddJoin(rightNavigationProperty,rightPath,leftPath);

        public void AddSelect(EdmPath edmPath, EdmStructuralProperty property, string? customName = null) => queryBuilderContext.AddSelect(edmPath,property,customName);

        public void AddSelectKey(EdmPath? path, EdmEntityType? type) => queryBuilderContext.AddSelectKey(path, type);
        public void AddSelectAll(EdmPath? path, EdmEntityType? type) => queryBuilderContext.AddSelectAll(path, type);

        public void AddCount(EdmPath edmPath, EdmStructuralProperty edmStructuralProperty) => queryBuilderContext.AddCount(edmPath, edmStructuralProperty);

        public void AddFilter(BinaryFilter filter) => queryBuilderContext.AddFilter(filter);

        public void AddOrderBy(EdmPath edmPath, EdmStructuralProperty property, OrderByDirection direction) => queryBuilderContext.AddOrderBy(edmPath,property, direction);

        public void WrapQuery(IOdataParserContext context, EdmPath resourcePath) => queryBuilderContext.WrapQuery(context, resourcePath);

        public void AddLimit(long top) => queryBuilderContext.AddLimit(top);

        public void AddOffset(long skip) => queryBuilderContext.AddOffset(skip);
    }

}
