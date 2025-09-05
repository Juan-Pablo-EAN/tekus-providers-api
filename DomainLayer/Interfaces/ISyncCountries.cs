using DomainLayer.DTOs;

namespace DomainLayer.Interfaces
{
    public interface ISyncCountries
    {
        Task SynchronizeList();
        Task<List<RestCountriesResponse>?> GetCountriesFromApi();
    }
}
