using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;

namespace FStorm.Test;

public class OnEntityAccessMockPlugin : IOnEntityAccess
{
    public TestResult test;
    private readonly IEdmModel model;

    public OnEntityAccessMockPlugin(TestResult test, IEdmModel model) {
        this.test = test;
        this.model = model;
    }
    public string EntityName => "my.Customer";

    public void OnAccess(IEntityAccessContext accessContext)
    {
        test.Success = accessContext.GetTableString();
        //model.FindDeclaredEntitySet().EntityType
        accessContext.AddFrom(accessContext.Me.Type, accessContext.Me.ResourcePath);
        PropertyReference reference = accessContext.Me.GetStructuralProperty("RagSoc");
        accessContext.AddFilter(new BinaryFilter(reference,FilterOperatorKind.Equal,"ACME"));
        accessContext.AddSelectAll(accessContext.Me.ResourcePath, accessContext.Me.Type);
        accessContext.Kind = IEntityAccessContext.NEST_QUERY;
    }
}


public class OnPropertyNavigationPlugin : IOnPropertyNavigation
{
    public TestResult test;
    private readonly IEdmModel model;

    public OnPropertyNavigationPlugin(TestResult test, IEdmModel model) {
        this.test = test;
        this.model = model;
    }
    public string EntityName => "my.Customer";

    public string PropertyName => "Orders";

    public void OnNavigation(IOnPropertyNavigationContext navigationContext)
    {
        var l = navigationContext.Left;
        var r = navigationContext.Right;

        navigationContext.JoinCondition.Add(new PropertyFilter() {LeftPropertyReference=l.GetStructuralProperty("Number"), RightPropertyReference=r.GetStructuralProperty("ID")});
        navigationContext.CustomizedJoin = true;
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
                .AddOnEntityAccess<OnEntityAccessMockPlugin>();
            var serviceProvider = services.BuildServiceProvider();
            MockModel.PrepareModel(serviceProvider.GetService<ODataService>()!.CreateNewModel());
            var r = serviceProvider.GetService<ODataService>()!.OpenConnection(new FakeConnection()).Get("Customers?$filter=ID eq 1").ToSQL();
            var t = serviceProvider.GetService<TestResult>()!;
            Assert.That(t.Success, Is.EqualTo("TABCustomers"));
            Assert.That(r.Statement, Is.EqualTo("SELECT [P1].[CustomerID] AS [P1/:key], [P1].[CustomerID] AS [P1/ID], [P1].[RagSoc] AS [P1/RagSoc], [P1].[AddressID] AS [P1/AddressID] FROM (SELECT [X1].[CustomerID], [X1].[RagSoc], [X1].[AddressID] FROM [TABCustomers] AS [X1] WHERE [X1].[RagSoc] = @p0) AS [P1] WHERE [P1].[CustomerID] = @p1"));
        }

        [Test]
        public void It_should_exec_plugin_OnNavigationProperties()
        {
            var services = new ServiceCollection();
            services.AddSingleton<TestResult>(x => new TestResult());
            services.AddFStorm(new ODataOptions() { SQLCompilerType= SQLCompilerType.MSSQL , ServiceRoot= "https://my.service/odata/"})
                .AddOnPropertyNavigation<OnPropertyNavigationPlugin>();
            var serviceProvider = services.BuildServiceProvider();
            MockModel.PrepareModel(serviceProvider.GetService<ODataService>()!.CreateNewModel());
            var r = serviceProvider.GetService<ODataService>()!.OpenConnection(new FakeConnection()).Get("Customers(1)/Orders").ToSQL();
            
            //Assert.That(t.Success, Is.EqualTo("TABCustomers"));
            Assert.That(r.Statement, Is.EqualTo("SELECT [P2].[OrderNumber] AS [P2/:key], [P2].[OrderNumber] AS [P2/Number], [P2].[OrderDate] AS [P2/OrderDate], [P2].[Note] AS [P2/Note], [P2].[Total] AS [P2/Total], [P2].[DeliveryAddressID] AS [P2/DeliveryAddressID], [P2].[CustomerID] AS [P2/CustomerID] FROM [TABCustomers] AS [P1] LEFT JOIN [TABOrders] AS [P2] ON ([P2].[OrderNumber] = [P1].[CustomerID]) WHERE [P1].[CustomerID] = @p0"));
        }


}
