using System;
using Microsoft.OData.UriParser;

namespace FStorm;

public class CompilerContextFactory
{
    private readonly ODataService service;

    public CompilerContextFactory(ODataService service)
    {
        this.service = service;
    }

    internal ICompilerContext CreateContext(string UriRequest) 
    {
        ODataUriParser parser = new ODataUriParser(service.Model, service.ServiceRoot, new Uri(UriRequest, UriKind.Relative));
        return CreateContext(
            UriRequest,
            parser.ParsePath(),
            parser.ParseFilter(),
            parser.ParseSelectAndExpand(),
            parser.ParseOrderBy(),
            new PaginationClause(parser.ParseTop(), parser.ParseSkip()),
            parser.ParseSkipToken());
    }

    internal ICompilerContext CreateContext(
        string UriRequest,
        ODataPath oDataPath,
        FilterClause filter,
        SelectExpandClause selectExpand,
        OrderByClause orderBy,
        PaginationClause pagination,
        string skipToken) => 
        new CompilerContext(service, UriRequest, oDataPath, filter, selectExpand,orderBy, pagination, skipToken);

    internal ICompilerContext CreateContext(IOdataParserContext context) 
    {
        return CreateContext(context.UriRequest, context.GetOdataRequestPath(), context.GetFilterClause(), context.GetSelectAndExpand(), context.GetOrderByClause(),context.GetPaginationClause(), context.GetSkipToken());
    }

    internal ICompilerContext CreateExpansionContext(ODataPath oDataPath, FilterClause filter, SelectExpandClause selectExpand, OrderByClause orderBy, PaginationClause pagination) =>
        new ExpansionCompilerContext(service, oDataPath, filter, selectExpand,orderBy, pagination);


    internal IQueryBuilderContext CreateQueryBuilderContext()
    {
        return new QueryBuilderContext(service);
    }

    internal IQueryBuilderContext CreateNoPluginQueryBuilderContext()
    {
        return new NoPluginQueryBuilderContext(service);
    }
}
