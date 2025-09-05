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
        /// Actualiza la información de un servicio junto con su proveedor y países relacionados
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<string> UpdateService(Services service)
        {
            try
            {
                var existingService = await _context.Services
                .Include(s => s.ProvidersServices)
                 .Include(s => s.ServicesCountries)
                .FirstOrDefaultAsync(s => s.Id == service.Id);

                existingService!.Name = service.Name;
                existingService!.ValuePerHourUsd = service.ValuePerHourUsd;
                existingService!.ProvidersServices = service.ProvidersServices;
                existingService!.ServicesCountries = service.ServicesCountries;

                int result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation($"Se ha actualizado el servicio con ID {service.Id} exitosamente");
                    return "OK";
                }
                else
                {
                    string response = $"No hubo cambios para el servicio con ID: {service.Id}";
                    _logger.LogInformation(response);
                    return response;
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
