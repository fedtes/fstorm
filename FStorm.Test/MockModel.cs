using Microsoft.OData.Edm;

namespace FStorm.Test
{
    internal class MockModel
    {
        public static EdmModel PrepareModel()
        {
            EdmModel edm = new EdmModel();

            // Customer
            EdmEntityType customerType = edm.AddEntityType("my", "Customer", "TABCustomers");
            EdmStructuralProperty customerKey = customerType.AddStructuralProperty("ID", EdmPrimitiveTypeKind.Int32, false, "CustomerID");
            EdmStructuralProperty ragSoc = customerType.AddStructuralProperty("RagSoc", EdmPrimitiveTypeKind.String, false);
            customerType.AddKey(customerKey);

            //Order
            EdmEntityType orderType = edm.AddEntityType("my", "Order", "TABOrders");
            EdmStructuralProperty orderKey = orderType.AddStructuralProperty("Number", EdmPrimitiveTypeKind.String, false, "OrderNumber");
            EdmStructuralProperty orderDate = orderType.AddStructuralProperty("OrderDate", EdmPrimitiveTypeKind.Date, false);
            EdmStructuralProperty orderNote = orderType.AddStructuralProperty("Note", EdmPrimitiveTypeKind.String, true);
            EdmStructuralProperty orderCustomerId = orderType.AddStructuralProperty("CustomerID", EdmPrimitiveTypeKind.Int32, false);
            orderType.AddKey(orderKey);

            //Entity-Relations
            customerType.AddNavigationProperty("Orders", orderType, EdmMultiplicity.Many, customerKey, orderCustomerId);
            orderType.AddNavigationProperty("Customer", customerType, EdmMultiplicity.One, orderCustomerId, customerKey);

            //EntitySet
            EdmEntityContainer container = edm.AddEntityContainer("my", "default");
            container.AddEntitySet("Customers", customerType);
            container.AddEntitySet("Orders", orderType);

            return edm;
        }
    }
}
