using DomainLayer.BusinessLogic;
using DomainLayer.DTOs;
using DomainLayer.Interfaces;
using DomainLayer.Utilities;
using InfraLayer.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace TekusProvidersAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProvidersController : Controller
    {
        private readonly TekusProvidersContext _context;
        private readonly ILogger<ProvidersController> _logger;
        public IProvidersCore _providersCore;

        public ProvidersController(TekusProvidersContext context, ILogger<ProvidersController> logger)
        {
            _context = context;
            _logger = logger;
            _providersCore = new ProvidersCore(_context, logger);
        }

        /// <summary>
        /// Obtiene una lista de la información básica de los proveedores
        /// </summary>
        /// <returns>Lista básica de proveedores</returns>
        [HttpGet("[action]")]
        public async Task<ActionResult<List<Providers>>> GetProviders()
        {
            try
            {
                var result = await _providersCore.GetProvidersList();
                return Ok(result);
            }
            catch (Exception e)
            {
                string message = "Ocurrió un error al obtener los proveedores";
                _logger.LogError(e, "{Message} - {Method} - {ExceptionMessage}", message, nameof(GetProviders), e.Message);
                return StatusCode(500, new { error = message });
            }
        }

        /// <summary>
        /// Obtiene una lista completa de los proveedores, incluyendo sus servicios, países y campos personalizados
        /// </summary>
        /// <returns></returns>
        [HttpGet("[action]")]
        public async Task<ActionResult<List<CompleteProviderDto>>> GetCompleteProviders()
        {
            try
            {
                _logger.LogInformation("Solicitud de información completa de proveedores recibida");

                var result = await _providersCore.GetCompleteProvidersListAsync();

                _logger.LogInformation($"Respuesta exitosa: {result.Count} proveedores con información completa");

                return Ok(result);
            }
            catch (Exception e)
            {
                string message = "Ocurrió un error al obtener la información completa de proveedores";
                _logger.LogError(e, "{Message} - {Method} - {ExceptionMessage}", message, nameof(GetCompleteProviders), e.Message);
                return StatusCode(500, new { error = message, details = e.Message });
            }
        }

        /// <summary>
        /// Crea un nuevo proveedor con sus campos personalizados en base de datos
        /// </summary>
        /// <param name="requestProvider"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [HttpPost("[action]")]
        public async Task<ProvidersResponse> CreateNewProvider([FromBody] RequestModel requestProvider)
        {
            if (!ModelState.IsValid)
            {
                return Utilities.SetFormatResponse("El modelo recibido no es válido.", false);
            }

            try
            {
                CompleteProviderDto request = JsonConvert.DeserializeObject<CompleteProviderDto>(requestProvider.ObjectRequest)!;
                string response = await _providersCore.CreateNewProvider(request);

                return (response.Contains("OK")) ? Utilities.SetFormatResponse(response, true)
                    : Utilities.SetFormatResponse(response, false);
            }
            catch (Exception e)
            {
                string message = "Ocurrió un error al crear proveedor";
                _logger.LogError(e, "{Message} - {Method} - {ExceptionMessage}", message, nameof(CreateNewProvider), e.Message);
                return Utilities.SetFormatResponse($"{message}: {e.Message}", false);
            }
        }

        /// <summary>
        /// Actualiza la información de un proveedor o de sus campos personalizados
        /// </summary>
        /// <param name="requestProvider">Modelo con la información del proveedor a actualizar</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [HttpPut("[action]")]
        public async Task<ProvidersResponse> UpdateProvider([FromBody] RequestModel requestProvider)
        {
            if (!ModelState.IsValid)
                return Utilities.SetFormatResponse("El modelo recibido no es válido.", false);

            try
            {
                Providers request = JsonConvert.DeserializeObject<Providers>(requestProvider.ObjectRequest)!;
                string response = await _providersCore.UpdateProvider(request);

                return (response.Contains("OK")) ? Utilities.SetFormatResponse("Proveedor actualizado exitosamente", true)
                    : Utilities.SetFormatResponse(response, false);
            }
            catch (Exception e)
            {
                string message = "Ocurrió un error al actualizar el proveedor";
                _logger.LogError(e, "{Message} - {Method} - {ExceptionMessage}", message, nameof(UpdateProvider), e.Message);
                return Utilities.SetFormatResponse($"{message}: {e.Message}", false);
            }
        }

        /// <summary>
        /// Elimina el proveedor junto con sus servicios y campos personalizados
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("[action]/{id}")]
        public async Task<ProvidersResponse> DeleteProvider(int id)
        {
            if (id <= 0)
                return Utilities.SetFormatResponse("El id del proveedor no es válido.", false);
            try
            {
                string response = await _providersCore.DeleteProvider(id);
                return (response.Contains("OK")) ? Utilities.SetFormatResponse("Proveedor eliminado exitosamente", true)
                    : Utilities.SetFormatResponse(response, false);
            }
            catch (Exception e)
            {
                string message = "Ocurrió un error al eliminar el proveedor";
                _logger.LogError(e, "{Message} - {Method} - {ExceptionMessage}", message, nameof(DeleteProvider), e.Message);
                return Utilities.SetFormatResponse($"{message}: {e.Message}", false);
            }
        }
    }
}
