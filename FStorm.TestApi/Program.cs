using System.Data.Common;
using System.IO;
using FStorm;
using FStorm.Test;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

//var odataService = new FStormService()

internal class Program
{

    

    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var edm = new EdmModel();
        var _gamba =  edm.AddEntityType("my","Gamba","Gambe");
        var _fkIDSedia = _gamba.AddStructuralProperty("IDSedia", Microsoft.OData.Edm.EdmPrimitiveTypeKind.Int32, false);
        _gamba.AddStructuralProperty("Rotta", Microsoft.OData.Edm.EdmPrimitiveTypeKind.Int32, false);
        var _idGamba = _gamba.AddStructuralProperty("IDGamba", Microsoft.OData.Edm.EdmPrimitiveTypeKind.Int32, false);
        _gamba.AddKey(_idGamba);

        var _sedia =  edm.AddEntityType("my","Sedia", "Sedie");
        var _idSedia = _sedia.AddStructuralProperty("IDSedia", Microsoft.OData.Edm.EdmPrimitiveTypeKind.Int32, false);
        _sedia.AddStructuralProperty("Proprietario", Microsoft.OData.Edm.EdmPrimitiveTypeKind.String, true);
        _sedia.AddKey(_idSedia);

        _sedia.AddNavigationProperty("Gambe", _gamba, Microsoft.OData.Edm.EdmMultiplicity.Many, _idSedia, _fkIDSedia);
        _gamba.AddNavigationProperty("Sedia", _sedia, Microsoft.OData.Edm.EdmMultiplicity.One, _fkIDSedia, _idSedia);
        _gamba.AddNavigationProperty("Sedia1", _sedia, Microsoft.OData.Edm.EdmMultiplicity.One, _fkIDSedia, _idSedia);

        var cnt = edm.AddEntityContainer("my","default");
        cnt.AddEntitySet("Sedie", _sedia);
        cnt.AddEntitySet("Gambe", _gamba);


     
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddFStorm(
            //MockModel.PrepareModel(), 
            edm,
            new FStormOptions() 
            {
                SQLCompilerType = SQLCompilerType.MSSQL, 
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
        //app.UseOdata("odata/v1", (s) => new SqliteConnection(app.Environment.IsDevelopment() ? "Data Source=.\\bin\\Debug\\net8.0\\MockData;" : ".\\MockData;"));
        app.UseOdata("odata/v1", (s) => new SqlConnection("Server=10.10.0.9\\SQL2016;Database=test;user id=sa;Password=P4ssw0rd;Trusted_Connection=False;TrustServerCertificate=True"));

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