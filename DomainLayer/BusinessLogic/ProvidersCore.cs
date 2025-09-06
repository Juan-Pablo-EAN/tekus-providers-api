using DomainLayer.DTOs;
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
        /// Obtiene la lista de proveedores desde la base de datos con toda la información relacionada
        /// Incluye: CustomFields, Services y Countries relacionados a esos servicios
        /// </summary>
        /// <returns>Lista completa de proveedores estructurada en JSON organizado</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<List<CompleteProviderDto>> GetCompleteProvidersListAsync()
        {
            try
            {
                _logger.LogInformation("Iniciando consulta completa de proveedores con información relacionada");

                // Cargar proveedores con todas las relaciones necesarias
                var providers = await _context.Providers
                    .Include(p => p.CustomFields)
                    .Include(p => p.ProvidersServices)
                        .ThenInclude(ps => ps.IdServiceNavigation)
                            .ThenInclude(s => s.ServicesCountries)
                                .ThenInclude(sc => sc.IdCountryNavigation)
                    .ToListAsync();

                var result = providers.Select(provider => new CompleteProviderDto
                {
                    Id = provider.Id,
                    Nit = provider.Nit,
                    Name = provider.Name,
                    Email = provider.Email,

                    CustomFields = provider.CustomFields.Select(cf => new CustomFieldCompleteDto
                    {
                        Id = cf.Id,
                        FieldName = cf.FieldName,
                        FieldValue = cf.FieldValue
                    }).ToList(),

                    Services = provider.ProvidersServices
                        .Where(ps => ps.IdServiceNavigation != null)
                        .Select(ps => new ServiceCompleteDto
                        {
                            Id = ps.IdServiceNavigation.Id,
                            Name = ps.IdServiceNavigation.Name,
                            ValuePerHourUsd = ps.IdServiceNavigation.ValuePerHourUsd,

                            Countries = ps.IdServiceNavigation.ServicesCountries
                                .Where(sc => sc.IdCountryNavigation != null)
                                .Select(sc => new CountryCompleteDto
                                {
                                    Id = sc.IdCountryNavigation.Id,
                                    Isocode = sc.IdCountryNavigation.Isocode,
                                    Name = sc.IdCountryNavigation.Name,
                                    FlagImage = sc.IdCountryNavigation.FlagImage
                                })
                                .Distinct()
                                .OrderBy(c => c.Name) // Ordenar por nombre de país
                                .ToList()
                        })
                        .Distinct()
                        .OrderBy(s => s.Name)
                        .ToList()
                })
                .OrderBy(p => p.Name)
                .ToList();

                _logger.LogInformation($"Consulta completa exitosa: {result.Count} proveedores obtenidos con información completa");

                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error obteniendo lista completa de proveedores con información relacionada");
                throw new InvalidOperationException(e.Message, e);
            }
        }

        /// <summary>
        /// Obtiene la lista básica de proveedores desde la base de datos (sin relaciones para evitar referencias circulares)
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<List<Providers>> GetProvidersList()
        {
            try
            {
                var providers = await _context.Providers.ToListAsync();
                return providers;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error obteniendo lista básica de proveedores");
                throw new InvalidOperationException(e.Message, e);
            }
        }

        /// <summary>
        /// Método para crear un nuevo proveedor completo en base de datos
        /// Incluye: Proveedor, CustomFields, Services con Countries relacionados
        /// </summary>
        /// <param name="provider">Objeto proveedor completo a crear</param>
        /// <returns>Retorna "OK" si la operación fue exitosa</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<string> CreateNewProvider(CompleteProviderDto provider)
        {
            try
            {
                _logger.LogInformation($"Iniciando creación de proveedor completo: {provider.Name}");

                try
                {
                    // 1. Crear el proveedor principal
                    var newProvider = new Providers
                    {
                        Nit = provider.Nit,
                        Name = provider.Name,
                        Email = provider.Email
                    };

                    await _context.Providers.AddAsync(newProvider);
                    await _context.SaveChangesAsync(); // Guardar para obtener el ID del proveedor

                    int providerId = newProvider.Id;
                    _logger.LogDebug($"Proveedor creado con ID: {providerId}");

                    // 2. Procesar CustomFields
                    if (provider.CustomFields?.Any() == true)
                    {
                        await ProcessCustomFields(provider.CustomFields, providerId);
                        _logger.LogDebug($"Procesados {provider.CustomFields.Count} campos personalizados");
                    }

                    // 3. Procesar Services con sus Countries
                    if (provider.Services?.Any() == true)
                    {
                        await ProcessServicesWithCountries(provider.Services, providerId);
                        _logger.LogDebug($"Procesados {provider.Services.Count} servicios");
                    }

                    // Guardar todos los cambios restantes
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Proveedor completo creado exitosamente: ID={providerId}, " +
                                         $"CustomFields={provider.CustomFields?.Count ?? 0}, " +
                                         $"Services={provider.Services?.Count ?? 0}");

                    return "OK";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en transacción, realizando rollback");
                    throw;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error al crear un nuevo proveedor completo");
                throw new InvalidOperationException(e.Message, e);
            }
        }

        /// <summary>
        /// Procesa los campos personalizados del proveedor
        /// </summary>
        /// <param name="customFields">Lista de campos personalizados</param>
        /// <param name="providerId">ID del proveedor</param>
        private async Task ProcessCustomFields(List<CustomFieldCompleteDto> customFields, int providerId)
        {
            var customFieldsToAdd = customFields.Select(cf => new CustomFields
            {
                IdProvider = providerId,
                FieldName = cf.FieldName,
                FieldValue = cf.FieldValue
            }).ToList();

            await _context.CustomFields.AddRangeAsync(customFieldsToAdd);
        }

        /// <summary>
        /// Procesa los servicios y sus países relacionados
        /// </summary>
        /// <param name="services">Lista de servicios del proveedor</param>
        /// <param name="providerId">ID del proveedor</param>
        private async Task ProcessServicesWithCountries(List<ServiceCompleteDto> services, int providerId)
        {
            foreach (var serviceDto in services)
            {
                int serviceId;

                // Crear nuevo servicio
                var newService = new Services
                {
                    Name = serviceDto.Name,
                    ValuePerHourUsd = serviceDto.ValuePerHourUsd
                };

                await _context.Services.AddAsync(newService);
                await _context.SaveChangesAsync(); // Guardar para obtener el ID

                serviceId = newService.Id;
                _logger.LogDebug($"Nuevo servicio creado: {serviceDto.Name} con ID: {serviceId}");


                // 2. Crear relación ProvidersServices
                var providerService = new ProvidersServices
                {
                    IdProvider = providerId,
                    IdService = serviceId
                };

                await _context.ProvidersServices.AddAsync(providerService);

                // 3. Procesar países del servicio
                if (serviceDto.Countries?.Any() == true)
                {
                    await ProcessServiceCountries(serviceDto.Countries, serviceId);
                }
            }
        }

        /// <summary>
        /// Procesa los países relacionados a un servicio
        /// </summary>
        /// <param name="countries">Lista de países</param>
        /// <param name="serviceId">ID del servicio</param>
        private async Task ProcessServiceCountries(List<CountryCompleteDto> countries, int serviceId)
        {
            foreach (var countryDto in countries)
            {
                // Crear relación ServicesCountries
                var serviceCountry = new ServicesCountries
                {
                    IdService = serviceId,
                    IdCountry = countryDto.Id
                };

                await _context.ServicesCountries.AddAsync(serviceCountry);
                _logger.LogDebug($"Relación servicio-país creada: ServiceID={serviceId}, CountryID={countryDto.Id}");
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

                if (result > 0)
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
            catch (Exception e)
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

                    foreach (ProvidersServices provService in provider.ProvidersServices)
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
                }
                else
                {
                    return "No se encontró el proveedor";
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error al eliminar el proveedor con ID: {id}");
                throw new InvalidOperationException(e.Message, e);
            }
        }
    }
}
