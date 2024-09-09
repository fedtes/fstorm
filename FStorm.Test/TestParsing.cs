using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace FStorm.Test
{
    internal class TestParsing
    {
        IServiceProvider serviceProvider;

        [TearDown()]
        public void TearDown()
        {
            if (serviceProvider is IDisposable disposable)
                disposable.Dispose();
        }

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddFStorm(MockModel.PrepareModel(), new FStormOptions() { SQLCompilerType= SQLCompilerType.MSSQL , ServiceRoot= "https://my.service/odata/", SQLConnection= new SqliteConnection() });
            serviceProvider = services.BuildServiceProvider();
        }


        [Test]
        public void It_should_parse_path_to_collection()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService
                .OpenConnection()
                .Get(new GetRequest() { ResourcePath = "Customers" })
                .ToSQL();

            string expected = "SELECT [#/Customer].[CustomerID] AS [#/Customer/:key], [#/Customer].[CustomerID] AS [#/Customer/ID], [#/Customer].[RagSoc] AS [#/Customer/RagSoc] FROM [TABCustomers] AS [#/Customer]";
            Assert.That(_SqlQuery.Statement, Is.EqualTo(expected));
        }

        [Test]
        public void It_should_parse_path_to_entity()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService
                .OpenConnection()
                .Get(new GetRequest() { ResourcePath = "Customers(1)" })
                .ToSQL();

            string expected = "SELECT [#/Customer].[CustomerID] AS [#/Customer/:key], [#/Customer].[CustomerID] AS [#/Customer/ID], [#/Customer].[RagSoc] AS [#/Customer/RagSoc] FROM [TABCustomers] AS [#/Customer] WHERE [#/Customer].[CustomerID] = @p0";
            Assert.That(_SqlQuery.Statement, Is.EqualTo(expected));
        }

        [Test]
        public void It_should_parse_path_to_structural_property()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection()
                .Get(new GetRequest() { ResourcePath = "Customers(1)/RagSoc" })
                .ToSQL();

            string expected = "SELECT [#/Customer].[RagSoc] AS [#/Customer/RagSoc], [#/Customer].[CustomerID] AS [#/Customer/:key] FROM [TABCustomers] AS [#/Customer] WHERE [#/Customer].[CustomerID] = @p0";
            Assert.That(_SqlQuery.Statement, Is.EqualTo(expected));
        }


        [Test]
        public void It_should_parse_path_to_nav_prop_n_cardinality()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection()
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders" })
                .ToSQL();

            string expected = @"SELECT [#/Customer/Orders].[OrderNumber] AS [#/Customer/Orders/:key], [#/Customer/Orders].[OrderNumber] AS [#/Customer/Orders/Number], [#/Customer/Orders].[OrderDate] AS [#/Customer/Orders/OrderDate], [#/Customer/Orders].[Note] AS [#/Customer/Orders/Note], [#/Customer/Orders].[CustomerID] AS [#/Customer/Orders/CustomerID] FROM [TABCustomers] AS [#/Customer] INNER JOIN [TABOrders] AS [#/Customer/Orders] ON [#/Customer].[CustomerID] = [#/Customer/Orders].[CustomerID] WHERE [#/Customer].[CustomerID] = @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_parse_path_to_nav_prop_n_cardinality_with_id()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection()
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders('O24-01')" })
                .ToSQL();

            string expected = @"SELECT [#/Customer/Orders].[OrderNumber] AS [#/Customer/Orders/:key], [#/Customer/Orders].[OrderNumber] AS [#/Customer/Orders/Number], [#/Customer/Orders].[OrderDate] AS [#/Customer/Orders/OrderDate], [#/Customer/Orders].[Note] AS [#/Customer/Orders/Note], [#/Customer/Orders].[CustomerID] AS [#/Customer/Orders/CustomerID] FROM [TABCustomers] AS [#/Customer] INNER JOIN [TABOrders] AS [#/Customer/Orders] ON [#/Customer].[CustomerID] = [#/Customer/Orders].[CustomerID] WHERE [#/Customer].[CustomerID] = @p0 AND [#/Customer/Orders].[OrderNumber] = @p1";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_parse_path_to_nav_prop_n_cardinality_with_id_to_structured_prop()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection()
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders('O24-01')/OrderDate" })
                .ToSQL();

            string expected = @"SELECT [#/Customer/Orders].[OrderDate] AS [#/Customer/Orders/OrderDate], [#/Customer/Orders].[OrderNumber] AS [#/Customer/Orders/:key] FROM [TABCustomers] AS [#/Customer] INNER JOIN [TABOrders] AS [#/Customer/Orders] ON [#/Customer].[CustomerID] = [#/Customer/Orders].[CustomerID] WHERE [#/Customer].[CustomerID] = @p0 AND [#/Customer/Orders].[OrderNumber] = @p1";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_parse_path_to_nav_prop_n_cardinality_with_id_to_nav_prop()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection()
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders('O24-01')/Customer" })
                .ToSQL();

            string expected = @"SELECT [#/Customer/Orders/Customer].[CustomerID] AS [#/Customer/Orders/Customer/:key], [#/Customer/Orders/Customer].[CustomerID] AS [#/Customer/Orders/Customer/ID], [#/Customer/Orders/Customer].[RagSoc] AS [#/Customer/Orders/Customer/RagSoc] FROM [TABCustomers] AS [#/Customer] INNER JOIN [TABOrders] AS [#/Customer/Orders] ON [#/Customer].[CustomerID] = [#/Customer/Orders].[CustomerID]INNER JOIN [TABCustomers] AS [#/Customer/Orders/Customer] ON [#/Customer/Orders].[CustomerID] = [#/Customer/Orders/Customer].[CustomerID] WHERE [#/Customer].[CustomerID] = @p0 AND [#/Customer/Orders].[OrderNumber] = @p1";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_parse_path_to_nav_prop_1_cardinality()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Orders('O24-01')/Customer" })
                .ToSQL();

            string expected = @"SELECT [#/Order/Customer].[CustomerID] AS [#/Order/Customer/:key], [#/Order/Customer].[CustomerID] AS [#/Order/Customer/ID], [#/Order/Customer].[RagSoc] AS [#/Order/Customer/RagSoc] FROM [TABOrders] AS [#/Order] INNER JOIN [TABCustomers] AS [#/Order/Customer] ON [#/Order].[CustomerID] = [#/Order/Customer].[CustomerID] WHERE [#/Order].[OrderNumber] = @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_count()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders/$count" })
                .ToSQL();

            string expected = @"SELECT COUNT([OrderNumber]) AS [count] FROM [TABCustomers] AS [#/Customer] INNER JOIN [TABOrders] AS [#/Customer/Orders] ON [#/Customer].[CustomerID] = [#/Customer/Orders].[CustomerID] WHERE [#/Customer].[CustomerID] = @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_address_subset_of_collecton_1()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders/$filter(Total gt 100)" })
                .ToSQL();

            string expected = @"SELECT [#/Order/Customer].[CustomerID] AS [#/Order/Customer/:key], [#/Order/Customer].[CustomerID] AS [#/Order/Customer/ID], [#/Order/Customer].[RagSoc] AS [#/Order/Customer/RagSoc] FROM [TABOrders] AS [#/Order] INNER JOIN [TABCustomers] AS [#/Order/Customer] ON [#/Order].[CustomerID] = [#/Order/Customer].[CustomerID] WHERE [#/Order].[OrderNumber] = @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_address_subset_of_collecton_2()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders/$filter(@expr)?@expr=Total gt 100" })
                .ToSQL();

            string expected = @"SELECT [#/Order/Customer].[CustomerID] AS [#/Order/Customer/:key], [#/Order/Customer].[CustomerID] AS [#/Order/Customer/ID], [#/Order/Customer].[RagSoc] AS [#/Order/Customer/RagSoc] FROM [TABOrders] AS [#/Order] INNER JOIN [TABCustomers] AS [#/Order/Customer] ON [#/Order].[CustomerID] = [#/Order/Customer].[CustomerID] WHERE [#/Order].[OrderNumber] = @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

    }
}
