using DomainLayer.DTOs;
using InfraLayer.Models;

namespace DomainLayer.Interfaces
{
    public interface ISyncCountries
    {
        public Task SynchronizeList();
        public Task<List<RestCountriesResponse>?> GetCountriesFromApi();
        public List<Countries> GetCountriesFromDb();

    }
}
