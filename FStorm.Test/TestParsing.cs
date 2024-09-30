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

            string expected = "SELECT [~/Customers].[CustomerID] AS [~/Customers/:key], [~/Customers].[CustomerID] AS [~/Customers/ID], [~/Customers].[RagSoc] AS [~/Customers/RagSoc], [~/Customers].[AddressID] AS [~/Customers/AddressID] FROM [TABCustomers] AS [~/Customers]";
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

            string expected = "SELECT [~/Customers].[CustomerID] AS [~/Customers/:key], [~/Customers].[CustomerID] AS [~/Customers/ID], [~/Customers].[RagSoc] AS [~/Customers/RagSoc], [~/Customers].[AddressID] AS [~/Customers/AddressID] FROM [TABCustomers] AS [~/Customers] WHERE [~/Customers].[CustomerID] = @p0";
            Assert.That(_SqlQuery.Statement, Is.EqualTo(expected));
        }

        [Test]
        public void It_should_parse_path_to_structural_property()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection()
                .Get(new GetRequest() { ResourcePath = "Customers(1)/RagSoc" })
                .ToSQL();

            string expected = "SELECT [~/Customers].[RagSoc] AS [~/Customers/RagSoc], [~/Customers].[CustomerID] AS [~/Customers/:key] FROM [TABCustomers] AS [~/Customers] WHERE [~/Customers].[CustomerID] = @p0";
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

            string expected = @"SELECT [~/Customers/Orders].[OrderDate] AS [~/Customers/Orders/OrderDate], [~/Customers/Orders].[OrderNumber] AS [~/Customers/Orders/:key] FROM [TABCustomers] AS [~/Customers] INNER JOIN [TABOrders] AS [~/Customers/Orders] ON [~/Customers/Orders].[CustomerID] = [~/Customers].[CustomerID] WHERE [~/Customers].[CustomerID] = @p0 AND [~/Customers/Orders].[OrderNumber] = @p1";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_parse_path_to_nav_prop_n_cardinality_with_id_to_nav_prop()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection()
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders('O24-01')/Customer" })
                .ToSQL();

            string expected = @"SELECT [~/Customers/Orders/Customer].[CustomerID] AS [~/Customers/Orders/Customer/:key], [~/Customers/Orders/Customer].[CustomerID] AS [~/Customers/Orders/Customer/ID], [~/Customers/Orders/Customer].[RagSoc] AS [~/Customers/Orders/Customer/RagSoc], [~/Customers/Orders/Customer].[AddressID] AS [~/Customers/Orders/Customer/AddressID] FROM [TABCustomers] AS [~/Customers] INNER JOIN [TABOrders] AS [~/Customers/Orders] ON [~/Customers/Orders].[CustomerID] = [~/Customers].[CustomerID]INNER JOIN [TABCustomers] AS [~/Customers/Orders/Customer] ON [~/Customers/Orders/Customer].[CustomerID] = [~/Customers/Orders].[CustomerID] WHERE [~/Customers].[CustomerID] = @p0 AND [~/Customers/Orders].[OrderNumber] = @p1";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_parse_path_to_nav_prop_1_cardinality()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Orders('O24-01')/Customer" })
                .ToSQL();

            string expected = @"SELECT [~/Orders/Customer].[CustomerID] AS [~/Orders/Customer/:key], [~/Orders/Customer].[CustomerID] AS [~/Orders/Customer/ID], [~/Orders/Customer].[RagSoc] AS [~/Orders/Customer/RagSoc], [~/Orders/Customer].[AddressID] AS [~/Orders/Customer/AddressID] FROM [TABOrders] AS [~/Orders] INNER JOIN [TABCustomers] AS [~/Orders/Customer] ON [~/Orders/Customer].[CustomerID] = [~/Orders].[CustomerID] WHERE [~/Orders].[OrderNumber] = @p0";
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

            string expected = @"SELECT [~/Customers/Orders].[OrderNumber] AS [~/Customers/Orders/:key], [~/Customers/Orders].[OrderNumber] AS [~/Customers/Orders/Number], [~/Customers/Orders].[OrderDate] AS [~/Customers/Orders/OrderDate], [~/Customers/Orders].[Note] AS [~/Customers/Orders/Note], [~/Customers/Orders].[Total] AS [~/Customers/Orders/Total], [~/Customers/Orders].[CustomerID] AS [~/Customers/Orders/CustomerID] FROM (SELECT [~/Customers/Orders].[OrderNumber] AS [~/Customers/Orders/:key], [~/Customers/Orders].[OrderNumber] AS [~/Customers/Orders/Number], [~/Customers/Orders].[OrderDate] AS [~/Customers/Orders/OrderDate], [~/Customers/Orders].[Note] AS [~/Customers/Orders/Note], [~/Customers/Orders].[Total] AS [~/Customers/Orders/Total], [~/Customers/Orders].[CustomerID] AS [~/Customers/Orders/CustomerID] FROM [TABCustomers] AS [~/Customers] INNER JOIN [TABOrders] AS [~/Customers/Orders] ON [~/Customers/Orders].[CustomerID] = [~/Customers].[CustomerID] WHERE [~/Customers].[CustomerID] = @p0) AS [~/Customers/Orders] WHERE [~/Customers/Orders].[Total] > @p1";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_address_property_of_subset_of_collecton()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders/$filter(Total gt 100)/1/OrderDate" })
                .ToSQL();

            string expected = @"SELECT [~/Customers/Orders].[OrderDate] AS [~/Customers/Orders/OrderDate], [~/Customers/Orders].[OrderNumber] AS [~/Customers/Orders/:key] FROM (SELECT [~/Customers/Orders].[OrderNumber] AS [~/Customers/Orders/:key], [~/Customers/Orders].[OrderNumber] AS [~/Customers/Orders/Number], [~/Customers/Orders].[OrderDate] AS [~/Customers/Orders/OrderDate], [~/Customers/Orders].[Note] AS [~/Customers/Orders/Note], [~/Customers/Orders].[Total] AS [~/Customers/Orders/Total], [~/Customers/Orders].[CustomerID] AS [~/Customers/Orders/CustomerID] FROM [TABCustomers] AS [~/Customers] INNER JOIN [TABOrders] AS [~/Customers/Orders] ON [~/Customers/Orders].[CustomerID] = [~/Customers].[CustomerID] WHERE [~/Customers].[CustomerID] = @p0) AS [~/Customers/Orders] WHERE [~/Customers/Orders].[Total] > @p1 AND [~/Customers/Orders].[OrderNumber] = @p2";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_address_subset_of_collecton_2()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders/$filter(@expr)?@expr=Total gt 100" })
                .ToSQL();

            string expected = @"SELECT [~/Customers/Orders].[OrderNumber] AS [~/Customers/Orders/:key], [~/Customers/Orders].[OrderNumber] AS [~/Customers/Orders/Number], [~/Customers/Orders].[OrderDate] AS [~/Customers/Orders/OrderDate], [~/Customers/Orders].[Note] AS [~/Customers/Orders/Note], [~/Customers/Orders].[Total] AS [~/Customers/Orders/Total], [~/Customers/Orders].[CustomerID] AS [~/Customers/Orders/CustomerID] FROM (SELECT [~/Customers/Orders].[OrderNumber] AS [~/Customers/Orders/:key], [~/Customers/Orders].[OrderNumber] AS [~/Customers/Orders/Number], [~/Customers/Orders].[OrderDate] AS [~/Customers/Orders/OrderDate], [~/Customers/Orders].[Note] AS [~/Customers/Orders/Note], [~/Customers/Orders].[Total] AS [~/Customers/Orders/Total], [~/Customers/Orders].[CustomerID] AS [~/Customers/Orders/CustomerID] FROM [TABCustomers] AS [~/Customers] INNER JOIN [TABOrders] AS [~/Customers/Orders] ON [~/Customers/Orders].[CustomerID] = [~/Customers].[CustomerID] WHERE [~/Customers].[CustomerID] = @p0) AS [~/Customers/Orders] WHERE [~/Customers/Orders].[Total] = @p1";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_filter_collection_eq()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=RagSoc eq 'Acme'" })
                .ToSQL();

            string expected = @"SELECT [~/Customers].[CustomerID] AS [~/Customers/:key], [~/Customers].[CustomerID] AS [~/Customers/ID], [~/Customers].[RagSoc] AS [~/Customers/RagSoc], [~/Customers].[AddressID] AS [~/Customers/AddressID] FROM [TABCustomers] AS [~/Customers] WHERE [~/Customers].[RagSoc] = @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_filter_collection_ne()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=RagSoc ne 'Acme'" })
                .ToSQL();

            string expected = @"SELECT [~/Customers].[CustomerID] AS [~/Customers/:key], [~/Customers].[CustomerID] AS [~/Customers/ID], [~/Customers].[RagSoc] AS [~/Customers/RagSoc], [~/Customers].[AddressID] AS [~/Customers/AddressID] FROM [TABCustomers] AS [~/Customers] WHERE [~/Customers].[RagSoc] <> @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_filter_collection_gt()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=ID gt 10" })
                .ToSQL();

            string expected = @"SELECT [~/Customers].[CustomerID] AS [~/Customers/:key], [~/Customers].[CustomerID] AS [~/Customers/ID], [~/Customers].[RagSoc] AS [~/Customers/RagSoc], [~/Customers].[AddressID] AS [~/Customers/AddressID] FROM [TABCustomers] AS [~/Customers] WHERE [~/Customers].[CustomerID] > @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_filter_collection_ge()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=ID ge 10" })
                .ToSQL();

            string expected = @"SELECT [~/Customers].[CustomerID] AS [~/Customers/:key], [~/Customers].[CustomerID] AS [~/Customers/ID], [~/Customers].[RagSoc] AS [~/Customers/RagSoc], [~/Customers].[AddressID] AS [~/Customers/AddressID] FROM [TABCustomers] AS [~/Customers] WHERE [~/Customers].[CustomerID] >= @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_filter_collection_lt()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=ID lt 10" })
                .ToSQL();

            string expected = @"SELECT [~/Customers].[CustomerID] AS [~/Customers/:key], [~/Customers].[CustomerID] AS [~/Customers/ID], [~/Customers].[RagSoc] AS [~/Customers/RagSoc], [~/Customers].[AddressID] AS [~/Customers/AddressID] FROM [TABCustomers] AS [~/Customers] WHERE [~/Customers].[CustomerID] < @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_filter_collection_le()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=ID le 10" })
                .ToSQL();

            string expected = @"SELECT [~/Customers].[CustomerID] AS [~/Customers/:key], [~/Customers].[CustomerID] AS [~/Customers/ID], [~/Customers].[RagSoc] AS [~/Customers/RagSoc], [~/Customers].[AddressID] AS [~/Customers/AddressID] FROM [TABCustomers] AS [~/Customers] WHERE [~/Customers].[CustomerID] <= @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_filter_collection_and()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=ID ge 1 and RagSoc eq 'acme'" })
                .ToSQL();

            string expected = @"SELECT [~/Customers].[CustomerID] AS [~/Customers/:key], [~/Customers].[CustomerID] AS [~/Customers/ID], [~/Customers].[RagSoc] AS [~/Customers/RagSoc], [~/Customers].[AddressID] AS [~/Customers/AddressID] FROM [TABCustomers] AS [~/Customers] WHERE ([~/Customers].[CustomerID] >= @p0 AND [~/Customers].[RagSoc] = @p1)";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_filter_collection_and_2()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=ID ge 1 and ID le 2 and RagSoc eq 'acme'" })
                .ToSQL();

            string expected = @"SELECT [~/Customers].[CustomerID] AS [~/Customers/:key], [~/Customers].[CustomerID] AS [~/Customers/ID], [~/Customers].[RagSoc] AS [~/Customers/RagSoc], [~/Customers].[AddressID] AS [~/Customers/AddressID] FROM [TABCustomers] AS [~/Customers] WHERE ([~/Customers].[CustomerID] >= @p0 AND [~/Customers].[CustomerID] <= @p1 AND [~/Customers].[RagSoc] = @p2)";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_filter_collection_or()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=ID le 1 or ID ge 2" })
                .ToSQL();

            string expected = @"SELECT [~/Customers].[CustomerID] AS [~/Customers/:key], [~/Customers].[CustomerID] AS [~/Customers/ID], [~/Customers].[RagSoc] AS [~/Customers/RagSoc], [~/Customers].[AddressID] AS [~/Customers/AddressID] FROM [TABCustomers] AS [~/Customers] WHERE ([~/Customers].[CustomerID] <= @p0 OR [~/Customers].[CustomerID] >= @p1)";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_filter_collection_or_2()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=ID le 1 or ID ge 3 or ID eq 2" })
                .ToSQL();

            string expected = @"SELECT [~/Customers].[CustomerID] AS [~/Customers/:key], [~/Customers].[CustomerID] AS [~/Customers/ID], [~/Customers].[RagSoc] AS [~/Customers/RagSoc], [~/Customers].[AddressID] AS [~/Customers/AddressID] FROM [TABCustomers] AS [~/Customers] WHERE ([~/Customers].[CustomerID] <= @p0 OR [~/Customers].[CustomerID] >= @p1 OR [~/Customers].[CustomerID] = @p2)";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_filter_collection_or_and()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=(ID le 1 or ID ge 3) and ID eq 2" })
                .ToSQL();

            string expected = @"SELECT [~/Customers].[CustomerID] AS [~/Customers/:key], [~/Customers].[CustomerID] AS [~/Customers/ID], [~/Customers].[RagSoc] AS [~/Customers/RagSoc], [~/Customers].[AddressID] AS [~/Customers/AddressID] FROM [TABCustomers] AS [~/Customers] WHERE (([~/Customers].[CustomerID] <= @p0 OR [~/Customers].[CustomerID] >= @p1) AND [~/Customers].[CustomerID] = @p2)";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_filter_collection_or_and_2()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=ID le 1 or ID ge 3 and ID eq 2" })
                .ToSQL();

            string expected = @"SELECT [~/Customers].[CustomerID] AS [~/Customers/:key], [~/Customers].[CustomerID] AS [~/Customers/ID], [~/Customers].[RagSoc] AS [~/Customers/RagSoc], [~/Customers].[AddressID] AS [~/Customers/AddressID] FROM [TABCustomers] AS [~/Customers] WHERE ([~/Customers].[CustomerID] <= @p0 OR ([~/Customers].[CustomerID] >= @p1 AND [~/Customers].[CustomerID] = @p2))";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_filter_collection_or_and_3()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=ID eq 1 and ID eq 1 or ID eq 1 and ((ID eq 1 or ID eq 1 or ID eq 1) and ID eq 1)" })
                .ToSQL();

            string expected = @"SELECT [~/Customers].[CustomerID] AS [~/Customers/:key], [~/Customers].[CustomerID] AS [~/Customers/ID], [~/Customers].[RagSoc] AS [~/Customers/RagSoc], [~/Customers].[AddressID] AS [~/Customers/AddressID] FROM [TABCustomers] AS [~/Customers] WHERE (([~/Customers].[CustomerID] = @p0 AND [~/Customers].[CustomerID] = @p1) OR ([~/Customers].[CustomerID] = @p2 AND ([~/Customers].[CustomerID] = @p3 OR [~/Customers].[CustomerID] = @p4 OR [~/Customers].[CustomerID] = @p5) AND [~/Customers].[CustomerID] = @p6))";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_filter_collection_on_nav_prop_1()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Orders?$filter=Customer/ID eq 1" })
                .ToSQL();

            string expected = @"SELECT [~/Orders].[OrderNumber] AS [~/Orders/:key], [~/Orders].[OrderNumber] AS [~/Orders/Number], [~/Orders].[OrderDate] AS [~/Orders/OrderDate], [~/Orders].[Note] AS [~/Orders/Note], [~/Orders].[Total] AS [~/Orders/Total], [~/Orders].[CustomerID] AS [~/Orders/CustomerID] FROM [TABOrders] AS [~/Orders] INNER JOIN [TABCustomers] AS [~/Orders/Customer] ON [~/Orders/Customer].[CustomerID] = [~/Orders].[CustomerID] WHERE [~/Orders/Customer].[CustomerID] = @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_filter_collection_on_nav_prop_2()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Orders?$filter=Customer/Address/City eq 'New York'" })
                .ToSQL();

            string expected = @"SELECT [~/Orders].[OrderNumber] AS [~/Orders/:key], [~/Orders].[OrderNumber] AS [~/Orders/Number], [~/Orders].[OrderDate] AS [~/Orders/OrderDate], [~/Orders].[Note] AS [~/Orders/Note], [~/Orders].[Total] AS [~/Orders/Total], [~/Orders].[CustomerID] AS [~/Orders/CustomerID] FROM [TABOrders] AS [~/Orders] INNER JOIN [TABCustomers] AS [~/Orders/Customer] ON [~/Orders/Customer].[CustomerID] = [~/Orders].[CustomerID]INNER JOIN [TABAddresses] AS [~/Orders/Customer/Address] ON [~/Orders/Customer/Address].[AddressID] = [~/Orders/Customer].[AddressID] WHERE [~/Orders/Customer/Address].[City] = @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_filter_collection_any()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection() 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=Orders/any(x:x/Total gt 100)" })
                .ToSQL();

            string expected = @"SELECT [~/Customers].[CustomerID] AS [~/Customers/:key], [~/Customers].[CustomerID] AS [~/Customers/ID], [~/Customers].[RagSoc] AS [~/Customers/RagSoc], [~/Customers].[AddressID] AS [~/Customers/AddressID] FROM [TABCustomers] AS [~/Customers] WHERE EXISTS (SELECT 1 FROM [TABOrders] AS [~/Customers/Orders] WHERE [~/Customers/Orders].[OrderNumber] = [~/Customers].[CustomerID] AND [~/Customers/Orders].[Total] > @p0)";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

    }
}
