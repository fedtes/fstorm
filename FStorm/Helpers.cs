using Microsoft.OData.Edm;

namespace FStorm
{
    internal static class Helpers
    {
        public static object? TypeConverter(object? value, EdmPrimitiveTypeKind typeKind)
        {
            if (value == null) return null;

            return typeKind switch
            {
                EdmPrimitiveTypeKind.Int16 => Convert.ToInt16(value),
                EdmPrimitiveTypeKind.Int32=> Convert.ToInt32(value),
                EdmPrimitiveTypeKind.Int64=> Convert.ToInt64(value),
                EdmPrimitiveTypeKind.Boolean=> Convert.ToBoolean(value),
                EdmPrimitiveTypeKind.Byte=> Convert.ToByte(value),
                EdmPrimitiveTypeKind.Decimal=> Convert.ToDecimal(value),
                EdmPrimitiveTypeKind.Double=> Convert.ToDouble(value),
                EdmPrimitiveTypeKind.Single=> Convert.ToSingle(value),
                EdmPrimitiveTypeKind.SByte=> Convert.ToSByte(value),
                EdmPrimitiveTypeKind.String=> Convert.ToString(value),
                EdmPrimitiveTypeKind.Guid=> Guid.Parse(Convert.ToString(value)!),
                _ => value
            };
        }

        public static EdmStructuralProperty GetEntityKey(this EdmEntityType type) => (EdmStructuralProperty)type.DeclaredKey.First();

        public static IEdmProperty? FindProperty(this EdmEntityType type, string propertyName) {
            if (propertyName==":key")
            {
                return type.GetEntityKey();
            }
            else
            {
                return type.DeclaredProperties.FirstOrDefault(x => x.Name == propertyName);
            }
        }

        /// <summary>
        /// Ensure the current implementation of IEdmEntityType is of type <see cref="EdmEntityType"/>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public static EdmEntityType EnsureType(this IEdmType type, ODataService service) {
            if (type is Microsoft.OData.Edm.EdmEntityType odataType) {
                return (EdmEntityType)service.Model.FindDeclaredType(odataType.FullName);
            } 
            else {
                return (EdmEntityType)type;
            }
        }

        /// <summary>
        /// Use in conjunction with <see cref="EdmPath.AsEdmElements"/> to retrive the underline type of an element.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static EdmEntityType GetEntityType(this IEdmElement x)
        {
            return x is EdmNavigationProperty n ? (EdmEntityType)n.Type.Definition.AsElementType() : (EdmEntityType)(x as IEdmEntitySet)!.EntityType.AsElementType();
        }

        public static (EdmStructuralProperty sourceProperty, EdmStructuralProperty targetProperty) GetRelationProperties(this EdmNavigationProperty property) {
            var constraint = property.ReferentialConstraint.PropertyPairs.First();
            return ((EdmStructuralProperty)constraint.DependentProperty, (EdmStructuralProperty)constraint.PrincipalProperty);
        }

        public static Rows ToRows(this IEnumerable<Dictionary<string, object?>> l) 
        {
            var r = new Rows();
            foreach (var item in l)
            {
                r.Add(item);
            }
            return r;
        }        

    }
}
