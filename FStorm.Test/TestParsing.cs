using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            services.AddFStorm();
            serviceProvider = services.BuildServiceProvider();
        }


        [Test]
        public void It_should_parse_entity_set()
        {
            var _FStormService = serviceProvider.GetService<FStormService>();
            var statement = _FStormService.Get().Path("Order").ToSQLString();
            Assert.AreEqual("",statement);
        }

    }
}
