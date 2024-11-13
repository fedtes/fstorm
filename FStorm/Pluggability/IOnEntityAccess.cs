namespace FStorm;

public interface IOnEntityAccess
{
    /// <summary>
    /// fully namespaced name of the entity where this plugin should execute on.
    /// </summary>
    string EntityName { get; }

    void OnAccess(IEntityAccessContext accessContext);

}
