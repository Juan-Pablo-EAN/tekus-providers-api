using DomainLayer.BusinessLogic;
using DomainLayer.Interfaces;
using InfraLayer.Models;
using Microsoft.AspNetCore.Mvc;

namespace TekusProvidersAPI.Controllers
{
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

            //Crear proveedor
            //Editar proveedor
            //Eliminar proveedor
        }

        [HttpGet("[action]")]
        public async Task<List<Providers>> GetProviders()
        {
            try
            {
                return await _providersCore.GetProvidersList();
            } catch(Exception e)
            {
                string message = "Ocurrió un error al obtener los proveedores";
                _logger.LogError(e, message + nameof(GetProviders) + " - " + e.Message);
                return [];
            }
        }

        
    }
}
