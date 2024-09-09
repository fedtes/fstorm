using Microsoft.OData.UriParser;

namespace FStorm
{

    public enum OutputType
    {
        Collection,
        Object,
        Property,
        RawValue
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
            public OutputType OutputType;
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

        public ResourceMetadata Resource = new ResourceMetadata();
    }

}
