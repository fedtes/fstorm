using Microsoft.OData.Edm;
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
        public IEdmStructuralProperty Property {get; set;} = null!;
    }

    /// <summary>
    /// Result of accesing a scope variable node such as $it
    /// </summary>
    public class Variable : PathValue
    {
        public IEdmEntityType Type {get; set;} = null!;

        public String Name {get; set;} = null!;

        public PropertyReference GetStructuralProperty(string name) {
            return new PropertyReference() 
            {
                Property = Type.StructuralProperties().First(x => x.Name == name),
                ResourcePath = ResourcePath
            };
        }
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
        public BinaryFilter() {}
        public BinaryFilter(PropertyReference propertyReference, FilterOperatorKind operatorKind, object? value)
        {
            PropertyReference = propertyReference;
            OperatorKind = operatorKind;
            Value = value;
        }
        public PropertyReference PropertyReference {get; set;} = null!;
        public FilterOperatorKind OperatorKind = FilterOperatorKind.Equal;
        public object? Value = null;
    }

    public class PropertyFilter : Filter
    {
        public PropertyFilter() {}
        public PropertyFilter(PropertyReference propertyReference, FilterOperatorKind operatorKind, PropertyReference rightPropertyReference )
        {
            LeftPropertyReference = propertyReference;
            OperatorKind = operatorKind;
            RightPropertyReference = rightPropertyReference;
        }
        public PropertyReference LeftPropertyReference {get; set;} = null!;
        public FilterOperatorKind OperatorKind = FilterOperatorKind.Equal;
        public PropertyReference RightPropertyReference {get; set;} = null!;
    }

    public enum FilterOperatorKind
    {
        /// <summary>
        /// The eq operator.
        /// </summary>
        Equal = 2,

        /// <summary>
        /// The ne operator.
        /// </summary>
        NotEqual = 3,

        /// <summary>
        /// The gt operator.
        /// </summary>
        GreaterThan = 4,

        /// <summary>
        /// The ge operator.
        /// </summary>
        GreaterThanOrEqual = 5,

        /// <summary>
        /// The lt operator.
        /// </summary>
        LessThan = 6,

        /// <summary>
        /// The le operator.
        /// </summary>
        LessThanOrEqual = 7,

        Has=13,
        /// <summary>
        /// The startswith operator.
        /// </summary>
        StartsWith = 14,
        /// <summary>
        /// The endswith operator.
        /// </summary>
        EndsWith = 15,

        /// <summary>
        /// The conatins operator.
        /// </summary>
        Contains = 16

    }

}