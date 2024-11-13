using System;

namespace FStorm;


internal class OnPropertyNavigationContext : BasePluginContext, IOnPropertyNavigationContext
{
    private List<PropertyFilter> _JoinCondition;
    public OnPropertyNavigationContext(CompilerContextFactory contextFactory, IEntityAccessContext accessContext) 
    { 
        _JoinCondition = new List<PropertyFilter>();
        _accessContext = (EntityAccessContext)accessContext;
    }

    internal EntityAccessContext _accessContext;

    internal Func<IJoinCondition, IJoinCondition> GetJoinCondition() 
    {
        return (j) => {
            _JoinCondition.ForEach(jc => { 
                j.On(
                    $"{LeftAlias}.{((EdmStructuralProperty)jc.LeftPropertyReference.Property).columnName}",
                    $"{RightAlias}.{((EdmStructuralProperty)jc.RightPropertyReference.Property).columnName}",
                    "="
                );
            });
            return j;
        };
    }

    public string RightAlias;
    public Variable Right {get; set;} = null!;
    public string LeftAlias;
    public Variable Left {get; set;} = null!;
    public string Kind { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public bool CustomizedJoin {get; set; }

    public IEntityAccessContext AccessContext => _accessContext;

    public List<PropertyFilter> JoinCondition => _JoinCondition;

    internal IQueryBuilder GetNestedQuery() => ((EntityAccessContext)AccessContext).GetNestedQuery();

    //public String GetTableString() => AccessContext.GetTableString();

    // public void SetTableString(string table)
    // {
    //     AccessContext.SetTableString(table);
    // }
}

public interface IOnPropertyNavigationContext
{

    IEntityAccessContext AccessContext { get; }

    /// <summary>
    /// Variable referencing the owner of the current navigation property has been navigated
    /// </summary>
    Variable Right { get; }

    /// <summary>
    /// Variable referencing the target object of the current navigation property has been navigated
    /// </summary>
    Variable Left { get; }

    /// <summary>
    /// List of condition used for the join. Conditions are all in AND relation.
    /// </summary>
    List<PropertyFilter> JoinCondition {get; }

    // /// <summary>
    // /// Get current access table only if the access direct to the table.
    // /// </summary>
    // /// <returns></returns>
    // String GetTableString();

    // /// <summary>
    // /// Overwrite the current access table. It will be used instead of the one defined in the EdmModel. Works only if <see cref="Kind"/> equals to <see cref="TABLE_STRING"/>.
    // /// </summary>
    // void SetTableString(string table);

    // /// <summary>
    // /// Define the type of entity access. Possible values: <see cref="TABLE_STRING"/> or <see cref="NEST_QUERY"/>. Default value to <see cref="TABLE_STRING"/>
    // /// </summary>
    // string Kind {get; set;}
    // public const string TABLE_STRING = "TABLE_STRING";
    // public const string NEST_QUERY = "NEST_QUERY";
    public bool CustomizedJoin {get; set;}
}



public interface IOnPropertyNavigation
{
    string EntityName {get; }
    string PropertyName {get; }

    void OnNavigation(IOnPropertyNavigationContext navigationContext);
}