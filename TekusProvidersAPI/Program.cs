using DomainLayer.DTOs;
using InfraLayer.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CountriesServiceConfig>(builder.Configuration.GetSection("CountriesService"));

builder.Services.AddDbContextPool<TekusProvidersContext>(options =>
{
    string connectionString; // Se obtiene la cadena de conexión según el entorno

#if DEBUG
    connectionString = builder.Configuration.GetConnectionString("DataBase:ConnectionString") ??
                            builder.Configuration["DataBase:ConnectionString"] ??
                            throw new InvalidOperationException("Connection string 'DataBase:ConnectionString' not found.");
#else
         connectionString = Environment.GetEnvironmentVariable("DbConnection");
#endif


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

builder.Services.AddCors(options => options.AddPolicy("CorsPolicy", corsBuilder =>
{
    if (builder.Environment.IsDevelopment())
    {
        // En desarrollo
        corsBuilder.WithOrigins("http://localhost:4200", "https://localhost:4200")
                   .AllowCredentials();
    }
    else
    {
        // En producción
        corsBuilder.AllowAnyOrigin()
                   .AllowAnyHeader()
                   .AllowAnyMethod();    }
    
    // Configuración común para ambos entornos
    corsBuilder.WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
               .WithHeaders("Authorization", "Content-Type", "Accept", "Origin", "X-Requested-With");
}));

builder.Services.AddHttpClient();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tekus Providers API V1");
    c.RoutePrefix = "swagger";
});

app.UseCors("CorsPolicy");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
