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
        DataTable dt = new DataTable(factory.Parse("#/Customer"));
        dt.AddColumn(factory.Parse("#/Customer/:key"));
        dt.AddColumn(factory.Parse("#/Customer/RagSoc"));
        dt.AddColumn(factory.Parse("#/Customer/ID"));
        dt.AddColumn(factory.Parse("#/Customer/Orders/:key"));
        dt.AddColumn(factory.Parse("#/Customer/Orders/OrderNumber"));
        
        var r = dt.CreateRow();
        r[factory.Parse("#/Customer/:key")]=1;
        r[factory.Parse("#/Customer/RagSoc")]="ACME";
        r[factory.Parse("#/Customer/ID")]=1;
        r[factory.Parse("#/Customer/Orders/:key")]="O_1";
        r[factory.Parse("#/Customer/Orders/OrderNumber")]="O_1";

        r = dt.CreateRow();
        r[factory.Parse("#/Customer/:key")]=1;
        r[factory.Parse("#/Customer/RagSoc")]="ACME";
        r[factory.Parse("#/Customer/ID")]=1;
        r[factory.Parse("#/Customer/Orders/:key")]="O_2";
        r[factory.Parse("#/Customer/Orders/OrderNumber")]="O_2";

        r = dt.CreateRow();
        r[factory.Parse("#/Customer/:key")]=1;
        r[factory.Parse("#/Customer/RagSoc")]="ACME";
        r[factory.Parse("#/Customer/ID")]=1;
        r[factory.Parse("#/Customer/Orders/:key")]="O_3";
        r[factory.Parse("#/Customer/Orders/OrderNumber")]="O_3";

        r = dt.CreateRow();
        r[factory.Parse("#/Customer/:key")]=2;
        r[factory.Parse("#/Customer/RagSoc")]="ECorp";
        r[factory.Parse("#/Customer/ID")]=2;
        r[factory.Parse("#/Customer/Orders/:key")]="O_5";
        r[factory.Parse("#/Customer/Orders/OrderNumber")]="O_5";

        r = dt.CreateRow();
        r[factory.Parse("#/Customer/Orders/:key")]=null;
        r[factory.Parse("#/Customer/:key")]=3;
        r[factory.Parse("#/Customer/RagSoc")]="DreamSolutions";
        r[factory.Parse("#/Customer/ID")]=3;
        r[factory.Parse("#/Customer/Orders/OrderNumber")]=null;
    
        r = dt.CreateRow();
        r[factory.Parse("#/Customer/:key")]=1;
        r[factory.Parse("#/Customer/RagSoc")]="ACME";
        r[factory.Parse("#/Customer/Orders/:key")]="O_4";
        r[factory.Parse("#/Customer/ID")]=1;
        r[factory.Parse("#/Customer/Orders/OrderNumber")]="O_4";

        var dos= dt.ToDataObjects();

        Assert.That(dos.Count, Is.EqualTo(3));
        Assert.That(dos.First()["ID"], Is.EqualTo(1));
        Assert.That((dos.First()["Orders"] as DataObjects)!.Count, Is.EqualTo(4));
        Assert.That((dos.First()["Orders"] as DataObjects)!.Last()["OrderNumber"], Is.EqualTo("O_4"));
        Assert.That((dos.First(x=> x["RagSoc"]=="DreamSolutions")["Orders"] as DataObjects)!.Count, Is.EqualTo(0));
    }
}
