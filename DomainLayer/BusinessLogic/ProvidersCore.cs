using DomainLayer.Interfaces;
using InfraLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DomainLayer.BusinessLogic
{
    public class ProvidersCore : IProvidersCore
    {
        private readonly TekusProvidersContext _context;
        private readonly ILogger _logger;
        public ProvidersCore(TekusProvidersContext context, ILogger logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene la lista de proveedores desde la base de datos
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<List<Providers>> GetProvidersList()
        {
            try
            {
                List<Providers> providers = await _context.Providers.ToListAsync();
                return providers;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error obteniendo lista de proveedores");
                throw new InvalidOperationException(e.Message, e);
            }
        }

        /// <summary>
        /// Metodo para crear un nuevo proveedor en base de datos
        /// </summary>
        /// <param name="provider">Objeto proveedor a crear</param>
        /// <returns>Retorna "OK" si la operación fue exitosa</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<string> CreateNewProvider(Providers provider)
        {
            try
            {
                string response = string.Empty;
                
                //Se guardan los datos del proveedor en la tabla Providers y en la tabla CustomFields ya que ambas tablas tienen relación
                var entityEntryProvider = await _context.Providers.AddAsync(provider);
                
                int result = await _context.SaveChangesAsync();
                
                if(result > 0)
                {
                    response = "OK";
                    _logger.LogInformation($"Nuevo proveedor creado con éxito. ID: {entityEntryProvider.Entity.Id}, Campos personalizados: {provider.CustomFields.Count}");
                }
                else
                {
                    response = "Error al crear el nuevo proveedor";
                    _logger.LogWarning(response);
                }
                
                return response;
            } 
            catch(Exception e)
            {
                _logger.LogError(e, "Error al crear un nuevo proveedor");
                throw new InvalidOperationException(e.Message, e);
            }
        }

        /// <summary>
        /// Metodo para actualizar un proveedor existente en base de datos junto con sus campos personalizados
        /// </summary>
        /// <param name="provider">Objeto proveedor con los datos actualizados</param>
        /// <returns>Retorna "OK" si la operación fue exitosa</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<string> UpdateProvider(Providers provider)
        {
            try
            {
                string response = string.Empty;
                
                var existingProvider = await _context.Providers
                    .FirstOrDefaultAsync(p => p.Id == provider.Id);
                
                if (existingProvider == null)
                {
                    response = "Proveedor no encontrado";
                    _logger.LogWarning($"El proveedor con ID {provider.Id} no existe");
                    return response;
                }
                
                // Actualizar los datos básicos del proveedor
                existingProvider.Nit = provider.Nit;
                existingProvider.Name = provider.Name;
                existingProvider.Email = provider.Email;
                int result = await _context.SaveChangesAsync();
                
                if(result > 0)
                {
                    response = "OK";
                    _logger.LogInformation("Proveedor actualizado con éxito");
                }
                else
                {
                    response = $"No hubo cambios para el proveedor con ID: {provider.Id}";
                    _logger.LogInformation(response);
                }
                
                return response;
            }
            catch(Exception e)
            {
                _logger.LogError(e, $"Error al actualizar el proveedor con ID: {provider.Id}");
                throw new InvalidOperationException(e.Message, e);
            }
        }

        /// <summary>
        /// Elimina el proveedor junto con sus servicios y campos personalizados
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<string> DeleteProvider(int id)
        {
            try
            {
                var provider = await _context.Providers
                    .Include(p => p.CustomFields)
                    .Include(p => p.ProvidersServices)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (provider != null)
                {
                    List<Services> listServices = new();

                    foreach(ProvidersServices provService in provider.ProvidersServices)
                    {
                        //busca los servicios relacionados al proveedor
                        var service = _context.Services.Where(s => s.Id == provService.IdService);
                        listServices.Add((Services)service);
                    }

                    if (listServices.Count > 0)
                        _context.RemoveRange(listServices); //elimina los servicios

                    if (provider.CustomFields.Count > 0)
                        _context.RemoveRange(provider.CustomFields); //elimina los campos personalizados

                    _context.Providers.Remove(provider); //elimna el proveedor

                    int result = await _context.SaveChangesAsync();

                    return (result > 0) ? "OK" : "Ocurrió un error al elimninar el proveedor";
                } else
                {
                    return "No se encontró el proveedor";
                }

            } catch(Exception e)
            {
                _logger.LogError(e, $"Error al eliminar el proveedor con ID: {id}");
                throw new InvalidOperationException(e.Message, e);
            }
        }
        
    }
}
