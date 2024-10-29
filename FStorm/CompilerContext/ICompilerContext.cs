using System;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace FStorm;

public interface ICompilerContext
{
    internal string UriRequest { get; }
    /// <summary>
    /// Keeps track of the aliases used in the query and the meaning they have.
    /// </summary>
    internal AliasStore Aliases { get; }
    /// <summary>
    /// Ask the underline query builder to produce an executable query.
    /// </summary>
    /// <returns></returns>
    internal SQLCompiledQuery Compile();
    internal ODataPath GetOdataRequestPath();
    internal FilterClause GetFilterClause();
    internal SelectExpandClause GetSelectAndExpand();
    internal PaginationClause GetPaginationClause();
    internal OrderByClause GetOrderByClause();
    internal string GetSkipToken();
    internal OutputKind GetOutputKind();
    internal EdmPath? GetOutputPath();
    internal void SetOutputKind(OutputKind OutputType);
    internal void SetOutputPath(EdmPath ResourcePath);
    internal bool HasFrom();
    internal EdmEntityType? GetOutputType();
    internal void SetOutputType(EdmEntityType? ResourceEdmType);
    internal IQueryBuilder GetQuery();
    internal List<Variable> GetVariablesInScope();
    internal Variable? GetCurrentVariableInScope();
    internal ICompilerContext GetSubContext(string name);
    internal bool HasSubContext();
    internal IDictionary<string, ICompilerContext> GetSubContextes();
#region "scopes"
    /// <summary>
    /// Open an "AND" scope. While in that the where clauses are in AND relation each others 
    /// </summary>
    internal void OpenAndScope();
    /// <summary>
    /// Close the current AND scope
    /// </summary>
    internal void CloseAndScope();
    /// <summary>
    /// Open an "OR" scope. While in that the where clauses are in OR relation each others 
    /// </summary>
    internal void OpenOrScope();
     /// <summary>
    /// Close the current OR scope
    /// </summary>
    internal void CloseOrScope();
    /// <summary>
    /// Open an "NOT" scope. While in that the "where" clauses are wrapped around NOT 
    /// </summary>
    internal void OpenNotScope();
    /// <summary>
    /// Close the current NOT scope
    /// </summary>
    internal void CloseNotScope();
     /// <summary>
    /// Open a variable scope by pushing a variable into the current context. Variable are visibile from all "children" scope opened from here.
    /// </summary>
    /// <param name="variable"></param>
    internal void OpenVariableScope(Variable variable);
    /// <summary>
    /// Close current variable scope and pop the variable out of the visibility, destroying it.
    /// </summary>
    /// <exception cref="ApplicationException"></exception>
    internal void CloseVariableScope();
    /// <summary>
    /// Open a scope where handling the ANY operator. This open a sub-query where all operations are perfomed until the scope is closed.
    /// </summary>
    internal void OpenAnyScope();
    /// <summary>
    /// Close the current ANY scope and link the resutl to the main query.
    /// </summary>
    /// <exception cref="ApplicationException"></exception>
    internal void CloseAnyScope();
    /// <summary>
    /// Open a scope where handling the ALL operator. This open a sub-query where all operations are perfomed until the scope is closed.
    /// </summary>
    internal void OpenAllScope();
    /// <summary>
    /// Close the current ALL scope and link the resutl to the main query.
    /// </summary>
    /// <exception cref="ApplicationException"></exception>
    internal void CloseAllScope();
    /// <summary>
    /// Open a special scope that create a new <see cref="ICompilerContext"/> and used to create as subcontext. Used when parsing Expand clauses.
    /// </summary>
    /// <returns></returns>
    internal ICompilerContext OpenExpansionScope(ExpandedNavigationSelectItem i);
    /// <summary>
    /// Close the current expation scope and join back the information to the parent context.
    /// </summary>
    /// <returns></returns>
    internal void CloseExpansionScope(ICompilerContext expansionContext, ExpandedNavigationSelectItem i);

#endregion

#region "query manipulation"
    internal EdmPath AddFrom(EdmEntityType edmEntityType, EdmPath edmPath);

    internal EdmPath AddJoin(EdmNavigationProperty rightNavigationProperty, EdmPath rightPath, EdmPath leftPath);
    internal void AddSelect(EdmPath edmPath, EdmStructuralProperty property, string? customName = null);
    internal void AddSelectKey(EdmPath? path, EdmEntityType? type);
    internal void AddSelectAll(EdmPath? path, EdmEntityType? type);
    internal void AddCount(EdmPath edmPath, EdmStructuralProperty edmStructuralProperty);
    internal void AddFilter(BinaryFilter filter);
    internal void AddOrderBy(EdmPath edmPath, EdmStructuralProperty property,OrderByDirection direction );
    internal void WrapQuery(EdmPath resourcePath);
    internal void AddLimit(long top);
    internal void AddOffset(long skip);
#endregion


}
