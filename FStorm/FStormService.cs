﻿using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;

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
            services.AddTransient<Command, GetRequestCommand>();
            services.AddTransient<Writer>();
            services.AddSingleton<SemanticVisitor>();
            services.AddTransient<IQueryBuilder, DelegatedQueryBuilder>();
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

        public Connection OpenConnection(DbConnection SQLConnection)
        {
            var con = serviceProvider.GetService<Connection>()!;
            con.connection = SQLConnection!;
            con.Open();
            return con;
        }

        public async Task<Connection> OpenConnectionAsync(DbConnection SQLConnection)
        {
            var con = serviceProvider.GetService<Connection>()!;
            con.connection = SQLConnection;
            await con.OpenAsync();
            return con;
        }
    }

}