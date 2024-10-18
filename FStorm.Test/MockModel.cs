using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.OData.Edm;

namespace FStorm.Test
{
    public class MockModel
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
            EdmStructuralProperty orderDate = orderType.AddStructuralProperty("OrderDate", EdmPrimitiveTypeKind.String, false);
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
            container.AddEntitySet("Adresses", addressType);
            container.AddEntitySet("AddressHints", addressHintType);
            container.AddEntitySet("Articles", articleType);

            return edm;
        }
    

    }

    public class FakeConnection : DbConnection
    {
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
        public override string ConnectionString { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).

        public override string Database => throw new NotImplementedException();

        public override string DataSource => throw new NotImplementedException();

        public override string ServerVersion => throw new NotImplementedException();

        public override ConnectionState State => throw new NotImplementedException();

        public override void ChangeDatabase(string databaseName)
        {
            throw new NotImplementedException();
        }

        public override void Close()
        {
        }

        public override void Open()
        {
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return new FakeTransaction();
        }

        protected override DbCommand CreateDbCommand()
        {
            throw new NotImplementedException();
        }
    }

    public class FakeTransaction : DbTransaction
    {
        public override IsolationLevel IsolationLevel => throw new NotImplementedException();

        protected override DbConnection? DbConnection => throw new NotImplementedException();

        public override void Commit()
        {        }

        public override void Rollback()
        {        }
    }
}
