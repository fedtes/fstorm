using Microsoft.OData.Edm;

namespace FStorm
{
    public class EdmNavigationProperty : IEdmNavigationProperty
    {
        public EdmNavigationProperty(IEdmStructuredType declaringType, EdmNavigationPropertyInfo propertyInfo)
        {
            property = Microsoft.OData.Edm.EdmNavigationProperty.CreateNavigationProperty(declaringType, propertyInfo);
        }

        internal readonly Microsoft.OData.Edm.EdmNavigationProperty property;

        public IEdmNavigationProperty Partner => property.Partner;

        public EdmOnDeleteAction OnDelete => property.OnDelete;

        public bool ContainsTarget => property.ContainsTarget;

        public IEdmReferentialConstraint ReferentialConstraint => property.ReferentialConstraint;

        public EdmPropertyKind PropertyKind => property.PropertyKind;

        public IEdmTypeReference Type => property.Type;

        public IEdmStructuredType DeclaringType => property.DeclaringType;

        public string Name => property.Name;
    }
}