using Microsoft.Extensions.DependencyInjection;
using FStorm;

namespace FStorm.Test
{

    [TestFixture]
    public class TestInitialization
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
        public void It_Should_Create_Service()
        {
            var _FStormService = serviceProvider.GetService<FStormService>();
            Assert.IsInstanceOf<FStormService>(_FStormService);
        }


        [Test]
        public void It_Should_Create_Commands()
        {
            var _FStormService = serviceProvider.GetService<FStormService>();
            if (_FStormService != null) 
            {
                var cmd1 = _FStormService.Get();
                var cmd2 = _FStormService.Get();
                Assert.True(cmd1.CommandId != cmd2.CommandId);
            }
        }


    }
}