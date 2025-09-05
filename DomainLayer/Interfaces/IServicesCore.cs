using DomainLayer.DTOs;
using InfraLayer.Models;

namespace DomainLayer.Interfaces
{
    public interface IServicesCore
    {
        public Task<string> CreateNewService(Services services);
        public Task<string> UpdateService(Services service);
        public Task<string> DeleteService(int id);
        public Task<List<ServicesByProviderModel>> GetServicesByProviderName(string name);
        public Task<List<ServicesByCountry>> GetServicesByCountry(string isocode);

    }
}
