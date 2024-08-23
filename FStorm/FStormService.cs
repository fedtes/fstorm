using Microsoft.Extensions.DependencyInjection;

namespace FStorm
{
    public static class ServiceCollectionExtensions
    {
        public static void AddFStorm(this IServiceCollection services, EdmModel model, string serviceRoot, FStormOptions options)
        {
            services.AddSingleton(p => new FStormService(p, model, serviceRoot, options));
            services.AddSingleton<EdmPathFactory>();
            services.AddTransient<GetCommand>();
            services.AddTransient<GetCompiler>();
            services.AddTransient<PathCompiler>();
            services.AddTransient<ResourceRootCompiler>();
            services.AddTransient<NavigationPropertyCompiler>();
            services.AddTransient<SelectPropertyCompiler>();
            services.AddTransient<BinaryFilterCompiler>();
        }
    }

    public enum SQLCompilerType
    {
        MSSQL
    }


    public class FStormOptions
    {
        public SQLCompilerType SQLCompilerType { get; set; }
    }

    public class FStormService
    {
        IServiceProvider serviceProvider;
        internal readonly FStormOptions options;

        public FStormService(IServiceProvider serviceProvider, EdmModel model, string serviceRoot, FStormOptions options) {
            this.serviceProvider = serviceProvider;
            Model = model;
            this.options = options;
            ServiceRoot = new Uri(serviceRoot);
        }

        public EdmModel Model { get; }
        public Uri ServiceRoot { get; }

        public GetCommand Get(GetConfiguration configuration)  
        { 
            var cmd = serviceProvider.GetService<GetCommand>()!; 
            cmd.Configuration= configuration;
            return cmd;
        }
    }

}