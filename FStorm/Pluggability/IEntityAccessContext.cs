namespace FStorm;

public interface IEntityAccessContext : IQueryBuilderContext
{
    /// <summary>
    /// Current variable in this evaulation. Rappresent the entity that are going to be acessed.
    /// </summary>
    Variable Me { get; }

    /// <summary>
    /// Get current access table only if the access direct to the table.
    /// </summary>
    /// <returns></returns>
    String GetTableString();

    /// <summary>
    /// Overwrite the current access table. It will be used instead of the one defined in the EdmModel. Works only if <see cref="Kind"/> equals to <see cref="TABLE_STRING"/>.
    /// </summary>
    void SetTableString(string table);

    /// <summary>
    /// Define the type of entity access. Possible values: <see cref="TABLE_STRING"/> or <see cref="NEST_QUERY"/>. Default value to <see cref="TABLE_STRING"/>
    /// </summary>
    string Kind {get; set;}

    public const string TABLE_STRING = "TABLE_STRING";
    public const string NEST_QUERY = "NEST_QUERY";
}
