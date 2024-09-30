using Microsoft.Extensions.DependencyInjection;

namespace FStorm.Test
{
    internal class TestEdmPath
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
            services.AddFStorm(MockModel.PrepareModel(), new FStormOptions() { SQLCompilerType = SQLCompilerType.MSSQL, ServiceRoot= "https://my.service/odata/" });
            serviceProvider = services.BuildServiceProvider();
        }


        [Test]
        public void It_should_create_1_entity_path()
        {
            var factory = serviceProvider.GetService<EdmPathFactory>()!;
            var path = factory.CreatePath("Customers");
            Assert.That(path.ToString(), Is.EqualTo("~/Customers"));
        }


        [Test]
        public void It_should_create_complete_path()
        {
            var factory = serviceProvider.GetService<EdmPathFactory>()!;
            var path = factory.CreatePath("Customers", "Orders");
            Assert.That(path.ToString(), Is.EqualTo("~/Customers/Orders"));
        }


        [Test]
        public void It_should_combine_path()
        {
            var factory = serviceProvider.GetService<EdmPathFactory>()!;
            var path = factory.CreatePath("Customers");
            var path1 = path + "Orders";
            Assert.That(path.ToString(), Is.EqualTo("~/Customers"));
            Assert.That(path1.ToString(), Is.EqualTo("~/Customers/Orders"));
        }

        [Test]
        public void It_should_pop_from_path()
        {
            var factory = serviceProvider.GetService<EdmPathFactory>()!;
            var path = factory.CreatePath("Customers");
            var path1 = path + "Orders";
            var path2 = path1 - 1;
            Assert.That(path.ToString(), Is.EqualTo("~/Customers"));
            Assert.That(path1.ToString(), Is.EqualTo("~/Customers/Orders"));
            Assert.That(path2.ToString(), Is.EqualTo("~/Customers"));
        }

        [Test]
        public void It_should_throw_on_invalid_pop_len()
        {
            var factory = serviceProvider.GetService<EdmPathFactory>()!;
            var path = factory.CreatePath("Customers") + "Orders";
            Assert.Throws<ArgumentException>(() => { var x = path - 3; });
        }


        [Test]
        public void It_should_throw_on_wrong_segment_format()
        {
            var factory = serviceProvider.GetService<EdmPathFactory>()!;
            Assert.Throws<ArgumentException>(() => factory.CreatePath("Cus/tomers"));
        }


        [Test]
        public void It_should_sort_datatable_columns()
        {
            EdmPathFactory factory = serviceProvider.GetService<EdmPathFactory>()!;
            DataTable dt = new DataTable(factory.ParseString("~/Customer"));
            dt.AddColumn(factory.ParseString("~/Customer/:key"));
            dt.AddColumn(factory.ParseString("~/Customer/Orders/OrderNumber"));
            dt.AddColumn(factory.ParseString("~/Customer/ID"));
            dt.AddColumn(factory.ParseString("~/Customer/Orders/:key"));
            dt.AddColumn(factory.ParseString("~/Customer/RagSoc"));

            var s = dt.SortedColumns();
            Assert.That(s[0].ToString(), Is.EqualTo("~/Customer/:key"));
            Assert.That(s[1].ToString(), Is.EqualTo("~/Customer/ID"));
            Assert.That(s[2].ToString(), Is.EqualTo("~/Customer/RagSoc"));
            Assert.That(s[3].ToString(), Is.EqualTo("~/Customer/Orders/:key"));
            Assert.That(s[4].ToString(), Is.EqualTo("~/Customer/Orders/OrderNumber"));
        }

    }
}
