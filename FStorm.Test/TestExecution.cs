﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;

namespace FStorm.Test
{
    public class TestExecution
    {

        IServiceProvider serviceProvider;
#pragma warning disable NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
        SqliteConnection connection;
#pragma warning restore NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method

        [TearDown()]
        public void TearDown()
        {
            if (serviceProvider is IDisposable disposable)
                disposable.Dispose();

            //if (connection != null)
            //    connection.Close();
        }

        [SetUp]
        public void Setup()
        {
            connection = new SqliteConnection("Data Source=InMemorySample;Mode=Memory;Cache=Shared");
            connection.Open();
            MockModel.CreateDB(connection);
            var services = new ServiceCollection();
            services.AddFStorm(
                MockModel.PrepareModel(),
                new FStormOptions()
                {
                    SQLCompilerType = SQLCompilerType.SQLLite,
                    ServiceRoot = "https://my.service/odata/"
                }
            );
            serviceProvider = services.BuildServiceProvider();
        }

        [Test]
        public async Task It_Should_read_entity_collection()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var con = _FStormService.OpenConnection(connection);
            
            var r = await con.Get(new GetRequest() { RequestPath = "Customers" }).ToListAsync();

            var w = serviceProvider.GetService<Writer>()!;
            var sr = w.WriteResult(r);
            Assert.That(sr.ToString(), Is.EqualTo("{\"@odata.context\":\"https://my.service/odata/$metadata#Customers\",\"value\":[{\"ID\":1,\"RagSoc\":\"ACME\",\"AddressID\":null},{\"ID\":2,\"RagSoc\":\"ECorp\",\"AddressID\":null},{\"ID\":3,\"RagSoc\":\"DreamSolutions\",\"AddressID\":null}]}"));
        }

        [Test]
        public async Task It_Should_read_entity_single()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var con = _FStormService.OpenConnection(connection);

            var r = await con.Get(new GetRequest() { RequestPath = "Customers(1)" }).ToListAsync();

            var w = serviceProvider.GetService<Writer>()!;
            var sr = w.WriteResult(r);
            Assert.That(sr.ToString(), Is.EqualTo("{\"@odata.context\":\"https://my.service/odata/$metadata#Customers/$entity\",\"ID\":1,\"RagSoc\":\"ACME\",\"AddressID\":null}"));
        }

        [Test]
        public async Task It_Should_read_entity_property()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var con = _FStormService.OpenConnection(connection);

            var r = await con.Get(new GetRequest() { RequestPath = "Customers(1)/RagSoc" }).ToListAsync();

            var w = serviceProvider.GetService<Writer>()!;
            var sr = w.WriteResult(r);
            Assert.That(sr.ToString(), Is.EqualTo("{\"@odata.context\":\"https://my.service/odata/$metadata#Customers(1)/RagSoc\",\"RagSoc\":\"ACME\"}"));
        }


        [Test]
        public async Task It_Should_count_collection()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var con = _FStormService.OpenConnection(connection);

            var r = await con.Get(new GetRequest() { RequestPath = "Customers(1)/Orders/$count" }).ToListAsync();

            var w = serviceProvider.GetService<Writer>()!;
            var sr = w.WriteResult(r);
            Assert.That(sr.ToString(), Is.EqualTo("2"));
        }

    }
}
