using DomainLayer.DTOs;
using InfraLayer.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CountriesServiceConfig>(builder.Configuration.GetSection("CountriesService"));

builder.Services.AddDbContextPool<TekusProvidersContext>(options =>
{
    string connectionString = builder.Configuration.GetConnectionString("DataBase:ConnectionString") ??
                             builder.Configuration["DataBase:ConnectionString"] ??
                             throw new InvalidOperationException("Connection string 'DataBase:ConnectionString' not found.");
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(60); // Tiempo máximo de espera para comandos SQL (segundos).
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3, // Número máximo de reintentos en caso de fallo.
            maxRetryDelay: TimeSpan.FromSeconds(10), // Tiempo máximo entre reintentos.
            errorNumbersToAdd: null
        );
    });
});

builder.Services.AddCors(options => options.AddPolicy("CorsPolicy", builder =>
{
    builder.WithOrigins("http://localhost:4200")
           .WithMethods("GET", "POST", "PUT", "DELETE")
           .WithHeaders("Authorization", "Content-Type")
           .AllowCredentials();
}));

builder.Services.AddHttpClient();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("CorsPolicy");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
