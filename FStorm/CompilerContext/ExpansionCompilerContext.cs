using System;
using Microsoft.OData.UriParser;

namespace FStorm;

public class ExpansionCompilerContext : CompilerContext, ICompilerContext
{
    public ExpansionCompilerContext(
        ODataService service,
        ODataPath oDataPath,
        FilterClause filter,
        SelectExpandClause selectExpand,
        OrderByClause orderBy,
        PaginationClause pagination) : base(service,string.Empty, oDataPath, filter, selectExpand, orderBy, pagination, string.Empty)
    {    }

    internal int Skip {get; private set;} = 0;
    internal int Top {get; private set;} = int.MaxValue;

    void ICompilerContext.AddOffset(long skip)
    {
        this.Skip = Convert.ToInt32(skip);
    }

    void ICompilerContext.AddLimit(long top)
    {
        this.Top= Convert.ToInt32(top);
    }
}
