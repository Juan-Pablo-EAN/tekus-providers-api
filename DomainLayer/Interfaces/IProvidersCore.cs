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
        /// <summary>
        /// Obtiene la lista completa de proveedores con toda la información relacionada
        /// </summary>
        /// <returns>Lista completa de proveedores con detalles</returns>
        public Task<List<CompleteProviderDto>> GetCompleteProvidersListAsync();
        /// <summary>
        /// Crea un nuevo proveedor completo en la base de datos
        /// </summary>
        /// <param name="provider">Objeto proveedor completo a crear</param>
        /// <returns>Retorna "OK" si la operación fue exitosa</returns>
        public Task<string> CreateNewProvider(CompleteProviderDto provider);

        /// <summary>
        /// Actualiza un proveedor existente en la base de datos junto con sus campos personalizados
        /// </summary>
        /// <param name="provider">Objeto proveedor completo con los datos actualizados</param>
        /// <returns>Retorna "OK" si la operación fue exitosa</returns>
        public Task<string> UpdateProvider(CompleteProviderDto provider);

        /// <summary>
        /// Elimina un proveedor de la base de datos junto con sus relaciones
        /// </summary>
        /// <param name="id">ID del proveedor a eliminar</param>
        /// <returns>Retorna "OK" si la operación fue exitosa</returns>
        public Task<string> DeleteProvider(int id);

    }
}
