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

                    if (provider.CustomFields?.Any() == true)
                    {
                        await ProcessCustomFields(provider.CustomFields, providerId);
                        _logger.LogDebug($"Procesados {provider.CustomFields.Count} campos personalizados");
                    }

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

                var newService = new Services
                {
                    Name = serviceDto.Name,
                    ValuePerHourUsd = serviceDto.ValuePerHourUsd
                };

                await _context.Services.AddAsync(newService);
                await _context.SaveChangesAsync(); // Guardar para obtener el ID

                serviceId = newService.Id;
                _logger.LogDebug($"Nuevo servicio creado: {serviceDto.Name} con ID: {serviceId}");


                var providerService = new ProvidersServices
                {
                    IdProvider = providerId,
                    IdService = serviceId
                };

                await _context.ProvidersServices.AddAsync(providerService);

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
        /// Método para actualizar un proveedor existente en base de datos junto con sus campos personalizados
        /// Incluye lógica completa para: actualizar, agregar y eliminar CustomFields
        /// </summary>
        /// <param name="provider">Objeto proveedor completo con los datos actualizados</param>
        /// <returns>Retorna "OK" si la operación fue exitosa</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<string> UpdateProvider(CompleteProviderDto provider)
        {
            try
            {
                _logger.LogInformation($"Iniciando actualización de proveedor: {provider.Name} (ID: {provider.Id})");
                
                try
                {
                    var existingProvider = await _context.Providers
                        .Include(p => p.CustomFields)
                        .FirstOrDefaultAsync(p => p.Id == provider.Id);

                    if (existingProvider == null)
                    {
                        string notFoundMsg = "Proveedor no encontrado";
                        _logger.LogWarning($"El proveedor con ID {provider.Id} no existe");
                        return notFoundMsg;
                    }

                    bool providerChanged = false;
                    if (existingProvider.Nit != provider.Nit)
                    {
                        existingProvider.Nit = provider.Nit;
                        providerChanged = true;
                    }
                    if (existingProvider.Name != provider.Name)
                    {
                        existingProvider.Name = provider.Name;
                        providerChanged = true;
                    }
                    if (existingProvider.Email != provider.Email)
                    {
                        existingProvider.Email = provider.Email;
                        providerChanged = true;
                    }

                    await UpdateCustomFields(existingProvider, provider.CustomFields ?? new List<CustomFieldCompleteDto>());

                    int result = await _context.SaveChangesAsync(); //Se guardan todos los cambios

                    if (result > 0 || providerChanged)
                    {
                        _logger.LogInformation($"Proveedor actualizado exitosamente: ID={provider.Id}, " +
                                             $"CustomFields procesados={provider.CustomFields?.Count ?? 0}");
                        return "OK";
                    }
                    else
                    {
                        string noChangesMsg = $"No hubo cambios para el proveedor con ID: {provider.Id}";
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
                _logger.LogError(e, $"Error al actualizar el proveedor con ID: {provider.Id}");
                throw new InvalidOperationException(e.Message, e);
            }
        }

        /// <summary>
        /// Actualiza los CustomFields de un proveedor de manera inteligente
        /// Maneja: actualización de existentes, eliminación de faltantes y creación de nuevos
        /// </summary>
        /// <param name="existingProvider">Proveedor existente con sus CustomFields cargados</param>
        /// <param name="newCustomFields">Lista nueva de CustomFields desde el cliente</param>
        private async Task UpdateCustomFields(Providers existingProvider, List<CustomFieldCompleteDto> newCustomFields)
        {
            var existingFields = existingProvider.CustomFields.ToList();
            
            _logger.LogDebug($"CustomFields - Existentes: {existingFields.Count}, Nuevos: {newCustomFields.Count}");

            // Elimina campos que ya no están en la nueva lista
            var fieldsToRemove = existingFields
                .Where(existing => !newCustomFields.Any(newField => 
                    newField.Id > 0 && newField.Id == existing.Id))
                .ToList();

            if (fieldsToRemove.Any())
            {
                _context.CustomFields.RemoveRange(fieldsToRemove);
            }

            foreach (var newField in newCustomFields)
            {
                if (newField.Id > 0) //Si existe el campo se actualiza
                {
                    var existingField = existingFields.FirstOrDefault(ef => ef.Id == newField.Id);
                    if (existingField != null)
                    {
                        if (existingField.FieldName != newField.FieldName)
                        {
                            existingField.FieldName = newField.FieldName;
                        }
                        
                        if (existingField.FieldValue != newField.FieldValue)
                        {
                            existingField.FieldValue = newField.FieldValue;
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Campo con ID {newField.Id} no encontrado para actualizar");
                    }
                }
                else
                {
                    var newCustomField = new CustomFields
                    {
                        IdProvider = existingProvider.Id,
                        FieldName = newField.FieldName,
                        FieldValue = newField.FieldValue
                    };

                    await _context.CustomFields.AddAsync(newCustomField);
                    _logger.LogDebug($"Agregando nuevo campo: {newField.FieldName}");
                }
            }
            
            _logger.LogDebug($"Procesamiento de CustomFields completado");
        }

        /// <summary>
        /// Elimina un proveedor de la base de datos junto con sus relaciones
        /// </summary>
        /// <param name="id">ID del proveedor a eliminar</param>
        /// <returns>Retorna "OK" si la operación fue exitosa</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<string> DeleteProvider(int id)
        {
            try
            {
                _logger.LogInformation($"Iniciando eliminación del proveedor con ID: {id}");

                var provider = await _context.Providers
                    .Include(p => p.CustomFields)
                    .Include(p => p.ProvidersServices)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (provider == null)
                {
                    string notFoundMsg = "Proveedor no encontrado";
                    _logger.LogWarning($"El proveedor con ID {id} no existe");
                    return notFoundMsg;
                }

                // Eliminar campos personalizados
                if (provider.CustomFields.Any())
                {
                    _context.CustomFields.RemoveRange(provider.CustomFields);
                    _logger.LogDebug($"Eliminando {provider.CustomFields.Count} campos personalizados");
                }

                // Eliminar relaciones ProvidersServices (no los Services)
                if (provider.ProvidersServices.Any())
                {
                    _context.ProvidersServices.RemoveRange(provider.ProvidersServices);
                    _logger.LogDebug($"Eliminando {provider.ProvidersServices.Count} relaciones proveedor-servicio");
                }

                // Eliminar el proveedor
                _context.Providers.Remove(provider);

                int result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation($"Proveedor eliminado exitosamente: ID={id}, Nombre={provider.Name}");
                    return "OK";
                }
                else
                {
                    string errorMsg = "Ocurrió un error al eliminar el proveedor";
                    _logger.LogError(errorMsg);
                    return errorMsg;
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
