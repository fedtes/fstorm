using Microsoft.OData.Edm;

namespace FStorm.Test
{
    internal class MockModel
    {
        public static EdmModel PrepareModel()
        {
            EdmModel edm = new EdmModel();
            EdmEntityType edmEntityType = edm.AddEntityType("my", "Customer", "TABCustomers");
            EdmStructuralProperty customerKey = edmEntityType.AddStructuralProperty("ID", EdmPrimitiveTypeKind.Int32, false, "CustomerID");
            EdmStructuralProperty ragSoc = edmEntityType.AddStructuralProperty("RagSoc", EdmPrimitiveTypeKind.String, false);
            edmEntityType.AddKey(customerKey);

            EdmEntityContainer container = edm.AddEntityContainer("my", "default");
            container.AddEntitySet("Customers", edmEntityType);

            return edm;
        }
    }
}
