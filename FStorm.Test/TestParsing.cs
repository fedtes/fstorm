﻿using Microsoft.Data.Sqlite;
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
            services.AddFStorm(MockModel.PrepareModel(), new FStormOptions() { SQLCompilerType= SQLCompilerType.MSSQL , ServiceRoot= "https://my.service/odata/"});
            serviceProvider = services.BuildServiceProvider();
        }


        [Test]
        public void It_should_parse_path_to_collection()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService
                .OpenConnection(new FakeConnection())
                .Get(new GetRequest() { ResourcePath = "Customers" })
                .ToSQL();

            string expected = "SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM [TABCustomers] AS [P1]";
            Assert.That(_SqlQuery.Statement, Is.EqualTo(expected));
        }

        [Test]
        public void It_should_parse_path_to_entity()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService
                .OpenConnection(new FakeConnection())
                .Get(new GetRequest() { ResourcePath = "Customers(1)" })
                .ToSQL();

            string expected = "SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM [TABCustomers] AS [P1] WHERE [P1].[CustomerID] = @p0";
            Assert.That(_SqlQuery.Statement, Is.EqualTo(expected));
        }

        [Test]
        public void It_should_parse_path_to_structural_property()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection())
                .Get(new GetRequest() { ResourcePath = "Customers(1)/RagSoc" })
                .ToSQL();

            string expected = "SELECT [P1].[RagSoc] AS [P1/RagSoc], [P1].[CustomerID] AS [P1/:key] FROM [TABCustomers] AS [P1] WHERE [P1].[CustomerID] = @p0";
            Assert.That(_SqlQuery.Statement, Is.EqualTo(expected));
        }


        [Test]
        public void It_should_parse_path_to_nav_prop_n_cardinality()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection())
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders" })
                .ToSQL();

            string expected = @"SELECT [P2].[OrderNumber] AS [P2/:key], [P2].[OrderNumber] AS [P2/Number], [P2].[OrderDate] AS [P2/OrderDate], [P2].[Note] AS [P2/Note], [P2].[Total] AS [P2/Total], [P2].[DeliveryAddressID] AS [P2/DeliveryAddressID], [P2].[CustomerID] AS [P2/CustomerID] FROM [TABCustomers] AS [P1] LEFT JOIN [TABOrders] AS [P2] ON [P2].[CustomerID] = [P1].[CustomerID] WHERE [P1].[CustomerID] = @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_parse_path_to_nav_prop_n_cardinality_with_id()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection())
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders('O24-01')" })
                .ToSQL();

            string expected = @"SELECT [P2].[OrderNumber] AS [P2/:key], [P2].[OrderNumber] AS [P2/Number], [P2].[OrderDate] AS [P2/OrderDate], [P2].[Note] AS [P2/Note], [P2].[Total] AS [P2/Total], [P2].[DeliveryAddressID] AS [P2/DeliveryAddressID], [P2].[CustomerID] AS [P2/CustomerID] FROM [TABCustomers] AS [P1] LEFT JOIN [TABOrders] AS [P2] ON [P2].[CustomerID] = [P1].[CustomerID] WHERE [P1].[CustomerID] = @p0 AND [P2].[OrderNumber] = @p1";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_parse_path_to_nav_prop_n_cardinality_with_id_to_structured_prop()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection())
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders('O24-01')/OrderDate" })
                .ToSQL();

            string expected = @"SELECT [P2].[OrderDate] AS [P2/OrderDate], [P2].[OrderNumber] AS [P2/:key] FROM [TABCustomers] AS [P1] LEFT JOIN [TABOrders] AS [P2] ON [P2].[CustomerID] = [P1].[CustomerID] WHERE [P1].[CustomerID] = @p0 AND [P2].[OrderNumber] = @p1";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_parse_path_to_nav_prop_n_cardinality_with_id_to_nav_prop()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection())
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders('O24-01')/Customer" })
                .ToSQL();

            string expected = @"SELECT [P3].[CustomerID] AS [P3/:key], [P3].[CustomerID] AS [P3/ID], [P3].[RagSoc] AS [P3/RagSoc], [P3].[AddressID] AS [P3/AddressID] FROM [TABCustomers] AS [P1] LEFT JOIN [TABOrders] AS [P2] ON [P2].[CustomerID] = [P1].[CustomerID]LEFT JOIN [TABCustomers] AS [P3] ON [P3].[CustomerID] = [P2].[CustomerID] WHERE [P1].[CustomerID] = @p0 AND [P2].[OrderNumber] = @p1";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_parse_path_to_nav_prop_1_cardinality()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Orders('O24-01')/Customer" })
                .ToSQL();

            string expected = @"SELECT [P2].[CustomerID] AS [P2/:key], [P2].[CustomerID] AS [P2/ID], [P2].[RagSoc] AS [P2/RagSoc], [P2].[AddressID] AS [P2/AddressID] FROM [TABOrders] AS [P1] LEFT JOIN [TABCustomers] AS [P2] ON [P2].[CustomerID] = [P1].[CustomerID] WHERE [P1].[OrderNumber] = @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_count()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders/$count" })
                .ToSQL();

            string expected = @"SELECT COUNT([P2].[OrderNumber]) AS [count] FROM [TABCustomers] AS [P1] LEFT JOIN [TABOrders] AS [P2] ON [P2].[CustomerID] = [P1].[CustomerID] WHERE [P1].[CustomerID] = @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_address_subset_of_collecton_1()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders/$filter(Total gt 100)" })
                .ToSQL();

            string expected = @"SELECT [P1].[OrderNumber] AS [P1/:key], [P1].[OrderNumber] AS [P1/Number], [P1].[OrderDate] AS [P1/OrderDate], [P1].[Note] AS [P1/Note], [P1].[Total] AS [P1/Total], [P1].[DeliveryAddressID] AS [P1/DeliveryAddressID], [P1].[CustomerID] AS [P1/CustomerID] FROM (SELECT [P2].[OrderNumber] AS [P2/:key], [P2].[OrderNumber] AS [P2/Number], [P2].[OrderDate] AS [P2/OrderDate], [P2].[Note] AS [P2/Note], [P2].[Total] AS [P2/Total], [P2].[DeliveryAddressID] AS [P2/DeliveryAddressID], [P2].[CustomerID] AS [P2/CustomerID] FROM [TABCustomers] AS [P1] LEFT JOIN [TABOrders] AS [P2] ON [P2].[CustomerID] = [P1].[CustomerID] WHERE [P1].[CustomerID] = @p0) AS [P1] WHERE [P1].[Total] > @p1";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_address_property_of_subset_of_collecton()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders/$filter(Total gt 100)/1/OrderDate" })
                .ToSQL();

            string expected = @"SELECT [P1].[OrderDate] AS [P1/OrderDate], [P1].[OrderNumber] AS [P1/:key] FROM (SELECT [P2].[OrderNumber] AS [P2/:key], [P2].[OrderNumber] AS [P2/Number], [P2].[OrderDate] AS [P2/OrderDate], [P2].[Note] AS [P2/Note], [P2].[Total] AS [P2/Total], [P2].[DeliveryAddressID] AS [P2/DeliveryAddressID], [P2].[CustomerID] AS [P2/CustomerID] FROM [TABCustomers] AS [P1] LEFT JOIN [TABOrders] AS [P2] ON [P2].[CustomerID] = [P1].[CustomerID] WHERE [P1].[CustomerID] = @p0) AS [P1] WHERE [P1].[Total] > @p1 AND [P1].[OrderNumber] = @p2";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_address_subset_of_collecton_2()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers(1)/Orders/$filter(@expr)?@expr=Total gt 100" })
                .ToSQL();

            string expected = @"SELECT [P2].[OrderNumber] AS [P2/:key], [P2].[OrderNumber] AS [P2/Number], [P2].[OrderDate] AS [P2/OrderDate], [P2].[Note] AS [P2/Note], [P2].[Total] AS [P2/Total], [P2].[CustomerID] AS [P2/CustomerID] FROM (SELECT [P2].[OrderNumber] AS [P2/:key], [P2].[OrderNumber] AS [P2/Number], [P2].[OrderDate] AS [P2/OrderDate], [P2].[Note] AS [P2/Note], [P2].[Total] AS [P2/Total], [P2].[CustomerID] AS [P2/CustomerID] FROM [TABCustomers] AS [P1] LEFT JOIN [TABOrders] AS [P2] ON [P2].[CustomerID] = [P1].[CustomerID] WHERE [P1].[CustomerID] = @p0) AS [P2] WHERE [P2].[Total] = @p1";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_filter_collection_eq()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=RagSoc eq 'Acme'" })
                .ToSQL();

            string expected = @"SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM [TABCustomers] AS [P1] WHERE [P1].[RagSoc] = @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_filter_collection_ne()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=RagSoc ne 'Acme'" })
                .ToSQL();

            string expected = @"SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM [TABCustomers] AS [P1] WHERE [P1].[RagSoc] <> @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_filter_collection_gt()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=ID gt 10" })
                .ToSQL();

            string expected = @"SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM [TABCustomers] AS [P1] WHERE [P1].[CustomerID] > @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_filter_collection_ge()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=ID ge 10" })
                .ToSQL();

            string expected = @"SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM [TABCustomers] AS [P1] WHERE [P1].[CustomerID] >= @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_filter_collection_lt()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=ID lt 10" })
                .ToSQL();

            string expected = @"SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM [TABCustomers] AS [P1] WHERE [P1].[CustomerID] < @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_filter_collection_le()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=ID le 10" })
                .ToSQL();

            string expected = @"SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM [TABCustomers] AS [P1] WHERE [P1].[CustomerID] <= @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_filter_collection_and()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=ID ge 1 and RagSoc eq 'acme'" })
                .ToSQL();

            string expected = @"SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM [TABCustomers] AS [P1] WHERE ([P1].[CustomerID] >= @p0 AND [P1].[RagSoc] = @p1)";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_filter_collection_and_2()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=ID ge 1 and ID le 2 and RagSoc eq 'acme'" })
                .ToSQL();

            string expected = @"SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM [TABCustomers] AS [P1] WHERE ([P1].[CustomerID] >= @p0 AND [P1].[CustomerID] <= @p1 AND [P1].[RagSoc] = @p2)";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_filter_collection_or()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=ID le 1 or ID ge 2" })
                .ToSQL();

            string expected = @"SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM [TABCustomers] AS [P1] WHERE ([P1].[CustomerID] <= @p0 OR [P1].[CustomerID] >= @p1)";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_filter_collection_or_2()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=ID le 1 or ID ge 3 or ID eq 2" })
                .ToSQL();

            string expected = @"SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM [TABCustomers] AS [P1] WHERE ([P1].[CustomerID] <= @p0 OR [P1].[CustomerID] >= @p1 OR [P1].[CustomerID] = @p2)";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_filter_collection_or_and()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=(ID le 1 or ID ge 3) and ID eq 2" })
                .ToSQL();

            string expected = @"SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM [TABCustomers] AS [P1] WHERE (([P1].[CustomerID] <= @p0 OR [P1].[CustomerID] >= @p1) AND [P1].[CustomerID] = @p2)";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_filter_collection_or_and_2()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=ID le 1 or ID ge 3 and ID eq 2" })
                .ToSQL();

            string expected = @"SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM [TABCustomers] AS [P1] WHERE ([P1].[CustomerID] <= @p0 OR ([P1].[CustomerID] >= @p1 AND [P1].[CustomerID] = @p2))";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_filter_collection_or_and_3()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=ID eq 1 and ID eq 1 or ID eq 1 and ((ID eq 1 or ID eq 1 or ID eq 1) and ID eq 1)" })
                .ToSQL();

            string expected = @"SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM [TABCustomers] AS [P1] WHERE (([P1].[CustomerID] = @p0 AND [P1].[CustomerID] = @p1) OR ([P1].[CustomerID] = @p2 AND ([P1].[CustomerID] = @p3 OR [P1].[CustomerID] = @p4 OR [P1].[CustomerID] = @p5) AND [P1].[CustomerID] = @p6))";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_filter_collection_on_nav_prop_1()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Orders?$filter=Customer/ID eq 1" })
                .ToSQL();

            string expected = @"SELECT [P1].[OrderNumber] AS [P1/:key], [P1].[OrderNumber] AS [P1/Number], [P1].[OrderDate] AS [P1/OrderDate], [P1].[Note] AS [P1/Note], [P1].[Total] AS [P1/Total], [P1].[DeliveryAddressID] AS [P1/DeliveryAddressID], [P1].[CustomerID] AS [P1/CustomerID] FROM [TABOrders] AS [P1] LEFT JOIN [TABCustomers] AS [P2] ON [P2].[CustomerID] = [P1].[CustomerID] WHERE [P2].[CustomerID] = @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_should_filter_collection_on_nav_prop_2()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Orders?$filter=Customer/Address/City eq 'New York'" })
                .ToSQL();

            string expected = @"SELECT [P1].[OrderNumber] AS [P1/:key], [P1].[OrderNumber] AS [P1/Number], [P1].[OrderDate] AS [P1/OrderDate], [P1].[Note] AS [P1/Note], [P1].[Total] AS [P1/Total], [P1].[DeliveryAddressID] AS [P1/DeliveryAddressID], [P1].[CustomerID] AS [P1/CustomerID] FROM [TABOrders] AS [P1] LEFT JOIN [TABCustomers] AS [P2] ON [P2].[CustomerID] = [P1].[CustomerID]LEFT JOIN [TABAddresses] AS [P3] ON [P3].[AddressID] = [P2].[AddressID] WHERE [P3].[City] = @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_filter_collection_any()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=Orders/any(x:x/Total gt 100)" })
                .ToSQL();

            string expected = @"SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM [TABCustomers] AS [P1] WHERE EXISTS (SELECT 1 FROM [TABOrders] AS [ANY1] WHERE [ANY1].[CustomerID] = [P1].[CustomerID] AND [ANY1].[Total] > @p0)";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_filter_collection_any_2()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Orders?$filter=DeliveryAddress/Hints/any(x:x/Hint eq 'dangerous dog!')" })
                .ToSQL();

            string expected = @"SELECT [P1].[OrderNumber] AS [P1/:key], [P1].[OrderNumber] AS [P1/Number], [P1].[OrderDate] AS [P1/OrderDate], [P1].[Note] AS [P1/Note], [P1].[Total] AS [P1/Total], [P1].[DeliveryAddressID] AS [P1/DeliveryAddressID], [P1].[CustomerID] AS [P1/CustomerID] FROM [TABOrders] AS [P1] WHERE EXISTS (SELECT 1 FROM [TABAddresses] AS [ANY1] LEFT JOIN [TABAddressHints] AS [ANY2] ON [ANY2].[AddressID] = [ANY1].[AddressID] WHERE [ANY1].[AddressID] = [P1].[DeliveryAddressID] AND [ANY2].[Hint] = @p0)";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_filter_collection_all()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=Orders/all(x:x/Total gt 100)" })
                .ToSQL();

            string expected = @"SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM [TABCustomers] AS [P1] WHERE NOT EXISTS (SELECT 1 FROM [TABOrders] AS [ALL1] WHERE [ALL1].[CustomerID] = [P1].[CustomerID] AND NOT ([ALL1].[Total] > @p0))";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        public void It_should_filter_collection_all_2()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=Orders/all(x:x/Total gt 100 and x/Total lt 200)" })
                .ToSQL();

            string expected = @"SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM [TABCustomers] AS [P1] WHERE NOT EXISTS (SELECT 1 FROM [TABOrders] AS [ALL1] WHERE [ALL1].[CustomerID] = [P1].[CustomerID] AND NOT (([ALL1].[Total] > @p0 AND [ALL1].[Total] < @p1)))";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        [Test]
        [TestCase("contains(RagSoc,'acme')","%acme%")]
        [TestCase("endswith(RagSoc,'acme')","%acme")]
        [TestCase("startswith(RagSoc,'acme')","acme%")]
        public void It_should_filter_using_like(string filter,string p0)
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers?$filter=" + filter })
                .ToSQL();

            string expected = @"SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM [TABCustomers] AS [P1] WHERE [P1].[RagSoc] like @p0";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
            Assert.That(_SqlQuery.Bindings["@p0"], Is.EqualTo(p0));
        }


        [Test]
        public void It_should_select()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService.OpenConnection(new FakeConnection()) 
                .Get(new GetRequest() { ResourcePath = "Customers?$select=RagSoc" })
                .ToSQL();

            string expected = @"SELECT [P1].[RagSoc] AS [P1/RagSoc], [P1].[CustomerID] AS [P1/:key] FROM [TABCustomers] AS [P1]";
            Assert.That(_SqlQuery.Statement.Replace("\n", ""), Is.EqualTo(expected));
        }


        /*
        String and Collection Functions


contains    contains
endswith    endswith(CompanyName,'Futterkiste')
startswith  startswith(CompanyName,’Alfr’)
now StartTime ge now()

indexof     indexof(CompanyName,'lfreds') eq 1
length      length(CompanyName) eq 19
substring   substring(CompanyName,1) eq 'lfreds Futterkiste'

Collection Functions

hassubset   hassubset([4,1,3],[3,1])
hassubsequence hassubsequence([4,1,3,1],[1,1])
String Functions

matchesPattern  matchesPattern(CompanyName,'%5EA.*e$')

tolower     tolower(CompanyName) eq 'alfreds futterkiste'

toupper     toupper(CompanyName) eq 'ALFREDS FUTTERKISTE'

trim        trim(CompanyName) eq 'Alfreds Futterkiste'

Date and Time Functions

day         day(StartTime) eq 8

date        date(StartTime) ne date(EndTime)

fractionalseconds   second(StartTime) eq 0

hour    hour(StartTime) eq 1

maxdatetime EndTime eq maxdatetime()

mindatetime StartTime eq mindatetime()

minute  minute(StartTime) eq 0

month   month(BirthDate) eq 12


second  second(StartTime) eq 0

time    time(StartTime) le StartOfDay

totaloffsetminutes  totaloffsetminutes(StartTime) eq 60

totalseconds    totalseconds(duration'PT1M') eq 60

year    year(BirthDate) eq 0

Arithmetic Functions

ceiling ceiling(Freight) eq 33

floor   floor(Freight) eq 32

round   round(Freight) eq 32

Type Functions

cast    cast(ShipCountry,Edm.String)

isof    isof(NorthwindModel.Order)

isof    isof(ShipCountry,Edm.String)


        */

    }
}
