using InfraLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace UnitTestProject.DataBaseMock
{

    public class DbContextMock : TekusProvidersContext
    {
        private int _saveChangesCallCount = 0;
        private bool _saveChangesResult = true;

        public DbContextMock() : base(CreateInMemoryOptions())
        {
        }

        /// <summary>
        /// Crea las opciones para usar In-Memory Database
        /// </summary>
        private static DbContextOptions<TekusProvidersContext> CreateInMemoryOptions()
        {
            return new DbContextOptionsBuilder<TekusProvidersContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) 
                .Options;
        }

        /// <summary>
        /// Override de SaveChanges para agregar funcionalidades de testing
        /// </summary>
        public override int SaveChanges()
        {
            _saveChangesCallCount++;
            
            if (!_saveChangesResult)
                return 0;

            return base.SaveChanges();
        }

        /// <summary>
        /// Versión asíncrona de SaveChanges
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            _saveChangesCallCount++;
            
            if (!_saveChangesResult)
                return 0;

            return await base.SaveChangesAsync(cancellationToken);
        }

        // Métodos de utilidad para testing
        public void SetSaveChangesResult(bool result) => _saveChangesResult = result;
        public int GetSaveChangesCallCount() => _saveChangesCallCount;
        public void ResetSaveChangesCallCount() => _saveChangesCallCount = 0;

        /// <summary>
        /// Limpia todos los datos de las tablas
        /// </summary>
        public void ClearAllData()
        {
            Countries.RemoveRange(Countries);
            CustomFields.RemoveRange(CustomFields);
            ServicesCountries.RemoveRange(ServicesCountries);
            ProvidersServices.RemoveRange(ProvidersServices);
            Services.RemoveRange(Services);
            Providers.RemoveRange(Providers);

            SaveChanges();
            ResetSaveChangesCallCount();
        }

        /// <summary>
        /// Simula transacciones de base de datos
        /// </summary>
        public MockTransaction BeginMockTransaction()
        {
            return new MockTransaction();
        }

        /// <summary>
        /// Obtiene la cantidad de registros en una tabla específica
        /// </summary>
        public int GetRecordCount<T>() where T : class
        {
            return Set<T>().Count();
        }

        /// <summary>
        /// Verifica si una entidad existe por ID
        /// </summary>
        public bool EntityExists<T>(int id) where T : class
        {
            return Set<T>().Find(id) != null;
        }
    }

    /// <summary>
    /// Mock de transacción para simular transacciones de base de datos
    /// </summary>
    public class MockTransaction : IDisposable
    {
        public bool IsCommitted { get; private set; }
        public bool IsRolledBack { get; private set; }

        public void Commit()
        {
            IsCommitted = true;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Commit();
            return Task.CompletedTask;
        }

        public void Rollback()
        {
            IsRolledBack = true;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            Rollback();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            // Cleanup si es necesario
        }
    }
}
