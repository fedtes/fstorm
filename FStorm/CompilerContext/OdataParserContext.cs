using Microsoft.OData.UriParser;

namespace FStorm;


public class OdataParserContext : IOdataParserContext
{
    internal OdataParserContext(string UriRequest, ODataPath oDataPath, FilterClause filter, SelectExpandClause selectExpand, OrderByClause orderBy, PaginationClause pagination, string skipToken)
    {
        this._UriRequest = UriRequest;
        this.oDataPath = oDataPath;
        this.filter = filter;
        this.selectExpand = selectExpand;
        this.orderBy = orderBy;
        this.pagination = pagination;
        this.skipToken = skipToken;
    }

    private string _UriRequest;
    private ODataPath oDataPath;
    private FilterClause filter;
    private SelectExpandClause selectExpand;
    private readonly OrderByClause orderBy;
    private readonly PaginationClause pagination;
    private readonly string skipToken;

    public string UriRequest { get => _UriRequest; }
    public ODataPath GetOdataRequestPath() => oDataPath;
    public FilterClause GetFilterClause() => filter;
    public SelectExpandClause GetSelectAndExpand() => selectExpand;
    public PaginationClause GetPaginationClause() => pagination;
    public OrderByClause GetOrderByClause() => orderBy;
    public string GetSkipToken() => skipToken;
}

