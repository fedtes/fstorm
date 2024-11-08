using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.UriParser;

namespace FStorm;

public class SubContextSupportContext : ISubContextSupportContext
{
    internal SubContextSupportContext(ICompilerContext parentContext,ODataService service, Action<CompilerScope> push, Func<CompilerScope> pop){
        this.parentContext = parentContext;
        this.service = service;
        this.push = push;
        this.pop = pop;
    }

    private Dictionary<string, ICompilerContext> subcontextes = new Dictionary<string, ICompilerContext>();
    private readonly ICompilerContext parentContext;
    private readonly ODataService service;
    private readonly Action<CompilerScope> push;
    private readonly Func<CompilerScope> pop;

    public ICompilerContext GetSubContext(string name)
    {
        if (subcontextes.ContainsKey(name))
            return subcontextes[name];
        else
            throw new ArgumentException($"Subcontext with name {name} not found", nameof(name));
    }

    public bool HasSubContext() => subcontextes.Any();

    public  IDictionary<string, ICompilerContext> GetSubContextes() => subcontextes;

    
    public ICompilerContext OpenExpansionScope(ExpandedNavigationSelectItem i)
    {
        ICompilerContext expansionContext = service.serviceProvider.GetService<CompilerContextFactory>()!
            .CreateExpansionContext(i.PathToNavigationProperty, i.FilterOption, i.SelectAndExpand, i.OrderByOption, new PaginationClause(i.TopOption, i.SkipOption));
        expansionContext.SetOutputPath(parentContext.GetOutputPath()! + i.PathToNavigationProperty.FirstSegment.Identifier);
        var s = new CompilerScope(CompilerScope.EXPAND);
        this.push(s);
        return expansionContext;
    }

    public void CloseExpansionScope(ICompilerContext expansionContext, ExpandedNavigationSelectItem i)
    {

        var s = this.pop();
        if (s.ScopeType != CompilerScope.EXPAND || expansionContext is not ExpansionCompilerContext) throw new ApplicationException("Should not pass here!!");

        var name = expansionContext.GetOutputPath()!.ToString().Replace("~", "$expand");
        var (sourceProperty, targetProperty) = (((NavigationPropertySegment)i.PathToNavigationProperty.FirstSegment).NavigationProperty as EdmNavigationProperty)!.GetRelationProperties();

        // ensure select
        string localKeyAlias = name + "/:fkey";
        parentContext.AddSelect(parentContext.GetOutputPath()!, sourceProperty, localKeyAlias);
        string foreignKeyAlias = name + "/:fkey";
        expansionContext.AddSelect(expansionContext.GetOutputPath()!, targetProperty, foreignKeyAlias);
        subcontextes.Add(name, expansionContext);
    }

}
