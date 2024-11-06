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

}
