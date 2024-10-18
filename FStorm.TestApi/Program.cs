using System.Data.Common;
using System.IO;
using FStorm;
using FStorm.Test;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Data.Sqlite;

//var odataService = new FStormService()

internal class Program
{

    

    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddFStorm(
            MockModel.PrepareModel(), 
            new FStormOptions() 
            {
                SQLCompilerType = SQLCompilerType.SQLLite, 
                ServiceRoot = "http://localhost:5056/odata/v1/"
            }
        );


        var app = builder.Build();

        // Configure the HTTP request pipeline.
        // if (app.Environment.IsDevelopment())
        // {
        //     app.UseSwagger();
        //     app.UseSwaggerUI();
        // }
        app.UseOdata("odata/v1", (s) => new SqliteConnection(app.Environment.IsDevelopment() ? "Data Source=.\\bin\\Debug\\net8.0\\MockData;" : ".\\MockData;"));

        // app.MapGet("odata/v1/{*resourse}", async (HttpContext context) =>
        // {
        //     var connectionString =  app.Environment.IsDevelopment() ? "Data Source=.\\bin\\Debug\\net8.0\\MockData;" : ".\\MockData;" ; 
        //     var connection = new SqliteConnection(connectionString);
        //     var _odata = context.RequestServices.GetService<FStormService>()!;
        //     using (var con = _odata.OpenConnection(connection))
        //     {
        //         using (var _stream = new MemoryStream())
        //         {
        //             await con.Get(new GetRequest() {RequestPath = context.Request.Path + context.Request.QueryString.Value}).ToODataResponse(_stream);
        //             return Results.Text(_stream.ToArray(),"application/json");
        //         }
        //     }
        // });

        app.Run();
    }
}


public static class Extensions
{
    public static void UseOdata(this IEndpointRouteBuilder app, string basePath, Func<IServiceProvider, DbConnection> connectionFactory)
    {

        app.MapGet(basePath, (HttpContext context) => {
            var _odata = context.RequestServices.GetService<FStormService>()!;
            using (var _stream = new MemoryStream())
            {   
                _odata.GetServiceDocument(_stream);
                context.Response.Headers.TryAdd("OData-Version","4.0");
                return Results.Text(_stream.ToArray(),"application/json");
            }
            //return Results.Text(_odata.GetMetadataDocument());
        });

        app.MapGet(basePath + "/$metadata", (HttpContext context) => {
            var _odata = context.RequestServices.GetService<FStormService>()!;
            return Results.Text(_odata.GetMetadataDocument(),"application/xml",System.Text.Encoding.Unicode);
        });

        app.MapGet(basePath + "/{*resourse}", async (HttpContext context) =>
        {
            var _odata = context.RequestServices.GetService<FStormService>()!;
            using (var con = _odata.OpenConnection(connectionFactory(context.RequestServices)))
            {
                using (var _stream = new MemoryStream())
                {
                    await con.Get(new GetRequest() {RequestPath = context.Request.Path + context.Request.QueryString.Value}).ToODataResponse(_stream);
                    return Results.Text(_stream.ToArray(),"application/json");
                }
            }
        });
    }
}