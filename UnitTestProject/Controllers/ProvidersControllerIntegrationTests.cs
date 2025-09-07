using Xunit;
using UnitTestProject.DataBaseMock;
using TekusProvidersAPI.Controllers;
using Microsoft.Extensions.Logging;
using DomainLayer.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;

namespace UnitTestProject.Controllers
{
    /// <summary>
    /// Pruebas de integración para ProvidersController usando el DbContextMock
    /// Demuestra cómo probar controladores con el contexto mock
    /// </summary>
    public class ProvidersControllerIntegrationTests
    {
        private readonly Mock<ILogger<ProvidersController>> _mockLogger;

        public ProvidersControllerIntegrationTests()
        {
            _mockLogger = new Mock<ILogger<ProvidersController>>();
        }

        [Fact]
        public async Task GetProviders_ShouldReturnOkWithProvidersList()
        {
            // Arrange
            var mockContext = DbContextMockFactory.CreateWithMultipleProviders();
            var controller = new ProvidersController(mockContext, _mockLogger.Object);

            // Act
            var result = await controller.GetProviders();

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<InfraLayer.Models.Providers>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var providers = Assert.IsType<List<InfraLayer.Models.Providers>>(okResult.Value);
            
            Assert.Equal(3, providers.Count);
            Assert.Contains(providers, p => p.Name == "Proveedor A");
        }

        [Fact]
        public async Task CreateNewProvider_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var mockContext = DbContextMockFactory.CreateEmpty();
            var controller = new ProvidersController(mockContext, _mockLogger.Object);

            var providerData = new CompleteProviderDto
            {
                Name = "Nuevo Proveedor API",
                Nit = "777777777",
                Email = "api@nuevo.com",
                CustomFields = new List<CustomFieldCompleteDto>
                {
                    new CustomFieldCompleteDto { FieldName = "Sector", FieldValue = "Tecnología" }
                }
            };

            var requestModel = new RequestModel
            {
                ObjectRequest = JsonConvert.SerializeObject(providerData)
            };

            // Act
            var result = await controller.CreateNewProvider(requestModel);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Status); // Usar Status en lugar de Success
            Assert.Contains("OK", result.Message);

            // Verificar que se guardó en la base de datos mock
            Assert.Equal(1, mockContext.GetRecordCount<InfraLayer.Models.Providers>());
            Assert.Equal(1, mockContext.GetRecordCount<InfraLayer.Models.CustomFields>());
        }

        [Fact]
        public async Task DeleteProvider_WithInvalidId_ShouldReturnError()
        {
            // Arrange
            var mockContext = DbContextMockFactory.CreateEmpty();
            var controller = new ProvidersController(mockContext, _mockLogger.Object);

            // Act
            var result = await controller.DeleteProvider(0); // ID inválido

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Status); // Usar Status en lugar de Success
            Assert.Contains("válido", result.Message);
        }

        [Fact]
        public async Task GetCompleteProviders_ShouldReturnCompleteProviderData()
        {
            // Arrange
            var mockContext = DbContextMockFactory.CreateWithTestData();
            var controller = new ProvidersController(mockContext, _mockLogger.Object);

            // Act
            var result = await controller.GetCompleteProviders();

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<CompleteProviderDto>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var providers = Assert.IsType<List<CompleteProviderDto>>(okResult.Value);

            Assert.NotEmpty(providers);
            var provider = providers.First();
            Assert.Equal("Proveedor Test", provider.Name);
            Assert.NotEmpty(provider.CustomFields);
        }
    }

    /// <summary>
    /// Pruebas adicionales para demostrar diferentes escenarios con el DbContextMock
    /// </summary>
    public class DbContextMockAdvancedTests
    {
        [Fact]
        public void DbContextMock_TransactionSupport_ShouldWorkCorrectly()
        {
            // Arrange
            var mockContext = new DbContextMock();

            // Act & Assert - Probar transacciones
            using (var transaction = mockContext.BeginMockTransaction())
            {
                mockContext.Providers.Add(new InfraLayer.Models.Providers 
                { 
                    Name = "Test Provider", 
                    Nit = "123", 
                    Email = "test@test.com" 
                });
                
                Assert.False(transaction.IsCommitted);
                Assert.False(transaction.IsRolledBack);

                transaction.Commit();
                
                Assert.True(transaction.IsCommitted);
                Assert.False(transaction.IsRolledBack);
            }
        }

        [Fact]
        public void DbContextMock_SaveChangesCounter_ShouldTrackCalls()
        {
            // Arrange
            var mockContext = new DbContextMock();

            // Act
            Assert.Equal(0, mockContext.GetSaveChangesCallCount());

            mockContext.Providers.Add(new InfraLayer.Models.Providers 
            { 
                Name = "Test", 
                Nit = "123", 
                Email = "test@test.com" 
            });
            mockContext.SaveChanges();

            mockContext.Services.Add(new InfraLayer.Models.Services 
            { 
                Name = "Test Service", 
                ValuePerHourUsd = "50.00" 
            });
            mockContext.SaveChanges();

            // Assert
            Assert.Equal(2, mockContext.GetSaveChangesCallCount());

            // Reset counter
            mockContext.ResetSaveChangesCallCount();
            Assert.Equal(0, mockContext.GetSaveChangesCallCount());
        }

        [Fact]
        public void DbContextMock_ComplexQueries_ShouldWorkWithLinq()
        {
            // Arrange
            var mockContext = DbContextMockFactory.CreateWithTestData();

            // Act - Consultas LINQ complejas
            var providersWithCustomFields = mockContext.Providers
                .Where(p => p.Name.Contains("Test"))
                .ToList();

            var servicesWithHighValue = mockContext.Services
                .Where(s => decimal.Parse(s.ValuePerHourUsd) > 40)
                .OrderBy(s => s.Name)
                .ToList();

            // Assert
            Assert.NotEmpty(providersWithCustomFields);
            Assert.NotEmpty(servicesWithHighValue);
        }

    }
}