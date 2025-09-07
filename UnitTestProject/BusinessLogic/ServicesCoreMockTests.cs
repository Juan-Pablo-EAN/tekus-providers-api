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
    /// Pruebas unitarias para ServicesCore usando el DbContextMock
    /// </summary>
    public class ServicesCoreMockTests
    {
        private readonly Mock<ILogger> _mockLogger;

        public ServicesCoreMockTests()
        {
            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public async Task CreateNewService_ShouldAddServiceSuccessfully()
        {
            // Arrange
            var mockContext = DbContextMockFactory.CreateEmpty();
            var servicesCore = new ServicesCore(mockContext, _mockLogger.Object);

            var newService = new Services
            {
                Name = "Nuevo Servicio",
                ValuePerHourUsd = "85.00"
            };

            // Act
            var result = await servicesCore.CreateNewService(newService);

            // Assert
            Assert.Equal("OK", result);
            Assert.Equal(1, mockContext.GetRecordCount<Services>());

            var savedService = mockContext.Services.First();
            Assert.Equal("Nuevo Servicio", savedService.Name);
            Assert.Equal("85.00", savedService.ValuePerHourUsd);
        }

        [Fact]
        public async Task UpdateService_WithNonExistentService_ShouldReturnNotFound()
        {
            // Arrange
            var mockContext = DbContextMockFactory.CreateEmpty();
            var servicesCore = new ServicesCore(mockContext, _mockLogger.Object);

            var nonExistentService = new ServiceCompleteDto
            {
                Id = 999,
                Name = "Servicio Inexistente",
                ValuePerHourUsd = "100.00"
            };

            // Act
            var result = await servicesCore.UpdateService(nonExistentService);

            // Assert
            Assert.Equal("Servicio no encontrado", result);
        }

        [Fact]
        public async Task DeleteService_WithNonExistentService_ShouldReturnNotFound()
        {
            // Arrange
            var mockContext = DbContextMockFactory.CreateEmpty();
            var servicesCore = new ServicesCore(mockContext, _mockLogger.Object);

            // Act
            var result = await servicesCore.DeleteService(999);

            // Assert
            Assert.Equal("No se encontró el id del servicio", result);
        }

        [Fact]
        public async Task GetServicesByProviderName_WithNoMatches_ShouldReturnEmptyList()
        {
            // Arrange
            var mockContext = DbContextMockFactory.CreateEmpty();
            var servicesCore = new ServicesCore(mockContext, _mockLogger.Object);

            // Act
            var result = await servicesCore.GetServicesByProviderName("Inexistente");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetServicesByCountry_WithNonExistentCountry_ShouldReturnEmptyList()
        {
            // Arrange
            var mockContext = DbContextMockFactory.CreateEmpty();
            var servicesCore = new ServicesCore(mockContext, _mockLogger.Object);

            // Act
            var result = await servicesCore.GetServicesByCountry("XX");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void MockDbSet_AddRange_ShouldAddMultipleEntities()
        {
            // Arrange
            var mockContext = new DbContextMock();
            var services = new[]
            {
                new Services { Name = "Service 1", ValuePerHourUsd = "50.00" },
                new Services { Name = "Service 2", ValuePerHourUsd = "60.00" },
                new Services { Name = "Service 3", ValuePerHourUsd = "70.00" }
            };

            // Act
            mockContext.Services.AddRange(services);
            mockContext.SaveChanges();

            // Assert
            Assert.Equal(3, mockContext.GetRecordCount<Services>());
            
            var savedServices = mockContext.Services.ToList();
            Assert.All(savedServices, s => Assert.True(s.Id > 0)); // Verificar que se asignaron IDs
        }

        [Fact]
        public async Task MockDbSet_AddAsync_ShouldWorkCorrectly()
        {
            // Arrange
            var mockContext = new DbContextMock();
            var service = new Services { Name = "Async Service", ValuePerHourUsd = "80.00" };

            // Act
            await mockContext.Services.AddAsync(service);
            await mockContext.SaveChangesAsync();

            // Assert
            Assert.Equal(1, mockContext.GetRecordCount<Services>());
            Assert.True(service.Id > 0);
        }
    }
}