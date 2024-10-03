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
            EdmStructuralProperty addressId = customerType.AddStructuralProperty("AddressID", EdmPrimitiveTypeKind.Int32, true);
            customerType.AddKey(customerKey);

            //Order
            EdmEntityType orderType = edm.AddEntityType("my", "Order", "TABOrders");
            EdmStructuralProperty orderKey = orderType.AddStructuralProperty("Number", EdmPrimitiveTypeKind.String, false, "OrderNumber");
            EdmStructuralProperty orderDate = orderType.AddStructuralProperty("OrderDate", EdmPrimitiveTypeKind.Date, false);
            EdmStructuralProperty orderNote = orderType.AddStructuralProperty("Note", EdmPrimitiveTypeKind.String, true);
            EdmStructuralProperty total = orderType.AddStructuralProperty("Total", EdmPrimitiveTypeKind.Decimal, false);
            EdmStructuralProperty deliveryAddressID = orderType.AddStructuralProperty("DeliveryAddressID", EdmPrimitiveTypeKind.Int32, true);
            EdmStructuralProperty orderCustomerId = orderType.AddStructuralProperty("CustomerID", EdmPrimitiveTypeKind.Int32, false);
            orderType.AddKey(orderKey);

            //Articles
            EdmEntityType articleType = edm.AddEntityType("my", "Article", "TABArticles");
            var articleId = articleType.AddStructuralProperty("ArticleId",EdmPrimitiveTypeKind.Int32,false);
            articleType.AddStructuralProperty("Name", EdmPrimitiveTypeKind.String, false);
            articleType.AddStructuralProperty("Prize", EdmPrimitiveTypeKind.Decimal, false);
            var articleOrderNumber = articleType.AddStructuralProperty("Number", EdmPrimitiveTypeKind.String, false);
            articleType.AddKey(articleId);

            //Address
            EdmEntityType addressType = edm.AddEntityType("my", "Address", "TABAddresses");
            var addressKey = addressType.AddStructuralProperty("AddressID", EdmPrimitiveTypeKind.Int32, false);
            addressType.AddStructuralProperty("City", EdmPrimitiveTypeKind.String, true);
            addressType.AddStructuralProperty("Street", EdmPrimitiveTypeKind.String, true);
            addressType.AddStructuralProperty("Number", EdmPrimitiveTypeKind.Int32, true);
            addressType.AddStructuralProperty("Country", EdmPrimitiveTypeKind.String, true);
            addressType.AddKey(addressKey);

            //AddressHints
            EdmEntityType addressHintType = edm.AddEntityType("my", "AddressHint", "TABAddressHints");
            var addressHintTypeKey = addressHintType.AddStructuralProperty("AddressHintID", EdmPrimitiveTypeKind.Int32, false);
            var addressHintAddressID = addressHintType.AddStructuralProperty("AddressID", EdmPrimitiveTypeKind.Int32, false);
            addressHintType.AddStructuralProperty("Hint", EdmPrimitiveTypeKind.String, false);
            addressHintType.AddKey(addressHintTypeKey);

            //Entity-Relations
            customerType.AddNavigationProperty("Orders", orderType, EdmMultiplicity.Many, customerKey, orderCustomerId);
            customerType.AddNavigationProperty("Address", addressType, EdmMultiplicity.One, addressId, addressKey);
            orderType.AddNavigationProperty("Customer", customerType, EdmMultiplicity.One, orderCustomerId, customerKey);
            orderType.AddNavigationProperty("Articles", articleType, EdmMultiplicity.Many, orderKey, articleOrderNumber);
            orderType.AddNavigationProperty("DeliveryAddress", addressType, EdmMultiplicity.ZeroOrOne, deliveryAddressID, addressKey);
            articleType.AddNavigationProperty("Order", orderType, EdmMultiplicity.One, articleOrderNumber, orderKey);
            addressType.AddNavigationProperty("Hints",addressHintType, EdmMultiplicity.Many, addressKey, addressHintAddressID);

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
            cmd.CommandText = "CREATE TABLE TABCustomers (CustomerID INT NOT NULL, RagSoc CHAR(50) NOT NULL, AddressID INT NULL); CREATE TABLE TABOrders (OrderNumber INT NOT NULL, Note CHAR(50) NOT NULL, Total decimal(10,5) NULL, CustomerID INT NOT NULL);";
            cmd.Transaction = t;
            cmd.ExecuteNonQuery();
            t.Commit();

            var t1 = sqlite.BeginTransaction();
            var cmd1 = sqlite.CreateCommand();
            cmd1.CommandText = "INSERT INTO TABCustomers (CustomerID,RagSoc) VALUES (1, 'ACME'),(2, 'ECorp'),(3, 'DreamSolutions');";
            cmd1.Transaction = t1;
            cmd1.ExecuteNonQuery();
            t1.Commit();


            var t2 = sqlite.BeginTransaction();
            var cmd2 = sqlite.CreateCommand();
            cmd2.CommandText = "INSERT INTO TABOrders (OrderNumber, Note, Total, CustomerID) VALUES (123,'Dynamite', 500.12, 1), (124,'TNT', 1250.00, 1);";
            cmd2.Transaction = t2;
            cmd2.ExecuteNonQuery();
            t2.Commit();
        }
    }
}
