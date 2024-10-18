using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Vocabularies;

namespace FStorm
{
    public class EdmModel : IEdmModel
    {
        internal readonly Microsoft.OData.Edm.EdmModel model;

        public EdmModel() :base() 
        {
            model= new Microsoft.OData.Edm.EdmModel();
        }

        #region "Definition Methods"

        public EdmEntityType AddEntityType(string @namespace, string name, string table)
        {
            var _type = new EdmEntityType(@namespace, name, table);
            model.AddElement(_type);
            return _type;
        }

        public EdmEntityType AddEntityType(string @namespace, string name)
        {
            return AddEntityType(@namespace,name, name);
        }

        public EdmEntityContainer AddEntityContainer(string namespaceName, string name)
        {
            EdmEntityContainer edmEntityContainer = new EdmEntityContainer(namespaceName, name);
            model.AddElement(edmEntityContainer);
            return edmEntityContainer;
        }


        #endregion

        #region "Interface implementation"
        public IEnumerable<IEdmSchemaElement> SchemaElements => ((IEdmModel)model).SchemaElements;

        public IEnumerable<IEdmVocabularyAnnotation> VocabularyAnnotations => ((IEdmModel)model).VocabularyAnnotations;

        public IEnumerable<IEdmModel> ReferencedModels => ((IEdmModel)model).ReferencedModels;

        public IEnumerable<string> DeclaredNamespaces => ((IEdmModel)model).DeclaredNamespaces;

        public IEdmDirectValueAnnotationsManager DirectValueAnnotationsManager => ((IEdmModel)model).DirectValueAnnotationsManager;

        public IEdmEntityContainer EntityContainer => ((IEdmModel)model).EntityContainer;

        public IEnumerable<IEdmOperation> FindDeclaredBoundOperations(IEdmType bindingType)
        {
            return ((IEdmModel)model).FindDeclaredBoundOperations(bindingType);
        }

        public IEnumerable<IEdmOperation> FindDeclaredBoundOperations(string qualifiedName, IEdmType bindingType)
        {
            return ((IEdmModel)model).FindDeclaredBoundOperations(qualifiedName, bindingType);
        }

        public IEnumerable<IEdmOperation> FindDeclaredOperations(string qualifiedName)
        {
            return ((IEdmModel)model).FindDeclaredOperations(qualifiedName);
        }

        public IEdmTerm FindDeclaredTerm(string qualifiedName)
        {
            return ((IEdmModel)model).FindDeclaredTerm(qualifiedName);
        }

        public IEdmSchemaType FindDeclaredType(string qualifiedName)
        {
            return ((IEdmModel)model).FindDeclaredType(qualifiedName);
        }

        public IEnumerable<IEdmVocabularyAnnotation> FindDeclaredVocabularyAnnotations(IEdmVocabularyAnnotatable element)
        {
            return ((IEdmModel)model).FindDeclaredVocabularyAnnotations(element);
        }

        public IEnumerable<IEdmStructuredType> FindDirectlyDerivedTypes(IEdmStructuredType baseType)
        {
            return ((IEdmModel)model).FindDirectlyDerivedTypes(baseType);
        }

        #endregion
    }

    public class EdmEntityContainer : IEdmEntityContainer
    {
        internal readonly Microsoft.OData.Edm.EdmEntityContainer container;

        public EdmEntityContainer(string @namespace, string name)
        {
            container = new Microsoft.OData.Edm.EdmEntityContainer(@namespace,name);
        }

        #region "Interface implementation"
        public IEnumerable<IEdmEntityContainerElement> Elements => ((IEdmEntityContainer)container).Elements;

        public EdmSchemaElementKind SchemaElementKind => ((IEdmSchemaElement)container).SchemaElementKind;

        public string Namespace => ((IEdmSchemaElement)container).Namespace;

        public string Name => ((IEdmNamedElement)container).Name;

        public void AddEntitySet(string name, EdmEntityType edmEntityType)
        {
            EdmEntitySet edmEntitySet = new EdmEntitySet(this, name, edmEntityType);
            container.AddElement(edmEntitySet);
        }

        public IEdmEntitySet FindEntitySet(string setName)
        {
            return ((IEdmEntityContainer)container).FindEntitySet(setName);
        }

        public IEnumerable<IEdmOperationImport> FindOperationImports(string operationName)
        {
            return ((IEdmEntityContainer)container).FindOperationImports(operationName);
        }

        public IEdmSingleton FindSingleton(string singletonName)
        {
            return ((IEdmEntityContainer)container).FindSingleton(singletonName);
        }

        #endregion
    }


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
            edmEntityType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo()
            {
                Name = name,
                ContainsTarget = multiplicity == EdmMultiplicity.One ? true : false,
                Target = targetType,
                TargetMultiplicity = multiplicity,
                PrincipalProperties = new List<IEdmStructuralProperty>() { targetProperty },
                DependentProperties = new List<IEdmStructuralProperty>() { sourceProperty },
                OnDelete = EdmOnDeleteAction.None
            });
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
