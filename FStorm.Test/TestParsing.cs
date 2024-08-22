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
            services.AddFStorm(MockModel.PrepareModel(), "https://my.service/odata/", new FStormOptions() { SQLCompilerType= SQLCompilerType.MSSQL});
            serviceProvider = services.BuildServiceProvider();
        }


        [Test]
        public void It_should_parse_path_to_collection()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService
                .Get(new GetConfiguration() { ResourcePath = "Customers" })
                .ToSQL();

            string expected = "SELECT [#/Customer].[CustomerID] AS [#/Customer/ID], [#/Customer].[RagSoc] AS [#/Customer/RagSoc] FROM [TABCustomers] AS [#/Customer]";
            Assert.That(_SqlQuery.Statement, Is.EqualTo(expected));
        }

        [Test]
        public void It_should_parse_path_to_entity()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService
                .Get(new GetConfiguration() { ResourcePath = "Customers(1)" })
                .ToSQL();

            string expected = "SELECT [#/Customer].[CustomerID] AS [#/Customer/ID], [#/Customer].[RagSoc] AS [#/Customer/RagSoc] FROM [TABCustomers] AS [#/Customer] WHERE [#/Customer].[CustomerID] = @p0";
            Assert.That(_SqlQuery.Statement, Is.EqualTo(expected));
        }

        [Test]
        public void It_should_parse_path_to_structural_property()
        {
            var _FStormService = serviceProvider.GetService<FStormService>()!;
            var _SqlQuery = _FStormService
                .Get(new GetConfiguration() { ResourcePath = "Customers(1)/RagSoc" })
                .ToSQL();

            string expected = "SELECT [#/Customer].[RagSoc] AS [#/Customer/RagSoc] FROM [TABCustomers] AS [#/Customer] WHERE [#/Customer].[CustomerID] = @p0";
            Assert.That(_SqlQuery.Statement, Is.EqualTo(expected));
        }

    }
}
