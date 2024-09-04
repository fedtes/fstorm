using System;
using System.Collections;
using Microsoft.Extensions.DependencyInjection;

namespace FStorm.Test;

public class TestResults
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
        services.AddFStorm(MockModel.PrepareModel(), new FStormOptions() { SQLCompilerType= SQLCompilerType.MSSQL , ServiceRoot= "https://my.service/odata/", SQLConnection= null });
        serviceProvider = services.BuildServiceProvider();
    }


    [Test()]
    public void It_should_create_object_data(){
        EdmPathFactory factory = serviceProvider.GetService<EdmPathFactory>()!;
        DataTable dt = new DataTable();
        dt.Add(new Row() {
            {factory.Parse("#/Customer/:key"), 1},
            {factory.Parse("#/Customer/RagSoc"), "ACME"},
            {factory.Parse("#/Customer/ID"), 1},
            {factory.Parse("#/Customer/Orders/:key"), "O_1"},
            {factory.Parse("#/Customer/Orders/OrderNumber"), "O_1"}
        });

        dt.Add(new Row() {
            {factory.Parse("#/Customer/:key"), 1},
            {factory.Parse("#/Customer/RagSoc"), "ACME"},
            {factory.Parse("#/Customer/ID"), 1},
            {factory.Parse("#/Customer/Orders/:key"), "O_2"},
            {factory.Parse("#/Customer/Orders/OrderNumber"), "O_2"}
        });

        dt.Add(new Row() {
            {factory.Parse("#/Customer/:key"), 1},
            {factory.Parse("#/Customer/RagSoc"), "ACME"},
            {factory.Parse("#/Customer/ID"), 1},
            {factory.Parse("#/Customer/Orders/:key"), "O_3"},
            {factory.Parse("#/Customer/Orders/OrderNumber"), "O_3"}
        });

        dt.Add(new Row() {
            {factory.Parse("#/Customer/:key"), 2},
            {factory.Parse("#/Customer/RagSoc"), "ECorp"},
            {factory.Parse("#/Customer/ID"), 2},
            {factory.Parse("#/Customer/Orders/:key"), "O_5"},
            {factory.Parse("#/Customer/Orders/OrderNumber"), "O_5"}
        });

        dt.Add(new Row() {
            {factory.Parse("#/Customer/Orders/:key"), null},
            {factory.Parse("#/Customer/:key"), 3},
            {factory.Parse("#/Customer/RagSoc"), "DreamSolutions"},
            {factory.Parse("#/Customer/ID"), 3},
            {factory.Parse("#/Customer/Orders/OrderNumber"), null}
        });


        dt.Add(new Row() {
            {factory.Parse("#/Customer/:key"), 1},
            {factory.Parse("#/Customer/RagSoc"), "ACME"},
            {factory.Parse("#/Customer/Orders/:key"), "O_4"},
            {factory.Parse("#/Customer/ID"), 1},
            {factory.Parse("#/Customer/Orders/OrderNumber"), "O_4"}
        });


        var ranges= dt.GetHRanges();

        DataObjects @do= new DataObjects(dt);
        Assert.That(@do.Count(), Is.EqualTo(3));
        Assert.That((@do.First()["Orders"] as DataObjects).Count, Is.EqualTo(4));
        Assert.That(@do.First()["RagSoc"], Is.EqualTo("ACME"));

        //Assert.AreEqual(2, ranges.Count());
    }
}
