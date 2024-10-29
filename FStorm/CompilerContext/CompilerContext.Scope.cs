

using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace FStorm;

public partial class CompilerContext
{
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
    private CompilerScope ActiveScope { get => scope.First(x => x.ScopeType != CompilerScope.NO_SCOPE); }

    /// <summary>
    /// Return the first "query builder" scope containing a from clasue. This may be the ROOT or a suquery actually processing.
    /// </summary>
    private CompilerScope MainScope { get => scope.First(x => x.ScopeType == CompilerScope.ROOT || x.ScopeType == CompilerScope.ANY || x.ScopeType == CompilerScope.ALL); }

    /// <summary>
    /// Return the first MAIN 
    /// </summary>
    private CompilerScope RootScope { get => scope.First(x => x.ScopeType == CompilerScope.ROOT); }

    /// <summary>
    /// Shortcut to access the underlyng query builder of the <see cref="MainScope"/>.
    /// </summary>
    private IQueryBuilder MainQuery { get => MainScope.Query; }

    /// <summary>
    /// Shortcut to access the underlyng query builder of the <see cref="ActiveScope"/>.
    /// </summary>
    private IQueryBuilder ActiveQuery { get => ActiveScope.Query; }

    private IQueryBuilder RootQuery { get => RootScope.Query; }

    /// <summary>
    /// Open an "AND" scope. While in that the where clauses are in AND relation each others 
    /// </summary>
    void ICompilerContext.OpenAndScope()
    {
        if (ActiveScope.ScopeType != CompilerScope.AND)
        {
            scope.Push(new CompilerScope(CompilerScope.AND, service.serviceProvider.GetService<IQueryBuilder>()!));
        }
        else
        {
            scope.Push(new CompilerScope(CompilerScope.NO_SCOPE, ActiveQuery));
        }
    }

    /// <summary>
    /// Close the current AND scope
    /// </summary>
    void ICompilerContext.CloseAndScope()
    {
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
    void ICompilerContext.OpenOrScope()
    {
        if (ActiveScope.ScopeType != CompilerScope.OR)
        {
            scope.Push(new CompilerScope(CompilerScope.OR, service.serviceProvider.GetService<IQueryBuilder>()!));
        }
        else
        {
            scope.Push(new CompilerScope(CompilerScope.NO_SCOPE, ActiveQuery));
        }
    }

    /// <summary>
    /// Close the current OR scope
    /// </summary>
    void ICompilerContext.CloseOrScope()
    {
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
    /// Open an "NOT" scope. While in that the "where" clauses are wrapped around NOT 
    /// </summary>
    void ICompilerContext.OpenNotScope()
    {
        scope.Push(new CompilerScope(CompilerScope.NOT, service.serviceProvider.GetService<IQueryBuilder>()!));
    }

    /// <summary>
    /// Close the current NOT scope
    /// </summary>
    void ICompilerContext.CloseNotScope()
    {
        var s = scope.Pop();
        (IsOr ? ActiveQuery.Or() : ActiveQuery).Not().Where(_q => s.Query);
    }


    /// <summary>
    /// Open a variable scope by pushing a variable into the current context. Variable are visibile from all "children" scope opened from here.
    /// </summary>
    /// <param name="variable"></param>
    void ICompilerContext.OpenVariableScope(Variable variable)
    {
        var s = new CompilerScope(CompilerScope.VARIABLE, ActiveQuery, variable);
        scope.Push(s);
    }

    /// <summary>
    /// Close current variable scope and pop the variable out of the visibility, destroying it.
    /// </summary>
    /// <exception cref="ApplicationException"></exception>
    void ICompilerContext.CloseVariableScope()
    {
        var s = scope.Pop();
        if (s.ScopeType != CompilerScope.VARIABLE)
            throw new ApplicationException("Should not pass here!!");
    }

    /// <summary>
    /// Open a scope where handling the ANY operator. This open a sub-query where all operations are perfomed until the scope is closed.
    /// </summary>
    void ICompilerContext.OpenAnyScope()
    {
        var anyQ = service.serviceProvider.GetService<IQueryBuilder>()!;
        var s = new CompilerScope(CompilerScope.ANY, anyQ, new AliasStore("ANY"));
        scope.Push(s);
        Variable _it = ((ICompilerContext)this).GetVariablesInScope().First(x => x.Name == "$it");
        var subQueryRoot = CreateSubQuery(_it);

        if (subQueryRoot.Item2 is EdmNavigationProperty navigationProperty)
        {
            var (sourceProperty, targetProperty) = navigationProperty.GetRelationProperties();
            var alias_var = ((ICompilerContext)this).Aliases.AddOrGet(subQueryRoot.Item1);
            var alias_it = RootScope.Aliases.AddOrGet(_it.ResourcePath);
            ActiveQuery.WhereColumns($"{alias_var}.{targetProperty.columnName}", "=", $"{alias_it}.{sourceProperty.columnName}");
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
    void ICompilerContext.CloseAnyScope()
    {
        var s = scope.Pop();
        if (s.ScopeType != CompilerScope.ANY) throw new ApplicationException("Should not pass here!!");
        (IsOr ? ActiveQuery.Or() : ActiveQuery).WhereExists(s.Query);
    }


    /// <summary>
    /// Open a scope where handling the ALL operator. This open a sub-query where all operations are perfomed until the scope is closed.
    /// </summary>
    void ICompilerContext.OpenAllScope()
    {
        var anyQ = service.serviceProvider.GetService<IQueryBuilder>()!;
        var s = new CompilerScope(CompilerScope.ALL, anyQ, new AliasStore("ALL"));
        scope.Push(s);
        Variable _it = ((ICompilerContext)this).GetVariablesInScope().First(x => x.Name == "$it");
        var subQueryRoot = CreateSubQuery(_it);

        if (subQueryRoot.Item2 is EdmNavigationProperty navigationProperty)
        {
            var (sourceProperty, targetProperty) = navigationProperty.GetRelationProperties();
            var alias_var = ((ICompilerContext)this).Aliases.AddOrGet(subQueryRoot.Item1);
            var alias_it = RootScope.Aliases.AddOrGet(_it.ResourcePath);
            ActiveQuery.WhereColumns($"{alias_var}.{targetProperty.columnName}", "=", $"{alias_it}.{sourceProperty.columnName}");
        }
        else
        {
            throw new ApplicationException("should not pass here!!");
        }
    }

    /// <summary>
    /// Close the current ALL scope and link the resutl to the main query.
    /// </summary>
    /// <exception cref="ApplicationException"></exception>
    void ICompilerContext.CloseAllScope()
    {
        var s = scope.Pop();
        if (s.ScopeType != CompilerScope.ALL) throw new ApplicationException("Should not pass here!!");
        (IsOr ? ActiveQuery.Or() : ActiveQuery).Not().WhereExists(s.Query);
    }

    ICompilerContext ICompilerContext.OpenExpansionScope(ExpandedNavigationSelectItem i)
    {
        ICompilerContext expansionContext = service.serviceProvider.GetService<CompilerContextFactory>()!
            .CreateExpansionContext(i.PathToNavigationProperty, i.FilterOption, i.SelectAndExpand, i.OrderByOption, new PaginationClause(i.TopOption, i.SkipOption));
        expansionContext.SetOutputPath(((ICompilerContext)this).GetOutputPath()! + i.PathToNavigationProperty.FirstSegment.Identifier);
        var s = new CompilerScope(CompilerScope.EXPAND, null);
        scope.Push(s);
        return expansionContext;
    }

    void ICompilerContext.CloseExpansionScope(ICompilerContext expansionContext, ExpandedNavigationSelectItem i)
    {

        var s = scope.Pop();
        if (s.ScopeType != CompilerScope.EXPAND || expansionContext is not ExpansionCompilerContext) throw new ApplicationException("Should not pass here!!");

        var name = expansionContext.GetOutputPath()!.ToString().Replace("~", "$expand");
        var (sourceProperty, targetProperty) = (((NavigationPropertySegment)i.PathToNavigationProperty.FirstSegment).NavigationProperty as EdmNavigationProperty)!.GetRelationProperties();

        // ensure select
        string localKeyAlias = name + "/:fkey";
        ((ICompilerContext)this).AddSelect(((ICompilerContext)this).GetOutputPath()!, sourceProperty, localKeyAlias);
        string foreignKeyAlias = name + "/:fkey";
        expansionContext.AddSelect(expansionContext.GetOutputPath()!, targetProperty, foreignKeyAlias);
        subcontextes.Add(name, expansionContext);
    }
}


