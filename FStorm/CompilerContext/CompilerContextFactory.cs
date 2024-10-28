using System;
using Microsoft.OData.UriParser;

namespace FStorm;

public class CompilerContextFactory
{
    internal ICompilerContext CreateContext(ODataService service, ODataPath oDataPath, FilterClause filter, SelectExpandClause selectExpand, OrderByClause orderBy, PaginationClause pagination) => 
        new CompilerContext(service, oDataPath, filter, selectExpand,orderBy, pagination);

    internal ICompilerContext CreateExpansionContext(ODataService service, ODataPath oDataPath, FilterClause filter, SelectExpandClause selectExpand, OrderByClause orderBy, PaginationClause pagination) =>
        new ExpansionCompilerContext(service, oDataPath, filter, selectExpand,orderBy, pagination);
}
