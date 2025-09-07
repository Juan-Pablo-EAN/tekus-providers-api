using DomainLayer.DTOs;
using DomainLayer.Interfaces;
using InfraLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DomainLayer.BusinessLogic
{
    public class ServicesCore : IServicesCore
    {
        private readonly TekusProvidersContext _context;
        private readonly ILogger _logger;
        public ServicesCore(TekusProvidersContext context, ILogger logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Almacena en base de datos un nuevo servicio, junto con su proveedor y paises asociados
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<string> CreateNewService(Services services)
        {
            try
            {
                string response = string.Empty;

                //Se guarda en base de datos el nuevo servicio, junto con su proveedor y paises asociados
                await _context.Services.AddAsync(services);

                int result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    response = "OK";
                    _logger.LogInformation($"Se ha creado el servicio exitosamente");
                }
                else
                {
                    response = "Error al crear el nuevo servicio";
                    _logger.LogWarning(response);
                }

                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error al crear un nuevo servicio");
                throw new InvalidOperationException(e.Message, e);
            }
        }

        /// <summary>
        /// Actualiza la información de un servicio junto con sus países relacionados
        /// Incluye lógica completa para actualizar servicio y gestionar países asociados
        /// </summary>
        /// <param name="service">Objeto servicio completo con los datos actualizados</param>
        /// <returns>Retorna "OK" si la operación fue exitosa</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<string> UpdateService(ServiceCompleteDto service)
        {
            try
            {                
                try
                {
                    var existingService = await _context.Services
                        .Include(s => s.ServicesCountries)
                            .ThenInclude(sc => sc.IdCountryNavigation)
                        .FirstOrDefaultAsync(s => s.Id == service.Id);

                    if (existingService == null)
                    {
                        string notFoundMsg = "Servicio no encontrado";
                        _logger.LogWarning($"El servicio con ID {service.Id} no existe");
                        return notFoundMsg;
                    }

                    bool serviceChanged = false;
                    if (existingService.Name != service.Name)
                    {
                        existingService.Name = service.Name;
                        serviceChanged = true;
                    }
                    if (existingService.ValuePerHourUsd != service.ValuePerHourUsd)
                    {
                        existingService.ValuePerHourUsd = service.ValuePerHourUsd;
                        serviceChanged = true;
                    }

                    // Actualiza países relacionados del servicio
                    await UpdateServiceCountries(existingService, service.Countries ?? new List<CountryCompleteDto>());

                    // Guarda todos los cambios
                    int result = await _context.SaveChangesAsync();

                    if (result > 0 || serviceChanged)
                    {
                        _logger.LogInformation($"Servicio actualizado exitosamente: ID={service.Id}, " +
                                             $"Countries procesados={service.Countries?.Count ?? 0}");
                        return "OK";
                    }
                    else
                    {
                        string noChangesMsg = $"No hubo cambios para el servicio con ID: {service.Id}";
                        _logger.LogInformation(noChangesMsg);
                        return noChangesMsg;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en transacción de actualización, realizando rollback");
                    throw;
                }
            }
            catch (Exception e)
            {
                string response = $"Ocurrió un error al actualizar servicio: {e.Message}";
                _logger.LogError(e, response);
                throw new InvalidOperationException(e.Message, e);
            }
        }

        /// <summary>
        /// Actualiza los países relacionados a un servicio de manera inteligente
        /// Maneja: actualización de países existentes, eliminación de faltantes y creación de nuevos países y relaciones
        /// </summary>
        /// <param name="existingService">Servicio existente con sus ServicesCountries cargados</param>
        /// <param name="newCountries">Lista nueva de países desde el cliente</param>
        private async Task UpdateServiceCountries(Services existingService, List<CountryCompleteDto> newCountries)
        {
            var existingServiceCountries = existingService.ServicesCountries.ToList();
            
            // Eliminar relaciones ServicesCountries que ya no están en la nueva lista
            var relationsToRemove = existingServiceCountries
                .Where(existing => !newCountries.Any(newCountry => 
                    newCountry.Id > 0 && newCountry.Id == existing.IdCountry))
                .ToList();

            if (relationsToRemove.Any())
            {
                _context.ServicesCountries.RemoveRange(relationsToRemove);
                _logger.LogDebug($"Relaciones a eliminar: {relationsToRemove.Count}");
            }

            foreach (var newCountry in newCountries)
            {
                int countryId = newCountry.Id;

                var existingRelation = existingServiceCountries
                    .FirstOrDefault(sc => sc.IdCountry == countryId);

                if (existingRelation == null)
                {
                    var newServiceCountry = new ServicesCountries
                    {
                        IdService = existingService.Id,
                        IdCountry = countryId
                    };

                    await _context.ServicesCountries.AddAsync(newServiceCountry);
                    _logger.LogDebug($"Nueva relación servicio-país creada: ServiceID={existingService.Id}, CountryID={countryId}");
                }
            }
            
            _logger.LogDebug($"Procesamiento de ServiceCountries completado");
        }

        /// <summary>
        /// Crea un nuevo país en la base de datos
        /// </summary>
        /// <param name="countryDto">Datos del país a crear</param>
        /// <returns>ID del país creado</returns>
        private async Task<int> CreateNewCountry(CountryCompleteDto countryDto)
        {
            var newCountry = new Countries
            {
                Isocode = countryDto.Isocode,
                Name = countryDto.Name,
                FlagImage = countryDto.FlagImage
            };

            await _context.Countries.AddAsync(newCountry);
            await _context.SaveChangesAsync(); // Guardar para obtener el ID

            _logger.LogDebug($"Nuevo país creado: {countryDto.Name} ({countryDto.Isocode}) con ID: {newCountry.Id}");
            return newCountry.Id;
        }

        /// <summary>
        /// Elimina un servicio de la tabla Services y de la tabla ServicesCountries
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<string> DeleteService(int id)
        {
            try
            {
                string response = string.Empty;
                var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == id);
                var countryServices = await _context.ServicesCountries.Where(sc => sc.IdService == id).ToListAsync();
                
                if(service != null)
                {
                    _context.Services.Remove(service);
                    _context.ServicesCountries.RemoveRange(countryServices);
                    int result = await _context.SaveChangesAsync();
                    response = (result > 0) ? "OK" : "Error al eliminar servicio";

                } else
                {
                    response = "No se encontró el id del servicio";
                    _logger.LogError(response);
                }

                return response;

            } catch(Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new InvalidOperationException(e.Message);
            }
        }

        /// <summary>
        /// Obtiene la lista de servicios por nombre del proveedor
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<List<ServicesByProviderModel>> GetServicesByProviderName(string name)
        {
            try
            {
                var services = await (from a in _context.Providers
                                join b in _context.ProvidersServices on a.Id equals b.IdProvider
                                join c in _context.Services on b.IdService equals c.Id
                                where a.Name.Contains(name)
                                select new ServicesByProviderModel() { NameProvider = a.Name, NitProvider = a.Nit, NameService = c.Name}).ToListAsync();

                return services;

            } catch(Exception e)
            {
                throw new InvalidOperationException(e.Message, e);
            }
        }

        /// <summary>
        /// Obtener todos los servicios y países relacionados para el código ISO especificado
        /// </summary>
        /// <param name="isocode"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<List<ServicesByCountry>> GetServicesByCountry(string isocode)
        {
            try
            {
                var servicesData = await (from a in _context.Countries
                                          join b in _context.ServicesCountries on a.Id equals b.IdCountry
                                          join c in _context.Services on b.IdService equals c.Id
                                          where a.Isocode == isocode
                                          select new { 
                                              CountryName = a.Name, 
                                              Service = c 
                                          }).ToListAsync();

                // Si no hay datos, retornar lista vacía
                if (!servicesData.Any())
                {
                    return new List<ServicesByCountry>();
                }

                // Agrupar por país y crear la respuesta con todos los servicios
                var result = servicesData
                    .GroupBy(x => x.CountryName)
                    .Select(group => new ServicesByCountry
                    {
                        NameCountry = group.Key,
                        Services = group.Select(g => g.Service).ToList()
                    })
                    .ToList();

                return result;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(e.Message, e);
            }
        }
    }
}
