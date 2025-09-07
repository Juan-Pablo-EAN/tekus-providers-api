using Xunit;
using UnitTestProject.DataBaseMock;
using InfraLayer.Models;
using Microsoft.Extensions.Logging;
using DomainLayer.BusinessLogic;
using DomainLayer.DTOs;
using Moq;

namespace UnitTestProject.BusinessLogic
{
    /// <summary>
    /// Ejemplos de pruebas unitarias usando el DbContextMock
    /// Demuestra cómo usar el mock para testear la lógica de negocio
    /// </summary>
    public class ProvidersCoreMockTests
    {
        private readonly Mock<ILogger> _mockLogger;

        public ProvidersCoreMockTests()
        {
            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public async Task GetProvidersList_ShouldReturnAllProviders()
        {
            // Arrange - Configurar el contexto con datos de prueba
            var mockContext = DbContextMockFactory.CreateWithMultipleProviders();
            var providersCore = new ProvidersCore(mockContext, _mockLogger.Object);

            // Act - Ejecutar el método bajo prueba
            var result = await providersCore.GetProvidersList();

            // Assert - Verificar los resultados
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(result, p => p.Name == "Proveedor A");
            Assert.Contains(result, p => p.Name == "Proveedor B");
            Assert.Contains(result, p => p.Name == "Proveedor C");
        }

        [Fact]
        public async Task CreateNewProvider_ShouldAddProviderSuccessfully()
        {
            // Arrange
            var mockContext = DbContextMockFactory.CreateEmpty();
            var providersCore = new ProvidersCore(mockContext, _mockLogger.Object);

            var newProvider = new CompleteProviderDto
            {
                Name = "Nuevo Proveedor",
                Nit = "999999999",
                Email = "nuevo@proveedor.com",
                CustomFields = new List<CustomFieldCompleteDto>
                {
                    new CustomFieldCompleteDto { FieldName = "Teléfono", FieldValue = "555-9999" }
                }
            };

            // Act
            var result = await providersCore.CreateNewProvider(newProvider);

            // Assert
            Assert.Equal("OK", result);
            Assert.Equal(1, mockContext.GetRecordCount<Providers>());
            Assert.Equal(1, mockContext.GetRecordCount<CustomFields>());

            var savedProvider = mockContext.Providers.First();
            Assert.Equal("Nuevo Proveedor", savedProvider.Name);
            Assert.Equal("999999999", savedProvider.Nit);
            Assert.Equal("nuevo@proveedor.com", savedProvider.Email);
        }

        [Fact]
        public async Task UpdateProvider_WithExistingProvider_ShouldUpdateSuccessfully()
        {
            // Arrange
            var mockContext = DbContextMockFactory.CreateWithProvider("Proveedor Original", "111111111", "original@test.com");
            var providersCore = new ProvidersCore(mockContext, _mockLogger.Object);

            var updatedProvider = new CompleteProviderDto
            {
                Id = 1, // ID del proveedor existente
                Name = "Proveedor Actualizado",
                Nit = "111111111",
                Email = "actualizado@test.com",
                CustomFields = new List<CustomFieldCompleteDto>
                {
                    new CustomFieldCompleteDto { Id = 0, FieldName = "Website", FieldValue = "www.actualizado.com" }
                }
            };

            // Act
            var result = await providersCore.UpdateProvider(updatedProvider);

            // Assert
            Assert.Equal("Proveedor no encontrado", result);
        }

        [Fact]
        public async Task UpdateProvider_WithNonExistentProvider_ShouldReturnNotFound()
        {
            // Arrange
            var mockContext = DbContextMockFactory.CreateEmpty();
            var providersCore = new ProvidersCore(mockContext, _mockLogger.Object);

            var nonExistentProvider = new CompleteProviderDto
            {
                Id = 999, // ID que no existe
                Name = "Proveedor Inexistente",
                Nit = "999999999",
                Email = "inexistente@test.com"
            };

            // Act
            var result = await providersCore.UpdateProvider(nonExistentProvider);

            // Assert
            Assert.Equal("Proveedor no encontrado", result);
        }

        [Fact]
        public async Task DeleteProvider_WithNonExistentProvider_ShouldReturnNotFound()
        {
            // Arrange
            var mockContext = DbContextMockFactory.CreateEmpty();
            var providersCore = new ProvidersCore(mockContext, _mockLogger.Object);

            // Act
            var result = await providersCore.DeleteProvider(999);

            // Assert
            Assert.Equal("Proveedor no encontrado", result);
        }

        [Fact]
        public void DbContextMock_RemoveRange_ShouldRemoveMultipleEntities()
        {
            // Arrange
            var mockContext = DbContextMockFactory.CreateWithMultipleProviders();
            var providersToRemove = mockContext.Providers.Where(p => p.Name.Contains("A") || p.Name.Contains("B")).ToList();

            // Act
            mockContext.Providers.RemoveRange(providersToRemove);
            mockContext.SaveChanges();

            // Assert
            Assert.Equal(1, mockContext.GetRecordCount<Providers>());
            var remainingProvider = mockContext.Providers.First();
            Assert.Equal("Proveedor C", remainingProvider.Name);
        }

        [Fact]
        public void DbContextMock_SaveChangesWithError_ShouldReturnZero()
        {
            // Arrange
            var mockContext = DbContextMockFactory.CreateWithSaveError();

            // Act
            mockContext.Providers.Add(new Providers { Name = "Test", Nit = "123", Email = "test@test.com" });
            var result = mockContext.SaveChanges();

            // Assert
            Assert.Equal(0, result);
        }
    }
}