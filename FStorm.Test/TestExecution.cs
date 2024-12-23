﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using System.Collections;
using System.Text.RegularExpressions;

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
            connection = new SqliteConnection("Data Source=.\\MockData;");
            connection.Open();
            var services = new ServiceCollection();
            services.AddFStorm(
                MockModel.PrepareModel(),
                new ODataOptions()
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
            
            var _FStormService = serviceProvider.GetService<ODataService>()!;
            var con = _FStormService.OpenConnection(connection);
            
            var r = (await con.Get("Customers").ToListAsync()).ToArray();
            Assert.That(r.Count, Is.EqualTo(3));
            Assert.That(r[0]["RagSoc"], Is.EqualTo("ACME"));
            Assert.That(r[1]["RagSoc"], Is.EqualTo("ECorp"));
            Assert.That(r[2]["RagSoc"], Is.EqualTo("DreamSolutions"));
        }


        [Test]
        [TestCase("Customers/$count","3")]
        [TestCase("Orders/$count","1000")]
        [TestCase("Customers(1)/Orders/$count","343")]
        [TestCase("Orders/$count?$filter=Customer/ID eq 1","343")]
        [TestCase("Orders/$count?$filter=CustomerID eq 1","343")]
        [TestCase("Orders/$filter(CustomerID eq 1)/$count","343")]
        [TestCase("Orders/$count?$filter=CustomerID eq 1 and Total gt 5000","176")]
        [TestCase("Customers(1)/Orders/$count?$filter=Total gt 5000","176")]
        [TestCase("Customers(1)/Orders/$count?$filter=Total gt 5000 and Articles/any(x:x/Name eq 'Wrangler')","2")]
        [TestCase("Orders/$count?$filter=CustomerID eq 1 or CustomerID eq 2","681")]
        public async Task It_Should_execute_count(string req, string exp)
        {
            
            var _FStormService = serviceProvider.GetService<ODataService>()!;
            var con = _FStormService.OpenConnection(connection);
            
            var r = (await con.Get(req).ToListAsync()).ToArray();
            Assert.That(r.First()["count"]?.ToString(), Is.EqualTo(exp));
        }

        [Test]
        [TestCase("Customers(2)?$select=RagSoc",1,"ECorp")]
        [TestCase("Orders?$select=Number, OrderDate, Note&$top=1&$orderby=OrderDate desc, Number desc", 3, "832")]
        [TestCase("Orders?$select=Note&$top=10&$filter=startswith(Note,'Major')&$orderby=Number desc", 1, "Major Pharmaceuticals")]
        public async Task It_Should_execute_select(string req,int colCount, string exp)
        {
            var _FStormService = serviceProvider.GetService<ODataService>()!;
            var con = _FStormService.OpenConnection(connection);
            
            var r = (await con.Get(req).ToListAsync()).ToArray();
            Assert.That(r.First().Count, Is.EqualTo(colCount));
            Assert.That(r.First()?.First().Value?.ToString(), Is.EqualTo(exp));
        }

        [Test]
        [TestCase("Customers?$expand=Address")]
        [TestCase("Customers(1)?$expand=Address")]
        public async Task It_Should_execute_expand(string req)
        {
            var _FStormService = serviceProvider.GetService<ODataService>()!;
            var con = _FStormService.OpenConnection(connection);
            var r = (await con.Get(req).ToListAsync()).ToArray();
            Assert.That(r[0]["RagSoc"], Is.EqualTo("ACME"));
            Assert.That((r[0]["Address"] as IDictionary<string,object?>)["Country"], Is.EqualTo("Indonesia"));
        }

        [Test]
        [TestCase("Customers(1)?$expand=Orders", 343)]
        [TestCase("Customers(1)?$expand=Orders($top=10)", 10)]
        [TestCase("Customers(1)?$expand=Orders($filter=Total le 1000)", 33)]
        [TestCase("Customers(1)?$expand=Orders($top=10;$expand=DeliveryAddress)", 10)]
        public async Task It_Should_execute_expand_2(string req, int cnt)
        {
            var _FStormService = serviceProvider.GetService<ODataService>()!;
            var con = _FStormService.OpenConnection(connection);
            var r = (await con.Get(req).ToListAsync()).ToArray();
            Assert.That(r[0]["RagSoc"], Is.EqualTo("ACME"));
            Assert.That((r[0]["Orders"] as IList).Count, Is.EqualTo(cnt));
        }

        [Test]
        [TestCase("Orders?$filter=Total le 1000&$top=10")]
        public async Task It_should_write_and_read_nextlink(string req)
        {
            var _FStormService = serviceProvider.GetService<ODataService>()!;
            var con = _FStormService.OpenConnection(connection);
            var s = (await con.Get(req).ToODataString());
            var r = (await con.Get(req).ToListAsync());
            Assert.That(s, Does.Contain("@odata.nextLink"));
            Assert.AreEqual(10, r.Count());
            var nextLink = new Regex("\"@odata.nextLink\":\"(?'nextlink'[^\"]+)\"").Match(s).Groups["nextlink"].Value;
            var r1 = (await con.Get(nextLink).ToListAsync());
            Assert.AreEqual(10, r1.Count());
            Assert.That(r1.First()[""], Is.EqualTo(""));
        }



        // [Test]
        // public async Task It_Should_read_entity_single()
        // {
        //     var _FStormService = serviceProvider.GetService<FStormService>()!;
        //     var con = _FStormService.OpenConnection(connection);

        //     var r = await con.Get("Customers(1)").ToListAsync_1();
        // Assert.That(sr.ToString(), Is.EqualTo("{\"@odata.context\":\"https://my.service/odata/$metadata#Customers\",\"value\":[{\"ID\":1,\"RagSoc\":\"ACME\",\"AddressID\":null},{\"ID\":2,\"RagSoc\":\"ECorp\",\"AddressID\":null},{\"ID\":3,\"RagSoc\":\"DreamSolutions\",\"AddressID\":null}]}"));
        //     var w = serviceProvider.GetService<Writer>()!;
        //     var sr = w.WriteResult(r);
        //     Assert.That(sr.ToString(), Is.EqualTo("{\"@odata.context\":\"https://my.service/odata/$metadata#Customers/$entity\",\"ID\":1,\"RagSoc\":\"ACME\",\"AddressID\":null}"));
        // }

        // [Test]
        // public async Task It_Should_read_entity_property()
        // {
        //     var _FStormService = serviceProvider.GetService<FStormService>()!;
        //     var con = _FStormService.OpenConnection(connection);

        //     var r = await con.Get("Customers(1)/RagSoc").ToListAsync_1();

        //     var w = serviceProvider.GetService<Writer>()!;
        //     var sr = w.WriteResult(r);
        //     Assert.That(sr.ToString(), Is.EqualTo("{\"@odata.context\":\"https://my.service/odata/$metadata#Customers(1)/RagSoc\",\"RagSoc\":\"ACME\"}"));
        // }


        // [Test]
        // public async Task It_Should_count_collection()
        // {
        //     var _FStormService = serviceProvider.GetService<FStormService>()!;
        //     var con = _FStormService.OpenConnection(connection);

        //     var r = await con.Get("Customers(1)/Orders/$count").ToListAsync_1();

        //     var w = serviceProvider.GetService<Writer>()!;
        //     var sr = w.WriteResult(r);
        //     Assert.That(sr.ToString(), Is.EqualTo("2"));
        // }

    }
}
