

using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;

namespace FStorm;

public partial class QueryBuilderContext
{
    /// <summary>
    /// Stack tracking various types of <see cref="CompilerScope"/> during the semantic parsing done by <see cref="SemanticVisitor"/>.
    /// </summary>
    /// <remarks>
    /// Each scope may contains various information about variables, subquery, boolean operations, lambdas etc..
    /// </remarks>
    private Stack<CompilerScope> scopes = new Stack<CompilerScope>();

    public void Push(CompilerScope scope) => this.scopes.Push(scope);

    public  CompilerScope Pop() => scopes.Pop();

    /// <summary>
    /// Return the first "meaningfull" scope of the stack. This could be anything and do not rely on it to access any from/join clasue.
    /// </summary>
    public CompilerScope ActiveScope { get => scopes.First(x => x.ScopeType != CompilerScope.NO_SCOPE); }

    /// <summary>
    /// Return the first "query builder" scope containing a from clasue. This may be the ROOT or a suquery actually processing.
    /// </summary>
    protected CompilerScope MainScope { get => scopes.First(x => x.ScopeType == CompilerScope.ROOT || x.ScopeType == CompilerScope.ANY || x.ScopeType == CompilerScope.ALL); }

    /// <summary>
    /// Return the first MAIN 
    /// </summary>
    protected CompilerScope RootScope { get => scopes.First(x => x.ScopeType == CompilerScope.ROOT); }

    /// <summary>
    /// Shortcut to access the underlyng query builder of the <see cref="MainScope"/>.
    /// </summary>
    protected IQueryBuilder MainQuery { get => MainScope.Query is null ? throw new ArgumentNullException("MainScope.Query") : MainScope.Query; }

    /// <summary>
    /// Shortcut to access the underlyng query builder of the <see cref="ActiveScope"/>.
    /// </summary>
    protected IQueryBuilder ActiveQuery { get => ActiveScope.Query is null ? throw new ArgumentNullException("ActiveScope.Query") : ActiveScope.Query; }

    /// <summary>
    /// Open an "AND" scope. While in that the where clauses are in AND relation each others 
    /// </summary>
    public void OpenAndScope()
    {
        if (ActiveScope.ScopeType != CompilerScope.AND)
        {
            scopes.Push(new CompilerScope(CompilerScope.AND, service.serviceProvider.GetService<IQueryBuilder>()!));
        }
        else
        {
            scopes.Push(new CompilerScope(CompilerScope.NO_SCOPE, ActiveQuery));
        }
    }

    /// <summary>
    /// Close the current AND scope
    /// </summary>
    public void CloseAndScope()
    {
        if (scopes.Peek().ScopeType == CompilerScope.AND)
        {
            var s = scopes.Pop();
            (IsOr ? ActiveQuery.Or() : ActiveQuery).Where(_q => s.Query is null ? throw new ArgumentNullException("Query") : s.Query);
        }
        else
        {
            scopes.Pop();
        }
    }

    /// <summary>
    /// Open an "OR" scope. While in that the where clauses are in OR relation each others 
    /// </summary>
    public void OpenOrScope()
    {
        if (ActiveScope.ScopeType != CompilerScope.OR)
        {
            scopes.Push(new CompilerScope(CompilerScope.OR, service.serviceProvider.GetService<IQueryBuilder>()!));
        }
        else
        {
            scopes.Push(new CompilerScope(CompilerScope.NO_SCOPE, ActiveQuery));
        }
    }

    /// <summary>
    /// Close the current OR scope
    /// </summary>
    public void CloseOrScope()
    {
        if (scopes.Peek().ScopeType == CompilerScope.OR)
        {
            var s = scopes.Pop();
            (IsOr ? ActiveQuery.Or() : ActiveQuery).Where(_q => s.Query is null ? throw new ArgumentNullException("Query") : s.Query);
        }
        else
        {
            scopes.Pop();
        }
    }

    /// <summary>
    /// Open an "NOT" scope. While in that the "where" clauses are wrapped around NOT 
    /// </summary>
    public void OpenNotScope()
    {
        scopes.Push(new CompilerScope(CompilerScope.NOT, service.serviceProvider.GetService<IQueryBuilder>()!));
    }

    /// <summary>
    /// Close the current NOT scope
    /// </summary>
    public void CloseNotScope()
    {
        var s = scopes.Pop();
        (IsOr ? ActiveQuery.Or() : ActiveQuery).Not().Where(_q => s.Query is null ? throw new ArgumentNullException("Query") : s.Query);
    }


    /// <summary>
    /// Open a variable scope by pushing a variable into the current context. Variable are visibile from all "children" scope opened from here.
    /// </summary>
    /// <param name="variable"></param>
    public void OpenVariableScope(Variable variable)
    {
        var s = new CompilerScope(CompilerScope.VARIABLE, ActiveQuery, variable);
        scopes.Push(s);
    }

    /// <summary>
    /// Close current variable scope and pop the variable out of the visibility, destroying it.
    /// </summary>
    /// <exception cref="ApplicationException"></exception>
    public void CloseVariableScope()
    {
        var s = scopes.Pop();
        if (s.ScopeType != CompilerScope.VARIABLE)
            throw new ApplicationException("Should not pass here!!");
    }

    public List<Variable> GetVariablesInScope() => scopes.Where(x => x.ScopeType == CompilerScope.VARIABLE)
            .Select(x => x.Variable)
            .Where(x => x != null)
            .Cast<Variable>()
            .ToList();

    public Variable? GetCurrentVariableInScope() => scopes.FirstOrDefault(x => x.ScopeType == CompilerScope.VARIABLE)?.Variable;

    /// <summary>
    /// Open a scope where handling the ANY operator. This open a sub-query where all operations are perfomed until the scope is closed.
    /// </summary>
    public void OpenAnyScope()
    {
        var anyQ = service.serviceProvider.GetService<IQueryBuilder>()!;
        var s = new CompilerScope(CompilerScope.ANY, anyQ, new AliasStore("ANY"));
        scopes.Push(s);
        Variable _it = this.GetVariablesInScope().First(x => x.Name == "$it");
        var subQueryRoot = CreateSubQuery(_it);

        if (subQueryRoot.Item2 is EdmNavigationProperty navigationProperty)
        {
            var (sourceProperty, targetProperty) = navigationProperty.GetRelationProperties();
            var alias_var = this.Aliases.AddOrGet(subQueryRoot.Item1);
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
    public void CloseAnyScope()
    {
        var s = scopes.Pop();
        if (s.ScopeType != CompilerScope.ANY) throw new ApplicationException("Should not pass here!!");
        (IsOr ? ActiveQuery.Or() : ActiveQuery).WhereExists(s.Query is null ? throw new ArgumentNullException("Query") : s.Query);
    }


    /// <summary>
    /// Open a scope where handling the ALL operator. This open a sub-query where all operations are perfomed until the scope is closed.
    /// </summary>
    public void OpenAllScope()
    {
        var anyQ = service.serviceProvider.GetService<IQueryBuilder>()!;
        var s = new CompilerScope(CompilerScope.ALL, anyQ, new AliasStore("ALL"));
        scopes.Push(s);
        Variable _it = this.GetVariablesInScope().First(x => x.Name == "$it");
        var subQueryRoot = CreateSubQuery(_it);

        if (subQueryRoot.Item2 is EdmNavigationProperty navigationProperty)
        {
            var (sourceProperty, targetProperty) = navigationProperty.GetRelationProperties();
            var alias_var = this.Aliases.AddOrGet(subQueryRoot.Item1);
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
    public void CloseAllScope()
    {
        var s = scopes.Pop();
        if (s.ScopeType != CompilerScope.ALL) throw new ApplicationException("Should not pass here!!");
        (IsOr ? ActiveQuery.Or() : ActiveQuery).Not().WhereExists(s.Query is null ? throw new ArgumentNullException("Query") : s.Query);
    }

    private (EdmPath, IEdmElement) CreateSubQuery(Variable _it)
    {
        var _var = this.GetCurrentVariableInScope()!;
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
                this.AddFrom((EdmEntityType)type.Type.Definition.AsElementType(), _varElements[i].Item1);
            }
            else
            {
                EdmNavigationProperty type = (EdmNavigationProperty)_varElements[i].Item2;
                this.AddJoin(type, _varElements[i].Item1 - 1, _varElements[i].Item1);
            }
        }

        return _varElements[_it.ResourcePath.Count()];
    }
}


