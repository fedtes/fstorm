﻿using Microsoft.OData.Edm;

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
        public static EdmEntityType EnsureType(this IEdmStructuredType type, FStormService service) {
            if (type is Microsoft.OData.Edm.EdmEntityType odataType) {
                return (EdmEntityType)service.Model.FindDeclaredType(odataType.FullName);
            } 
            else {
                return (EdmEntityType)type;
            }
        }

    }
}
