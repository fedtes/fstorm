using System;
using Microsoft.OData.UriParser;

namespace FStorm;

public class ExpansionCompilerContext : CompilerContext, ICompilerContext
{
    public ExpansionCompilerContext(
        FStormService service,
        ODataPath oDataPath,
        FilterClause filter,
        SelectExpandClause selectExpand,
        OrderByClause orderBy,
        PaginationClause pagination) : base(service, oDataPath, filter, selectExpand, orderBy, pagination)
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
