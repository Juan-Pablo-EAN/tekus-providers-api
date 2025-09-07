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
    public class ServicesController : ControllerBase
    {
        private readonly TekusProvidersContext _context;
        private readonly ILogger<ServicesController> _logger;
        public IServicesCore _servicesCore;

        public ServicesController(TekusProvidersContext context, ILogger<ServicesController> logger)
        {
            _context = context;
            _logger = logger;
            _servicesCore = new ServicesCore(_context, logger);
        }

        /// <summary>
        /// Crea un nuevo servicio junto con su proveedor y países asociados
        /// </summary>
        /// <param name="requestService">Modelo con la información del servicio a crear</param>
        /// <returns>Respuesta de la operación</returns>
        [HttpPost("[action]")]
        public async Task<ActionResult<ProvidersResponse>> CreateNewService([FromBody] RequestModel requestService)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(Utilities.SetFormatResponse("El modelo recibido no es válido.", false));
            }

            try
            {
                Services request = JsonConvert.DeserializeObject<Services>(requestService.ObjectRequest)!;
                string response = await _servicesCore.CreateNewService(request);

                return (response.Contains("OK")) ? 
                    Ok(Utilities.SetFormatResponse("Servicio creado exitosamente", true)) :
                    BadRequest(Utilities.SetFormatResponse(response, false));
            }
            catch (Exception e)
            {
                string message = "Ocurrió un error al crear el servicio";
                _logger.LogError(e, "{Message} - {Method} - {ExceptionMessage}", message, nameof(CreateNewService), e.Message);
                return StatusCode(500, Utilities.SetFormatResponse($"{message}: {e.Message}", false));
            }
        }

        /// <summary>
        /// Actualiza la información de un servicio junto con su proveedor y países relacionados
        /// </summary>
        /// <param name="requestService">Modelo con la información del servicio a actualizar</param>
        /// <returns>Respuesta de la operación</returns>
        [HttpPut("[action]")]
        public async Task<ActionResult<ProvidersResponse>> UpdateService([FromBody] RequestModel requestService)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(Utilities.SetFormatResponse("El modelo recibido no es válido.", false));
            }

            try
            {
                ServiceCompleteDto request = JsonConvert.DeserializeObject<ServiceCompleteDto>(requestService.ObjectRequest)!;
                
                string response = await _servicesCore.UpdateService(request);

                return (response.Contains("OK")) ? 
                    Ok(Utilities.SetFormatResponse("Servicio actualizado exitosamente", true)) :
                    BadRequest(Utilities.SetFormatResponse(response, false));
            }
            catch (Exception e)
            {
                string message = "Ocurrió un error al actualizar el servicio";
                _logger.LogError(e, "{Message} - {Method} - {ExceptionMessage}", message, nameof(UpdateService), e.Message);
                return StatusCode(500, Utilities.SetFormatResponse($"{message}: {e.Message}", false));
            }
        }

        /// <summary>
        /// Elimina un servicio de la tabla Services y de la tabla ServicesCountries
        /// </summary>
        /// <param name="id">ID del servicio a eliminar</param>
        /// <returns>Respuesta de la operación</returns>
        [HttpDelete("[action]/{id}")]
        public async Task<ActionResult<ProvidersResponse>> DeleteService(int id)
        {
            if (id <= 0)
            {
                return BadRequest(Utilities.SetFormatResponse("El ID del servicio no es válido.", false));
            }

            try
            {
                string response = await _servicesCore.DeleteService(id);
                
                return (response.Contains("OK")) ? 
                    Ok(Utilities.SetFormatResponse("Servicio eliminado exitosamente", true)) :
                    BadRequest(Utilities.SetFormatResponse(response, false));
            }
            catch (Exception e)
            {
                string message = "Ocurrió un error al eliminar el servicio";
                _logger.LogError(e, "{Message} - {Method} - {ExceptionMessage}", message, nameof(DeleteService), e.Message);
                return StatusCode(500, Utilities.SetFormatResponse($"{message}: {e.Message}", false));
            }
        }

        /// <summary>
        /// Obtiene la lista de servicios por nombre del proveedor
        /// </summary>
        /// <param name="providerName">Nombre del proveedor (puede ser parcial)</param>
        /// <returns>Lista de servicios asociados al proveedor</returns>
        [HttpGet("[action]/{providerName}")]
        public async Task<ActionResult<List<ServicesByProviderModel>>> GetServicesByProviderName(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
            {
                return BadRequest(new { error = "El nombre del proveedor es requerido" });
            }

            try
            {
                var result = await _servicesCore.GetServicesByProviderName(providerName);
                
                if (result.Count == 0)
                {
                    return NotFound(new { message = $"No se encontraron servicios para el proveedor: {providerName}" });
                }

                _logger.LogInformation($"Se encontraron {result.Count} servicios para el proveedor: {providerName}");
                return Ok(result);
            }
            catch (Exception e)
            {
                string message = "Ocurrió un error al obtener los servicios por proveedor";
                _logger.LogError(e, "{Message} - {Method} - {ExceptionMessage}", message, nameof(GetServicesByProviderName), e.Message);
                return StatusCode(500, new { error = message });
            }
        }

        /// <summary>
        /// Obtiene la lista de servicios por código ISO del país
        /// </summary>
        /// <param name="isoCode">Código ISO del país (ej: CO, US, ES)</param>
        /// <returns>Lista de servicios disponibles en el país especificado</returns>
        [HttpGet("[action]/{isoCode}")]
        public async Task<ActionResult<List<ServicesByCountry>>> GetServicesByCountry(string isoCode)
        {
            if (string.IsNullOrEmpty(isoCode))
            {
                return BadRequest(new { error = "El código ISO del país es requerido" });
            }

            try
            {
                var result = await _servicesCore.GetServicesByCountry(isoCode.ToUpper());
                
                if (result.Count == 0)
                {
                    return NotFound(new { message = $"No se encontraron servicios para el país con código ISO: {isoCode}" });
                }

                _logger.LogInformation($"Se encontraron {result.Count} grupos de servicios para el país: {isoCode}");
                return Ok(result);
            }
            catch (Exception e)
            {
                string message = "Ocurrió un error al obtener los servicios por país";
                _logger.LogError(e, "{Message} - {Method} - {ExceptionMessage}", message, nameof(GetServicesByCountry), e.Message);
                return StatusCode(500, new { error = message });
            }
        }
    }
}
