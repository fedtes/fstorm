using Microsoft.OData.Edm;

namespace FStorm
{
    public class EdmEntityType : IEdmEntityType
    {
        internal readonly Microsoft.OData.Edm.EdmEntityType edmEntityType;

        internal string Table { get; set; }

        public EdmEntityType(string namespaceName, string name, string table)
        {
            Table = table;
            edmEntityType = new Microsoft.OData.Edm.EdmEntityType(namespaceName,name);
        }

        public EdmStructuralProperty AddStructuralProperty(string name, EdmPrimitiveTypeKind kind, bool isNullable)
        {
            return this.AddStructuralProperty(name, kind, isNullable, name, Table);
        }

        public EdmStructuralProperty AddStructuralProperty(string name, EdmPrimitiveTypeKind kind, bool isNullable, string columnName)
        {
            return this.AddStructuralProperty(name, kind, isNullable, columnName, Table);
        }

        public EdmStructuralProperty AddStructuralProperty(string name, EdmPrimitiveTypeKind kind, bool isNullable, string columnName, string tableName)
        {
            var prop = new EdmStructuralProperty(edmEntityType, name, EdmCoreModel.Instance.GetPrimitive(kind, isNullable), columnName, tableName);
            this.edmEntityType.AddProperty(prop);
            return prop;
        }

        public void AddKey(EdmStructuralProperty customerKey)
        {
            edmEntityType.AddKeys(customerKey);
        }

        public void AddNavigationProperty(string name, IEdmEntityType targetType, EdmMultiplicity multiplicity, IEdmStructuralProperty sourceProperty, IEdmStructuralProperty targetProperty)
        {
            var navigationInfo = new EdmNavigationPropertyInfo()
            {
                Name = name,
                ContainsTarget = multiplicity == EdmMultiplicity.One ? true : false,
                Target = targetType,
                TargetMultiplicity = multiplicity,
                PrincipalProperties = new List<IEdmStructuralProperty>() { targetProperty },
                DependentProperties = new List<IEdmStructuralProperty>() { sourceProperty },
                OnDelete = EdmOnDeleteAction.None
            };

            edmEntityType.AddProperty(new EdmNavigationProperty(edmEntityType, navigationInfo));
        }

        #region "Interface implementation"
        public IEnumerable<IEdmStructuralProperty> DeclaredKey => ((IEdmEntityType)edmEntityType).DeclaredKey;

        public bool HasStream => ((IEdmEntityType)edmEntityType).HasStream;

        public bool IsAbstract => ((IEdmStructuredType)edmEntityType).IsAbstract;

        public bool IsOpen => ((IEdmStructuredType)edmEntityType).IsOpen;

        public IEdmStructuredType BaseType => ((IEdmStructuredType)edmEntityType).BaseType;

        public IEnumerable<IEdmProperty> DeclaredProperties => ((IEdmStructuredType)edmEntityType).DeclaredProperties;

        public EdmTypeKind TypeKind => ((IEdmType)edmEntityType).TypeKind;

        public EdmSchemaElementKind SchemaElementKind => ((IEdmSchemaElement)edmEntityType).SchemaElementKind;

        public string Namespace => ((IEdmSchemaElement)edmEntityType).Namespace;

        public string Name => ((IEdmNamedElement)edmEntityType).Name;

        public IEdmProperty FindProperty(string name)
        {
            return ((IEdmStructuredType)edmEntityType).FindProperty(name);
        }


        #endregion
    }
}