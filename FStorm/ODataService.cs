using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Validation;
using System.Data.Common;
using System.Xml;

namespace FStorm
{
    public static class ServiceCollectionExtensions
    {
        public static PluginRegistration AddFStorm(this IServiceCollection services, ODataOptions options)
        {
            services.AddTransient<EdmModel>();
            services.AddTransient<Connection>();
            services.AddTransient<Transaction>();
            services.AddTransient<Command>();
            services.AddTransient<Writer>();
            services.AddTransient<IQueryBuilder, SQLKataQueryBuilder>();
            services.AddTransient<IQueryExecutor, DBCommandQueryExecutor>();
            services.AddTransient<IEntityAccessContext, EntityAccessContext>();
            services.AddSingleton<EdmPathFactory>();
            services.AddSingleton<SemanticVisitor>();
            services.AddSingleton<CompilerContextFactory>();
            services.AddSingleton<DeltaTokenService>();
            services.AddSingleton(p => new ODataService(p, options));
            return new PluginRegistration(services);
        }
    }

    public sealed class PluginRegistration
    {
        private readonly IServiceCollection services;

        internal PluginRegistration(IServiceCollection services)
        {
            this.services = services;
        }

        /// <summary>
        /// Register a plugin that intercept event of accessing entities.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void AddOnEntityAccess<T>() where T : class, IOnEntityAccess
        {
            services.AddTransient<IOnEntityAccess, T>();
        }
    }

    public enum SQLCompilerType
    {
        MSSQL,
        SQLLite
    }


    public class ODataOptions
    {
        public SQLCompilerType SQLCompilerType { get; set; }

        public string ServiceRoot { get; set; }

        /// <summary>
        /// Default command execution timeout
        /// </summary>
        public uint DefaultCommandTimeout { get; set; } =  30;

        /// <summary>
        /// Default $top value if not speficied in the request
        /// </summary>
        public uint DefaultTopRequest {get; set;} = 100;

        public ODataOptions()
        {
            ServiceRoot = "http://localhost/";
        }
    }

    public class ODataService
    {
        internal IServiceProvider serviceProvider;
        internal readonly ODataOptions options;

        private EdmModel? _model = null;

        internal ODataService(IServiceProvider serviceProvider, ODataOptions options) {
            this.serviceProvider = serviceProvider;
            //Model = model;
            this.options = options;
            ServiceRoot = new Uri(options.ServiceRoot);
        }

        public EdmModel CreateNewModel() 
        {
            _model = serviceProvider.GetService<EdmModel>()!;
            return _model;
        }

        public EdmModel Model { get => _model is null ? throw new ArgumentNullException("Model") : _model; }
        public Uri ServiceRoot { get; }

        public string GetMetadataDocument()
        {
            using (var sb = new StringWriter())
            {
                using (var writer = XmlWriter.Create(sb))
                {
                    IEnumerable<EdmError> errors;
                    CsdlWriter.TryWriteCsdl(Model, writer, CsdlTarget.OData, out errors);
                }
                return sb.ToString();
            }
        }

        public void GetServiceDocument(Stream _stream)
        {
            var writer = serviceProvider.GetService<Writer>()!;
            writer.WriteServiceDocument(_stream);
            
            
            // using (var sb = new StringWriter())
            // {
            //     using (var writer = XmlWriter.Create(sb))
            //     {
            //         IEnumerable<EdmError> errors;
            //         CsdlWriter.TryWriteCsdl(Model, writer, CsdlTarget.OData, out errors);
            //     }
            //     return sb.ToString();
            // }
        }

        public Connection OpenConnection(DbConnection SQLConnection)
        {
            var con = serviceProvider.GetService<Connection>()!;
            con.DBConnection = SQLConnection!;
            con.Open();
            return con;
        }

        public async Task<Connection> OpenConnectionAsync(DbConnection SQLConnection)
        {
            var con = serviceProvider.GetService<Connection>()!;
            con.DBConnection = SQLConnection;
            await con.OpenAsync();
            return con;
        }
    }

}