using Microsoft.Data.Sqlite;
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
    
    
        public static void CreateDB(SqliteConnection sqlite)
        {
            var t = sqlite.BeginTransaction();
            var cmd = sqlite.CreateCommand();
            cmd.CommandText = "CREATE TABLE TABCustomers (CustomerID INT NOT NULL, RagSoc CHAR(50) NOT NULL);";
            cmd.Transaction = t;
            cmd.ExecuteNonQuery();
            t.Commit();

            var t1 = sqlite.BeginTransaction();
            var cmd1 = sqlite.CreateCommand();
            cmd.CommandText = "INSERT INTO TABCustomers (CustomerID,RagSoc) VALUES (1, 'ACME'),(2, 'ECorp'),(3, 'DreamSolutions');";
            cmd.Transaction = t1;
            cmd.ExecuteNonQuery();
            t1.Commit();
        }
    }
}
