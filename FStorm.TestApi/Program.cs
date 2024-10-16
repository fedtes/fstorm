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
        builder.Services.AddFStorm(MockModel.PrepareModel(), new FStormOptions() {SQLCompilerType = SQLCompilerType.SQLLite, ServiceRoot = "http://localhost:5056/"});


        var app = builder.Build();

        // Configure the HTTP request pipeline.
        // if (app.Environment.IsDevelopment())
        // {
        //     app.UseSwagger();
        //     app.UseSwaggerUI();
        // }


        app.MapGet("{res}", async (HttpContext context) =>
        {
            var connection = new SqliteConnection("Data Source=.\\MockData;");
            var _odata = context.RequestServices.GetService<FStormService>()!;
            using (var con = _odata.OpenConnection(connection))
            {
                 await con.Get(new GetRequest() {RequestPath = context.Request.Path}).ToODataResponse(context.Response.Body);
            }
        })
        .WithName("GetWeatherForecast")
        .WithOpenApi();

        app.Run();
    }
}