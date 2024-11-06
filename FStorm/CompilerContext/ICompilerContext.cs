using System;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace FStorm;

public interface IOdataParseContext
{
    string UriRequest { get; }
    ODataPath GetOdataRequestPath();
    FilterClause GetFilterClause();
    SelectExpandClause GetSelectAndExpand();
    PaginationClause GetPaginationClause();
    OrderByClause GetOrderByClause();
    string GetSkipToken();
}

public interface IOutputContext
{
    void SetOutputKind(OutputKind OutputType);
    OutputKind GetOutputKind();

    void SetOutputPath(EdmPath ResourcePath);
    EdmPath? GetOutputPath();

    EdmEntityType? GetOutputType();
    void SetOutputType(EdmEntityType? ResourceEdmType);
}


public interface ICompilerContext : IOdataParseContext, IOutputContext
{
    /// <summary>
    /// Keeps track of the aliases used in the query and the meaning they have.
    /// </summary>
    AliasStore Aliases { get; }
    /// <summary>
    /// Ask the underline query builder to produce an executable query.
    /// </summary>
    /// <returns></returns>
    SQLCompiledQuery Compile();

    bool HasFrom();
   
    IQueryBuilder GetQuery();
    List<Variable> GetVariablesInScope();
    Variable? GetCurrentVariableInScope();
    ICompilerContext GetSubContext(string name);
    bool HasSubContext();
    IDictionary<string, ICompilerContext> GetSubContextes();
#region "scopes"
    /// <summary>
    /// Open an "AND" scope. While in that the where clauses are in AND relation each others 
    /// </summary>
    void OpenAndScope();
    /// <summary>
    /// Close the current AND scope
    /// </summary>
    void CloseAndScope();
    /// <summary>
    /// Open an "OR" scope. While in that the where clauses are in OR relation each others 
    /// </summary>
    void OpenOrScope();
     /// <summary>
    /// Close the current OR scope
    /// </summary>
    void CloseOrScope();
    /// <summary>
    /// Open an "NOT" scope. While in that the "where" clauses are wrapped around NOT 
    /// </summary>
    void OpenNotScope();
    /// <summary>
    /// Close the current NOT scope
    /// </summary>
    void CloseNotScope();
     /// <summary>
    /// Open a variable scope by pushing a variable into the current context. Variable are visibile from all "children" scope opened from here.
    /// </summary>
    /// <param name="variable"></param>
    void OpenVariableScope(Variable variable);
    /// <summary>
    /// Close current variable scope and pop the variable out of the visibility, destroying it.
    /// </summary>
    /// <exception cref="ApplicationException"></exception>
    void CloseVariableScope();
    /// <summary>
    /// Open a scope where handling the ANY operator. This open a sub-query where all operations are perfomed until the scope is closed.
    /// </summary>
    void OpenAnyScope();
    /// <summary>
    /// Close the current ANY scope and link the resutl to the main query.
    /// </summary>
    /// <exception cref="ApplicationException"></exception>
    void CloseAnyScope();
    /// <summary>
    /// Open a scope where handling the ALL operator. This open a sub-query where all operations are perfomed until the scope is closed.
    /// </summary>
    void OpenAllScope();
    /// <summary>
    /// Close the current ALL scope and link the resutl to the main query.
    /// </summary>
    /// <exception cref="ApplicationException"></exception>
    void CloseAllScope();
    /// <summary>
    /// Open a special scope that create a new <see cref="ICompilerContext"/> and used to create as subcontext. Used when parsing Expand clauses.
    /// </summary>
    /// <returns></returns>
    ICompilerContext OpenExpansionScope(ExpandedNavigationSelectItem i);
    /// <summary>
    /// Close the current expation scope and join back the information to the parent context.
    /// </summary>
    /// <returns></returns>
    void CloseExpansionScope(ICompilerContext expansionContext, ExpandedNavigationSelectItem i);

#endregion

#region "query manipulation"
    EdmPath AddFrom(EdmEntityType edmEntityType, EdmPath edmPath);

    EdmPath AddJoin(EdmNavigationProperty rightNavigationProperty, EdmPath rightPath, EdmPath leftPath);
    void AddSelect(EdmPath edmPath, EdmStructuralProperty property, string? customName = null);
    void AddSelectKey(EdmPath? path, EdmEntityType? type);
    void AddSelectAll(EdmPath? path, EdmEntityType? type);
    void AddCount(EdmPath edmPath, EdmStructuralProperty edmStructuralProperty);
    void AddFilter(BinaryFilter filter);
    void AddOrderBy(EdmPath edmPath, EdmStructuralProperty property,OrderByDirection direction );
    void WrapQuery(EdmPath resourcePath);
    void AddLimit(long top);
    void AddOffset(long skip);
#endregion


}
