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
                _logger.LogError(e, "Error retrieving providers list");
                throw new InvalidOperationException(e.Message);
            }
        }
    }
}
