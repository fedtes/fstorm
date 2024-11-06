using Microsoft.OData.UriParser;

namespace FStorm
{

    /// <summary>
    /// Base class of all result types of visiting a specific node of OData request.
    /// </summary>
    public class VisitResult { }

    
    /// <summary>
    /// Result of visiting a ConstantNode
    /// </summary>
    public class ConstantValue : VisitResult
    {
        /// <summary>
        /// Node value
        /// </summary>
        public object? Value {get; set;}
    }

    /// <summary>
    /// Result of the navigation of ResourcePaths
    /// </summary>
    public class PathValue : VisitResult
    {
        public EdmPath ResourcePath {get; set;} = null!;
    }

    /// <summary>
    /// Result of Visiting an access to a StructuralProperty Node
    /// </summary>
    public class PropertyReference : PathValue
    {
        public EdmStructuralProperty Property {get; set;} = null!;
    }

    /// <summary>
    /// Result of accesing a scope variable node such as $it
    /// </summary>
    public class Variable : PathValue
    {
        public EdmEntityType Type {get; set;} = null!;

        public String Name {get; set;} = null!;
    }

    /// <summary>
    /// Base class for all $filter nodes
    /// </summary>
    public class Filter : VisitResult { }

    /// <summary>
    /// Node of $filter rappresenting a binary expression.
    /// </summary>
    public class BinaryFilter : Filter
    {
        public PropertyReference PropertyReference {get; set;} = null!;
        public BinaryOperatorKind OperatorKind = BinaryOperatorKind.Equal;
        public object? Value = null;
    }

}