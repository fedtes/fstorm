using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;

namespace FStorm.Test;

public class MockPlugin : IOnEntityAccess
{
    public TestResult test;

    public MockPlugin(TestResult test) {
        this.test = test;
    }
    public string EntityName => "my.Customer";

    public void OnAccess(IEntityAccessContext accessContext)
    {
        test.Success = accessContext.GetTableString();

        accessContext.AddFrom((EdmEntityType)accessContext.Me.Type, accessContext.Me.ResourcePath);
        PropertyReference reference = new PropertyReference() 
        {
            Property = (EdmStructuralProperty)accessContext.Me.Type.StructuralProperties().First(x => x.Name =="RagSoc"),
            ResourcePath = accessContext.Me.ResourcePath
        };
        accessContext.AddFilter(new BinaryFilter() {PropertyReference = reference,OperatorKind= Microsoft.OData.UriParser.BinaryOperatorKind.Equal, Value="ACME"});
        accessContext.AddSelectAll(accessContext.Me.ResourcePath, (EdmEntityType)accessContext.Me.Type);
        accessContext.Kind = IEntityAccessContext.NEST_QUERY;
    }
}

public class TestResult
{
    public string Success = "false";
}

public class PluginTest
{
        [Test]
        public void It_should_exec_plugin_OnEntityAccess()
        {
            var services = new ServiceCollection();
            services.AddSingleton<TestResult>(x => new TestResult());
            services.AddFStorm(new ODataOptions() { SQLCompilerType= SQLCompilerType.MSSQL , ServiceRoot= "https://my.service/odata/"})
                .AddOnEntityAccess<MockPlugin>();
            var serviceProvider = services.BuildServiceProvider();
            MockModel.PrepareModel(serviceProvider.GetService<ODataService>()!.CreateNewModel());
            var r = serviceProvider.GetService<ODataService>()!.OpenConnection(new FakeConnection()).Get("Customers?$filter=ID eq 1").ToSQL();
            var t = serviceProvider.GetService<TestResult>()!;
            Assert.That(t.Success, Is.EqualTo("TABCustomers"));
            Assert.That(r.Statement, Is.EqualTo("SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM (SELECT [X1].[CustomerID], [X1].[RagSoc], [X1].[AddressID] FROM [TABCustomers] AS [X1] WHERE [X1].[RagSoc] = @p0) AS [P1] WHERE [P1].[CustomerID] = @p1"));

        }


}
