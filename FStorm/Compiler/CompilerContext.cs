using Microsoft.OData.UriParser;

namespace FStorm
{

    public enum ResourceType
    {
        Collection,
        Object,
        Property
    }

    /// <summary>
    /// Model passed between compilers.
    /// </summary>
    public class CompilerContext
    {
        /// <summary>
        /// Define metadata of the resource requested via request path.
        /// </summary>
        public class ResourceMetadata
        {
            public ResourceType ResourceType;
            public EdmPath ResourcePath = null!;
            public EdmEntityType? ResourceEdmType;
            public ODataPath ODataPath = null!;
        }

        /// <summary>
        /// Query model result of the compilation
        /// </summary>
        public SqlKata.Query Query = new SqlKata.Query();

        /// <summary>
        /// List of all aliases used in the From clausole
        /// </summary>
        public List<EdmPath> Aliases = new List<EdmPath>();
    }

    /// <summary>
    /// Typed model passed between compilers. Expose ContextData to keep additional or compiler specific information
    /// </summary>
    public class CompilerContext<T> : CompilerContext
    {
        public ResourceMetadata Resource = new ResourceMetadata();

        public T ContextData = default!;

        public CompilerContext<T1> CloneTo<T1>(T1 ContextData)
        {
            CompilerContext<T1> instance = (CompilerContext<T1>)Activator.CreateInstance(typeof(CompilerContext<T1>))!;
            instance = this.CopyTo(instance);
            instance.ContextData = ContextData;
            return instance;
        }

        public CompilerContext<T1> CopyTo<T1>(CompilerContext<T1> newContext)
        {
            newContext.Query = this.Query;
            newContext.Aliases = this.Aliases;
            newContext.Resource.ResourceType = this.Resource.ResourceType;
            newContext.Resource.ResourcePath = this.Resource.ResourcePath;
            newContext.Resource.ResourceEdmType = this.Resource.ResourceEdmType;
            return newContext;
        }
    }



}
