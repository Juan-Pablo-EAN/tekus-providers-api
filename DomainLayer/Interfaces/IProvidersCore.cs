using DomainLayer.DTOs;
using InfraLayer.Models;

namespace DomainLayer.Interfaces
{
    public interface IProvidersCore
    {
        /// <summary>
        /// Obtiene la lista de proveedores desde la base de datos
        /// </summary>
        /// <returns>Lista de proveedores</returns>
        public Task<List<Providers>> GetProvidersList();
        public Task<List<CompleteProviderDto>> GetCompleteProvidersListAsync();
        /// <summary>
        /// Crea un nuevo proveedor en la base de datos
        /// </summary>
        /// <param name="provider">Objeto proveedor a crear</param>
        /// <returns>Retorna "OK" si la operación fue exitosa</returns>
        public Task<string> CreateNewProvider(CompleteProviderDto provider);

        /// <summary>
        /// Actualiza un proveedor existente en la base de datos junto con sus campos personalizados
        /// </summary>
        /// <param name="provider">Objeto proveedor con los datos actualizados</param>
        /// <returns>Retorna "OK" si la operación fue exitosa</returns>
        public Task<string> UpdateProvider(Providers provider);

        /// <summary>
        /// Elimina el proveedor junto con sus servicios y campos personalizados
        /// </summary>
        /// <param name="id">Id del proveedor</param>
        /// <returns></returns>
        public Task<string> DeleteProvider(int id);

    }
}
