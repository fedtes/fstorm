using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Validation;
using System.Data.Common;
using System.Xml;

namespace FStorm
{
    public static class ServiceCollectionExtensions
    {
        public static void AddFStorm(this IServiceCollection services, EdmModel model, FStormOptions options)
        {
            services.AddSingleton(p => new FStormService(p, model, options));
            services.AddSingleton<EdmPathFactory>();
            services.AddTransient<Connection>();
            services.AddTransient<Transaction>();
            services.AddTransient<Command>();
            services.AddTransient<Writer>();
            services.AddSingleton<SemanticVisitor>();
            services.AddTransient<IQueryBuilder, SQLKataQueryBuilder>();
            services.AddTransient<IQueryExecutor, DBCommandQueryExecutor>();
            services.AddSingleton<CompilerContextFactory>();
        }
    }

    public enum SQLCompilerType
    {
        MSSQL,
        SQLLite
    }


    public class FStormOptions
    {
        public SQLCompilerType SQLCompilerType { get; set; }

        public string ServiceRoot { get; set; }

        public FStormOptions()
        {
            ServiceRoot = "http://localhost/";
        }
    }

    public class FStormService
    {
        internal IServiceProvider serviceProvider;
        internal readonly FStormOptions options;

        public FStormService(IServiceProvider serviceProvider, EdmModel model, FStormOptions options) {
            this.serviceProvider = serviceProvider;
            Model = model;
            this.options = options;
            ServiceRoot = new Uri(options.ServiceRoot);
        }

        public EdmModel Model { get; }
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