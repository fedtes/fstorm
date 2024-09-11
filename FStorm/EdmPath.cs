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

    public abstract class EdmPath : IEnumerable<EdmSegment>
    {

        public const string PATH_ROOT = "~";
        protected List<EdmSegment> _segments;
        protected readonly FStormService fStormService;

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

#region "Operators"
        public static EdmPath operator +(EdmPath x, EdmSegment y)
        {
            var x1 = x.Clone();
            x1._segments.Add(y);
            return x1;
        }

        public static EdmPath operator -(EdmPath x, int segmentCount)
        {
            if (segmentCount >= x._segments.Count) { throw new ArgumentException($"Cannot subtract more than {x._segments.Count} segments."); }
            var x1 = x.Clone();
            x1._segments = x1._segments.Take(x1._segments.Count - segmentCount).ToList();
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
        public abstract EdmPrimitiveTypeKind GetTypeKind();
        public abstract bool IsPathToKey();
        public abstract EdmPath Clone();
        public override string ToString() => String.Join("/", _segments.Select(x => x.ToString()));
    }

    public class EdmResourcePath : EdmPath
    {
        internal EdmResourcePath(FStormService fStormService) : base(fStormService) { }
        internal EdmResourcePath(FStormService fStormService, params string[] segments) : base(fStormService,segments) { }

        internal EdmResourcePath(FStormService fStormService, params EdmSegment[] segments) : base(fStormService,segments) { }

        public override EdmPath Clone() => new EdmResourcePath(fStormService, this._segments.ToArray());

        public override string ToString() =>  EdmPath.PATH_ROOT + "/" + base.ToString();

        public override bool IsPathToKey() => _segments.Last().Identifier == ":key";

        private EdmEntityType GetContainerType()
        {
            EdmEntityType? entityType = null;
            for (int i = 0; i < this.Count()-1; i++)
            {
                if (i == 0)
                {
                    var ns = fStormService.Model.DeclaredNamespaces.First();
                    entityType = (EdmEntityType?)fStormService.Model.FindDeclaredEntitySet(_segments[i].ToString()).Type.AsElementType();
                }
                else
                {
                    var navProp = entityType.DeclaredNavigationProperties().First(x => x.Name == _segments[i].ToString());
                    entityType = (EdmEntityType?)navProp.Type.ToStructuredType()!;
                }
            }
            if (entityType == null)
                throw new InvalidOperationException($"Path {this.ToString()} do not refers to any valid EntityType.");
            else
                return entityType;
        }

        public override EdmPrimitiveTypeKind GetTypeKind()
        {
            if (IsPathToKey()) {
                EdmEntityType entityType = GetContainerType();
                var _key = entityType.DeclaredKey.First();
                return _key.Type switch {
                     EdmPrimitiveTypeReference primitive => primitive.PrimitiveKind(),
                        _ => EdmPrimitiveTypeKind.None
                };

            } else {
                var result = EdmPrimitiveTypeKind.None;
                var entityType = GetContainerType();
                var prop = entityType.Properties().First(x => x.Name == _segments.Last().ToString());
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
    }

    public class EdmPathFactory
    {
        private readonly FStormService fStormService;

        public EdmPathFactory(FStormService fStormService)
        {
            this.fStormService = fStormService;
        }
        public EdmResourcePath CreateResourcePath() => new EdmResourcePath(fStormService);
        public EdmResourcePath CreateResourcePath(params string[] segments) => new EdmResourcePath(fStormService, segments);
        public EdmResourcePath CreateResourcePath(params EdmSegment[] segments) => new EdmResourcePath(fStormService, segments);

        public EdmPath Parse(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (path.StartsWith(EdmPath.PATH_ROOT))
            {
                return new EdmResourcePath(fStormService, path.Substring(2).Split("/"));
            }

            throw new ArgumentException("Invalid path");
        }
    }

}