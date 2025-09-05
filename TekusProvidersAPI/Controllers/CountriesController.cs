using DomainLayer.BusinessLogic;
using DomainLayer.DTOs;
using DomainLayer.Interfaces;
using InfraLayer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace TekusProvidersAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CountriesController : ControllerBase
    {
        private readonly TekusProvidersContext _context;
        private readonly ILogger<CountriesController> _logger;
        private readonly ISyncCountries _syncCountries;
        private readonly IOptions<CountriesServiceConfig> _countriesServiceConfig;

        public CountriesController(TekusProvidersContext context, IOptions<CountriesServiceConfig> countriesService, ILogger<CountriesController> logger, HttpClient httpClient)
        {
            _context = context;
            _logger = logger;
            _countriesServiceConfig = countriesService;
            _syncCountries = new SyncCountries(_context, countriesService, logger, httpClient);
        }

        /// <summary>
        /// Sincroniza la lista de países con una fuente externa
        /// </summary>
        /// <returns></returns>
        [HttpPost("[action]")]
        public async Task<IActionResult> SyncCountriesList()
        {
            try
            {
                await _syncCountries.SynchronizeList();
                return Ok(new { message = "Sincronización de países completada exitosamente" });
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{nameof(SyncCountriesList)} - Error al sincronizar la lista de países: {e.Message}");
                return StatusCode(500, new { error = "Error interno del servidor durante la sincronización" });
            }
        }
    }
}
