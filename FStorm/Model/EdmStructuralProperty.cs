using Microsoft.OData.Edm;

namespace FStorm
{
    public class EdmStructuralProperty : IEdmStructuralProperty
    {
        internal readonly Microsoft.OData.Edm.EdmStructuralProperty property;
        internal readonly string columnName;
        internal readonly string tableName;

        public EdmStructuralProperty(IEdmStructuredType declaringType, string name, IEdmTypeReference type, string columnName, string tableName)
        {
            property = new Microsoft.OData.Edm.EdmStructuralProperty(declaringType, name, type);
            this.columnName = columnName;
            this.tableName = tableName;
        }

        #region "Interface implementation"
        public string DefaultValueString => ((IEdmStructuralProperty)property).DefaultValueString;

        public EdmPropertyKind PropertyKind => ((IEdmProperty)property).PropertyKind;

        public IEdmTypeReference Type => ((IEdmProperty)property).Type;

        public IEdmStructuredType DeclaringType => ((IEdmProperty)property).DeclaringType;

        public string Name => ((IEdmNamedElement)property).Name;

       

        #endregion

    }
}