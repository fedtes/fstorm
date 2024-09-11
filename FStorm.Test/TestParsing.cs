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

            string expected = "SELECT [~/Customers].[CustomerID] AS [~/Customers/:key], [~/Customers].[CustomerID] AS [~/Customers/ID], [~/Customers].[RagSoc] AS [~/Customers/RagSoc] FROM [TABCustomers] AS [~/Customers]";
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

            string expected = "SELECT [~/Customers].[CustomerID] AS [~/Customers/:key], [~/Customers].[CustomerID] AS [~/Customers/ID], [~/Customers].[RagSoc] AS [~/Customers/RagSoc] FROM [TABCustomers] AS [~/Customers] WHERE [~/Customers].[CustomerID] = @p0";
            Assert.That(_SqlQuery.Statement, Is.EqualTo(expected));
        }

        [Test]
        public void It_should_parse_path_to_structural_property()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection()
                .Get(new GetRequest() { ResourcePath = "Customers(1)/RagSoc" })
                .ToSQL();

            string expected = "SELECT [~/Customers].[RagSoc] AS [~/Customers/RagSoc] FROM [TABCustomers] AS [~/Customers] WHERE [~/Customers].[CustomerID] = @p0";
            Assert.That(_SqlQuery.Statement, Is.EqualTo(expected));
        }


        [Test]
        public void It_should_parse_path_to_nav_prop_n_cardinality()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection()
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders" })
                .ToSQL();

            string expected = @"SELECT [~/Customers/Orders].[OrderNumber] AS [~/Customers/Orders/:key], [~/Customers/Orders].[OrderNumber] AS [~/Customers/Orders/Number], [~/Customers/Orders].[OrderDate] AS [~/Customers/Orders/OrderDate], [~/Customers/Orders].[Note] AS [~/Customers/Orders/Note], [~/Customers/Orders].[Total] AS [~/Customers/Orders/Total], [~/Customers/Orders].[CustomerID] AS [~/Customers/Orders/CustomerID] FROM [TABCustomers] AS [~/Customers] INNER JOIN [TABOrders] AS [~/Customers/Orders] ON [~/Customers/Orders].[CustomerID] = [~/Customers].[CustomerID] WHERE [~/Customers].[CustomerID] = @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_parse_path_to_nav_prop_n_cardinality_with_id()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection()
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders('O24-01')" })
                .ToSQL();

            string expected = @"SELECT [~/Customers/Orders].[OrderNumber] AS [~/Customers/Orders/:key], [~/Customers/Orders].[OrderNumber] AS [~/Customers/Orders/Number], [~/Customers/Orders].[OrderDate] AS [~/Customers/Orders/OrderDate], [~/Customers/Orders].[Note] AS [~/Customers/Orders/Note], [~/Customers/Orders].[Total] AS [~/Customers/Orders/Total], [~/Customers/Orders].[CustomerID] AS [~/Customers/Orders/CustomerID] FROM [TABCustomers] AS [~/Customers] INNER JOIN [TABOrders] AS [~/Customers/Orders] ON [~/Customers/Orders].[CustomerID] = [~/Customers].[CustomerID] WHERE [~/Customers].[CustomerID] = @p0 AND [~/Customers/Orders].[OrderNumber] = @p1";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_parse_path_to_nav_prop_n_cardinality_with_id_to_structured_prop()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection()
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders('O24-01')/OrderDate" })
                .ToSQL();

            string expected = @"SELECT [~/Customers/Orders].[OrderDate] AS [~/Customers/Orders/OrderDate] FROM [TABCustomers] AS [~/Customers] INNER JOIN [TABOrders] AS [~/Customers/Orders] ON [~/Customers/Orders].[CustomerID] = [~/Customers].[CustomerID] WHERE [~/Customers].[CustomerID] = @p0 AND [~/Customers/Orders].[OrderNumber] = @p1";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_parse_path_to_nav_prop_n_cardinality_with_id_to_nav_prop()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection()
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders('O24-01')/Customer" })
                .ToSQL();

            string expected = @"SELECT [~/Customers/Orders/Customer].[CustomerID] AS [~/Customers/Orders/Customer/:key], [~/Customers/Orders/Customer].[CustomerID] AS [~/Customers/Orders/Customer/ID], [~/Customers/Orders/Customer].[RagSoc] AS [~/Customers/Orders/Customer/RagSoc] FROM [TABCustomers] AS [~/Customers] INNER JOIN [TABOrders] AS [~/Customers/Orders] ON [~/Customers/Orders].[CustomerID] = [~/Customers].[CustomerID]INNER JOIN [TABCustomers] AS [~/Customers/Orders/Customer] ON [~/Customers/Orders/Customer].[CustomerID] = [~/Customers/Orders].[CustomerID] WHERE [~/Customers].[CustomerID] = @p0 AND [~/Customers/Orders].[OrderNumber] = @p1";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_parse_path_to_nav_prop_1_cardinality()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Orders('O24-01')/Customer" })
                .ToSQL();

            string expected = @"SELECT [~/Orders/Customer].[CustomerID] AS [~/Orders/Customer/:key], [~/Orders/Customer].[CustomerID] AS [~/Orders/Customer/ID], [~/Orders/Customer].[RagSoc] AS [~/Orders/Customer/RagSoc] FROM [TABOrders] AS [~/Orders] INNER JOIN [TABCustomers] AS [~/Orders/Customer] ON [~/Orders/Customer].[CustomerID] = [~/Orders].[CustomerID] WHERE [~/Orders].[OrderNumber] = @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_count()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders/$count" })
                .ToSQL();

            string expected = @"SELECT COUNT([~/Customers/Orders].[OrderNumber]) AS [count] FROM [TABCustomers] AS [~/Customers] INNER JOIN [TABOrders] AS [~/Customers/Orders] ON [~/Customers/Orders].[CustomerID] = [~/Customers].[CustomerID] WHERE [~/Customers].[CustomerID] = @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_address_subset_of_collecton_1()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders/$filter(Total gt 100)" })
                .ToSQL();

            string expected = @"SELECT [~/Order/Customer].[CustomerID] AS [~/Order/Customer/:key], [~/Order/Customer].[CustomerID] AS [~/Order/Customer/ID], [~/Order/Customer].[RagSoc] AS [~/Order/Customer/RagSoc] FROM [TABOrders] AS [~/Order] INNER JOIN [TABCustomers] AS [~/Order/Customer] ON [~/Order].[CustomerID] = [~/Order/Customer].[CustomerID] WHERE [~/Order].[OrderNumber] = @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_address_subset_of_collecton_2()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders/$filter(@expr)?@expr=Total gt 100" })
                .ToSQL();

            string expected = @"SELECT [~/Order/Customer].[CustomerID] AS [~/Order/Customer/:key], [~/Order/Customer].[CustomerID] AS [~/Order/Customer/ID], [~/Order/Customer].[RagSoc] AS [~/Order/Customer/RagSoc] FROM [TABOrders] AS [~/Order] INNER JOIN [TABCustomers] AS [~/Order/Customer] ON [~/Order].[CustomerID] = [~/Order/Customer].[CustomerID] WHERE [~/Order].[OrderNumber] = @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

    }
}
