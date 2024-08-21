using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace FStorm
{
    public static class ServiceCollectionExtensions
    {
        public static void AddFStorm(this IServiceCollection services)
        {
            services.AddSingleton<FStormService>();
            services.AddTransient<GetCommand>();
        }
    }

    public class FStormService
    {

        IServiceProvider serviceProvider;
        public FStormService(IServiceProvider serviceProvider) {
            this.serviceProvider = serviceProvider;
        }

        public GetCommand Get() => serviceProvider.GetService<GetCommand>();
    }

    public class Command
    {
        public readonly string CommandId;
        protected readonly IServiceProvider serviceProvider;

        public Command(IServiceProvider serviceProvider) 
        {
            CommandId = Guid.NewGuid().ToString();
            this.serviceProvider = serviceProvider;
        }

        public virtual string ToSQLString()
        {
            return String.Empty;
        }
    }


    public class GetCommand :Command
    {
        public GetCommand(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public GetCommand Path(params string[] path) 
        {
            return this;
        }

    }

}