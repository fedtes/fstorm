using Microsoft.OData.Edm;

namespace FStorm
{
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
}