using InfraLayer.Models;

namespace DomainLayer.Interfaces
{
    public interface IProvidersCore
    {
        public Task<List<Providers>> GetProvidersList();
    }
}
