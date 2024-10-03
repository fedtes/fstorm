using Microsoft.OData.Edm;
using System.Collections;

namespace FStorm
{
    public class EdmSegment
    {
        public string Identifier { get; }

        internal EdmSegment(string identifier) 
        {
            if (String.IsNullOrEmpty(identifier)) { throw new ArgumentNullException("identifier"); }
            if (identifier.Contains("/")) { throw new ArgumentException("Identifier cannot have / character"); }
            Identifier = identifier;
        }

        public override string ToString() => Identifier;
    }

    /// <summary>
    /// Describe the path from a starting Entity to a specific property throught all the the navigation steps across the EntityDataModel (<see cref="EdmModel"/>)
    /// </summary>
    public class EdmPath : IEnumerable<EdmSegment>
    {

        public const string PATH_ROOT = "~";
        protected List<EdmSegment> _segments;
        protected readonly FStormService fStormService;
#region "costructors"
        internal EdmPath(FStormService fStormService)
        {
            this.fStormService = fStormService;
            _segments = new List<EdmSegment>();
        }

        internal EdmPath(FStormService fStormService, params string[] segments)
        {
            _segments = segments.Select(x => new EdmSegment(x)).ToList();
            this.fStormService = fStormService;
        }

        internal EdmPath(FStormService fStormService, params EdmSegment[] segments)
        {
            this.fStormService = fStormService;
            _segments = segments.ToList();
        }
#endregion

#region "Operators"
        public static EdmPath operator +(EdmPath x, EdmSegment y)
        {
            var x1 = x.Clone();
            x1._segments.Add(y);
            return x1;
        }

        public static EdmPath operator -(EdmPath x, int segmentCount)
        {
            if (segmentCount > x._segments.Count) { throw new ArgumentException($"Cannot subtract more than {x._segments.Count} segments."); }
            if (segmentCount == x._segments.Count) return x.Clone();
            var x1 = x.Clone();
            x1._segments = x1._segments.Take(x1._segments.Count - segmentCount).ToList();
            return x1;
        }

        public static EdmPath operator -(EdmPath x, EdmPath y) {
            if (x.Count() <= y.Count()) throw new ArgumentException("Left operand must be creater than right operand");
            int i=0;
            while (i < y.Count() && x._segments[i].Identifier == y._segments[i].Identifier)
            {
                i++;
            }
            var x1 = x.Clone();
            x1._segments = x1._segments.Skip(i).ToList();
            return x1;
        }

        public static EdmPath operator +(EdmPath x, string segment) => x + new EdmSegment(segment);

        public static bool operator ==(EdmPath x, EdmPath y) => x.ToString().Equals(y.ToString());

        public static bool operator !=(EdmPath x, EdmPath y) => !(x==y);
#endregion
        
#region "IEnumerable"

        public IEnumerator<EdmSegment> GetEnumerator()
        {
            return ((IEnumerable<EdmSegment>)_segments).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _segments.GetEnumerator();
        }

#endregion
        
#region "Equals"
        public override bool Equals(object? obj) => obj != null && GetType() == obj.GetType() && ToString().Equals(obj.ToString());

        public override int GetHashCode() => ToString().GetHashCode();
#endregion

        /// <summary>
        /// Return the primitive type kind of the property that refers to this path if it is a structural property else return <see cref="EdmPrimitiveTypeKind.None"/>
        /// </summary>
        /// <returns></returns>
        public EdmPrimitiveTypeKind GetTypeKind()
        {
            if (IsPathToKey()) {
                EdmEntityType entityType = GetContainerType()!;
                var _key = entityType.DeclaredKey.First();
                return _key.Type switch {
                     EdmPrimitiveTypeReference primitive => primitive.PrimitiveKind(),
                        _ => EdmPrimitiveTypeKind.None
                };

            } else {
                var result = EdmPrimitiveTypeKind.None;
                var entityType = GetContainerType();
                if (entityType == null) return EdmPrimitiveTypeKind.None;

                var l =_segments.Last().ToString();
                var prop = entityType.Properties().FirstOrDefault(x => x.Name == l);
                if (prop != null && prop.PropertyKind == EdmPropertyKind.Structural)
                {
                    var _type = (prop as IEdmStructuralProperty)!.Type;
                    result = _type switch
                    {
                        EdmPrimitiveTypeReference primitive => primitive.PrimitiveKind(),
                        _ => EdmPrimitiveTypeKind.None
                    };
                }
                else
                {
                    result = EdmPrimitiveTypeKind.None;
                }
                return result;
            }
        }

        /// <summary>
        /// Get the <see cref="EdmEntityType"/> that contains the element defined by this path. Return null if the path is empty.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public EdmEntityType? GetContainerType()
        {
            var _elements = AsEdmElements();
            if (_elements.Count == 0) {
                return null;
            } 
            if (_elements.Count == 1) {
                var (a,b) = _elements[_elements.Count-1];
                if (b is IEdmEntitySet entitySet)
                {
                    return (EdmEntityType)entitySet.EntityType.AsElementType();
                }
                else 
                {
                    throw new InvalidOperationException($"Path {this.ToString()} do not refers to any valid EntityType.");
                }
            } 
            else 
            {
                var (a,b) = _elements[_elements.Count-2];
                if (b is IEdmEntitySet entitySet)
                {
                    return (EdmEntityType)entitySet.EntityType.AsElementType();
                }
                else if (b is IEdmNavigationProperty property) 
                {
                    return property.DeclaringType.EnsureType(this.fStormService);
                } else 
                {
                    throw new InvalidOperationException($"Path {this.ToString()} do not refers to any valid EntityType.");
                }
            }
        }

        /// <summary>
        /// Trasform the current path into an ordered list of tuples where each one is the subpath (<see cref="EdmPath"/>) and the corrispondig <see cref="IEdmElement"/> described in the <see cref="EdmModel"/>.
        /// </summary>
        /// <returns></returns>
        public List<(EdmPath, IEdmElement)> AsEdmElements()
        {
            List<(EdmPath, IEdmElement)> result =new List<(EdmPath, IEdmElement)>();
            for (int i = 0; i < this.Count(); i++)
            {
                if (i == 0)
                {
                    var ns = fStormService.Model.DeclaredNamespaces.First();
                    result.Add((new EdmPath(fStormService,_segments.Take(i + 1).ToArray()), fStormService.Model.FindDeclaredEntitySet(_segments[i].ToString())));
                }
                else if (result.Last().Item2 is IEdmEntitySet)
                {
                    var _last = (IEdmEntitySet)result.Last().Item2;
                    var _prop = (_last.EntityType.AsElementType() as EdmEntityType)!.FindProperty(_segments[i].ToString());
                    result.Add((new EdmPath(fStormService,_segments.Take(i + 1).ToArray()), _prop));
                }
                else if (result.Last().Item2 is IEdmNavigationProperty) {
                    var _last = (IEdmNavigationProperty)result.Last().Item2;
                    var _prop = (_last.ToEntityType().AsElementType() as EdmEntityType)!.FindProperty(_segments[i].ToString());
                    result.Add((new EdmPath(fStormService,_segments.Take(i + 1).ToArray()), _prop));
                } 
                else if (result.Last().Item2 is IEdmStructuralProperty)
                {
                    // do nothing
                }
            }
            return result;
        }

        public bool IsPathToKey() =>  _segments.Last().Identifier == ":key";
        public EdmPath Clone() => new EdmPath(fStormService, this._segments.ToArray());

        public override string ToString() => EdmPath.PATH_ROOT + "/" + String.Join("/", _segments.Select(x => x.ToString()));
    }

    /// <summary>
    /// Expose method to create <see cref="EdmPath"/>
    /// </summary>
    public class EdmPathFactory
    {
        private readonly FStormService fStormService;

        public EdmPathFactory(FStormService fStormService)
        {
            this.fStormService = fStormService;
        }
        /// <summary>
        /// Create an empty <see cref="EdmPath"/>
        /// </summary>
        /// <returns></returns>
        public EdmPath CreatePath() => new EdmPath(fStormService);
        /// <summary>
        /// Create and initialize an <see cref="EdmPath"/> from the segments passed as argument.
        /// </summary>
        /// <param name="segments">from left (path start) to right (path end)</param>
        /// <returns></returns>
        public EdmPath CreatePath(params string[] segments) => new EdmPath(fStormService, segments);
        /// <summary>
        /// Create and initialize an <see cref="EdmPath"/> from the segments passed as argument.
        /// </summary>
        /// <param name="segments">from left (path start) to right (path end)</param>
        /// <returns></returns>
        public EdmPath CreatePath(params EdmSegment[] segments) => new EdmPath(fStormService, segments);
        
        /// <summary>
        /// Try parse a string into a <see cref="EdmPath"/>
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public EdmPath ParseString(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (path.StartsWith(EdmPath.PATH_ROOT))
            {
                return new EdmPath(fStormService, path.Substring(2).Split("/"));
            }

            throw new ArgumentException("Invalid path");
        }

        /// <summary>
        /// Create an <see cref="EdmPath"/> from a <see cref="IEdmNavigationSource"/> by following the hierarchy till the root.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public EdmPath FromNavigationSource(IEdmNavigationSource source) {

            List<(IEdmNavigationSource,string)> segments = new List<(IEdmNavigationSource,string)>();
            IEdmNavigationSource cursor = source;

            while (cursor is IEdmContainedEntitySet)
            {
                var x = (IEdmContainedEntitySet)cursor;
                segments.Insert(0, (x, x.Name));
                cursor = x.ParentNavigationSource;
            }

            segments.Insert(0,(cursor, cursor.Name));
            return CreatePath(segments.Select(x=> x.Item2).ToArray());

        }

    }

}