using DomainLayer.DTOs;
using DomainLayer.Interfaces;
using InfraLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace DomainLayer.BusinessLogic
{
    public class SyncCountries : ISyncCountries
    {
        private readonly TekusProvidersContext _context;
        private readonly IOptions<CountriesServiceConfig> _countriesService;
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public SyncCountries(TekusProvidersContext context, IOptions<CountriesServiceConfig> countriesService, ILogger logger, HttpClient httpClient)
        {
            _context = context;
            _countriesService = countriesService;
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Consume el servicio externo para traer el listado de países y los sincroniza con la tabla Countries de la base de datos
        /// </summary>
        /// <returns></returns>
        public async Task SynchronizeList()
        {
            try
            {
                var externalCountries = await GetCountriesFromApi(); //se obtiene la lista de países desde la API externa

                if (externalCountries?.Any() == true)
                {
                    await SyncCountriesWithDatabaseAsync(externalCountries); //se sincroniza la tabla Countries en base de datos
                }
                else
                {
                    _logger.LogWarning("No se obtuvieron países de la API externa");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{nameof(SynchronizeList)} - Error al sincronizar la lista de países: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Consume la API de RestCountries para obtener la lista de países
        /// </summary>
        /// <returns>Lista de países obtenidos de la API</returns>
        public async Task<List<RestCountriesResponse>?> GetCountriesFromApi()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(_countriesService.Value!.UrlAPI);
                
                if (response.IsSuccessStatusCode)
                {
                    string jsonContent = await response.Content.ReadAsStringAsync();

                    JsonSerializerOptions options = new()
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var countries = JsonSerializer.Deserialize<List<RestCountriesResponse>>(jsonContent, options);
                    _logger.LogInformation($"Se obtuvieron {countries?.Count ?? 0} países de la API");
                    
                    return countries;
                }
                else
                {
                    _logger.LogError($"Error al consultar la API. Status Code: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error al consumir la API de RestCountries");
                throw;
            }
        }

        /// <summary>
        /// Sincroniza los países obtenidos de la API con la tabla Countries de la base de datos
        /// </summary>
        /// <param name="externalCountries">Lista de países obtenidos de la API</param>
        private async Task SyncCountriesWithDatabaseAsync(List<RestCountriesResponse> externalCountries)
        {
            try
            {
                // Obtener países existentes en la base de datos
                var existingCountries = await _context.Countries.ToListAsync();
                
                foreach (var apiCountry in externalCountries)
                {
                    if (string.IsNullOrEmpty(apiCountry.Cca3) || string.IsNullOrEmpty(apiCountry.Name?.Common))
                    {
                        _logger.LogWarning($"País con datos incompletos ignorado: {apiCountry.Name?.Common ?? "Sin nombre"}");
                        continue;
                    }

                    // Convertir el código ISO a bytes para comparar
                    var isoCodeBytes = Encoding.UTF8.GetBytes(apiCountry.Cca3);
                    
                    // Buscar si el país ya existe en la base de datos
                    var existingCountry = existingCountries.FirstOrDefault(c => 
                        c.Isocode == apiCountry.Cca3);

                    if (existingCountry == null)
                    {
                        // Crear nuevo país
                        Countries newCountry = new()
                        {
                            Isocode = apiCountry.Cca3,
                            Name = apiCountry.Translations.Spa!.Common,
                            FlagImage = apiCountry.Flags?.Png ?? string.Empty
                        };

                        _context.Countries.Add(newCountry);
                        
                        _logger.LogDebug($"Nuevo país agregado: {newCountry.Name} ({apiCountry.Cca3})");
                    }
                    else
                    {
                        // Actualizar país existente si es necesario
                        string name = apiCountry.Translations.Spa!.Common;
                        string flagImage = apiCountry.Flags?.Png ?? string.Empty;

                        bool updated = false;
                        
                        if (existingCountry.Name != name)
                        {
                            existingCountry.Name = name;
                            updated = true;
                        }

                        if (existingCountry.FlagImage != flagImage)
                        {
                            existingCountry.FlagImage = flagImage;
                            updated = true;
                        }

                        if (updated)
                            _logger.LogDebug($"País actualizado: {existingCountry.Name} ({apiCountry.Cca3})");
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Sincronización completada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al sincronizar países con la base de datos");
                throw;
            }
        }
    }
}
