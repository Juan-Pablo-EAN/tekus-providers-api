using Microsoft.EntityFrameworkCore;
using InfraLayer.Models;
using UnitTestProject.DataBaseMock;

namespace UnitTestProject.DataBaseMock
{
    /// <summary>
    /// Extensiones para el DbContextMock que facilitan la creación de datos de prueba
    /// </summary>
    public static class DbContextMockExtensions
    {
        /// <summary>
        /// Crea datos de prueba básicos para testing
        /// </summary>
        public static DbContextMock SeedTestData(this DbContextMock context)
        {
            // Limpiar datos existentes
            context.ClearAllData();

            // Crear países de prueba
            var colombia = new Countries
            {
                Isocode = "CO",
                Name = "Colombia",
                FlagImage = "https://flagcdn.com/co.png"
            };

            var usa = new Countries
            {
                Isocode = "US",
                Name = "Estados Unidos",
                FlagImage = "https://flagcdn.com/us.png"
            };

            context.Countries.AddRange(colombia, usa);

            // Crear proveedor de prueba
            var provider = new Providers
            {
                Nit = "123456789",
                Name = "Proveedor Test",
                Email = "test@provider.com"
            };

            context.Providers.Add(provider);

            // Crear servicio de prueba
            var service = new Services
            {
                Name = "Desarrollo Web",
                ValuePerHourUsd = "50.00"
            };

            context.Services.Add(service);

            context.SaveChanges();

            // Obtener IDs generados
            var savedProvider = context.Providers.First(p => p.Name == "Proveedor Test");
            var savedService = context.Services.First(s => s.Name == "Desarrollo Web");
            var savedColombia = context.Countries.First(c => c.Isocode == "CO");

            // Crear relaciones
            var providerService = new ProvidersServices
            {
                IdProvider = savedProvider.Id,
                IdService = savedService.Id
            };

            context.ProvidersServices.Add(providerService);

            var serviceCountry = new ServicesCountries
            {
                IdService = savedService.Id,
                IdCountry = savedColombia.Id
            };

            context.ServicesCountries.Add(serviceCountry);

            // Crear campo personalizado
            var customField = new CustomFields
            {
                IdProvider = savedProvider.Id,
                FieldName = "Teléfono",
                FieldValue = "555-1234"
            };

            context.CustomFields.Add(customField);

            context.SaveChanges();
            context.ResetSaveChangesCallCount();

            return context;
        }

        /// <summary>
        /// Crea un proveedor completo con relaciones para testing
        /// </summary>
        public static Providers CreateCompleteTestProvider(this DbContextMock context, 
            string name = "Test Provider", 
            string nit = "987654321", 
            string email = "test@example.com")
        {
            var provider = new Providers
            {
                Name = name,
                Nit = nit,
                Email = email
            };

            context.Providers.Add(provider);
            context.SaveChanges();

            var savedProvider = context.Providers.First(p => p.Name == name);

            // Agregar campos personalizados
            var customFields = new[]
            {
                new CustomFields { IdProvider = savedProvider.Id, FieldName = "Teléfono", FieldValue = "555-0000" },
                new CustomFields { IdProvider = savedProvider.Id, FieldName = "Dirección", FieldValue = "Calle Test 123" }
            };

            context.CustomFields.AddRange(customFields);
            context.SaveChanges();

            return savedProvider;
        }

        /// <summary>
        /// Crea un servicio de prueba con países asociados
        /// </summary>
        public static Services CreateTestService(this DbContextMock context, 
            string name = "Test Service", 
            string valuePerHour = "75.00")
        {
            var service = new Services
            {
                Name = name,
                ValuePerHourUsd = valuePerHour
            };

            context.Services.Add(service);
            context.SaveChanges();

            return context.Services.First(s => s.Name == name);
        }

        /// <summary>
        /// Verifica si una entidad existe en el contexto
        /// </summary>
        public static bool EntityExists<T>(this DbContextMock context, int id) where T : class
        {
            return context.Set<T>().Find(id) != null;
        }

        /// <summary>
        /// Obtiene la cantidad de registros en una tabla específica
        /// </summary>
        public static int GetRecordCount<T>(this DbContextMock context) where T : class
        {
            return context.Set<T>().Count();
        }
    }

    /// <summary>
    /// Factory para crear instancias de DbContextMock configuradas para diferentes escenarios de testing
    /// </summary>
    public static class DbContextMockFactory
    {
        /// <summary>
        /// Crea un contexto vacío para pruebas
        /// </summary>
        public static DbContextMock CreateEmpty()
        {
            return new DbContextMock();
        }

        /// <summary>
        /// Crea un contexto con datos de prueba básicos
        /// </summary>
        public static DbContextMock CreateWithTestData()
        {
            var context = new DbContextMock();
            return context.SeedTestData();
        }

        /// <summary>
        /// Crea un contexto que simula errores en SaveChanges
        /// </summary>
        public static DbContextMock CreateWithSaveError()
        {
            var context = new DbContextMock();
            context.SetSaveChangesResult(false);
            return context;
        }

        /// <summary>
        /// Crea un contexto con un proveedor específico y sus relaciones
        /// </summary>
        public static DbContextMock CreateWithProvider(string providerName, string nit, string email)
        {
            var context = new DbContextMock();
            
            // Crear país
            var country = new Countries
            {
                Isocode = "CO",
                Name = "Colombia", 
                FlagImage = "https://flagcdn.com/co.png"
            };
            context.Countries.Add(country);

            // Crear proveedor
            var provider = new Providers
            {
                Name = providerName,
                Nit = nit,
                Email = email
            };
            context.Providers.Add(provider);

            context.SaveChanges();
            context.ResetSaveChangesCallCount();
            return context;
        }

        /// <summary>
        /// Crea un contexto con múltiples proveedores para pruebas de listado
        /// </summary>
        public static DbContextMock CreateWithMultipleProviders()
        {
            var context = new DbContextMock();

            var providers = new[]
            {
                new Providers { Name = "Proveedor A", Nit = "111111111", Email = "a@test.com" },
                new Providers { Name = "Proveedor B", Nit = "222222222", Email = "b@test.com" },
                new Providers { Name = "Proveedor C", Nit = "333333333", Email = "c@test.com" }
            };

            context.Providers.AddRange(providers);
            context.SaveChanges();
            context.ResetSaveChangesCallCount();

            return context;
        }
    }
}