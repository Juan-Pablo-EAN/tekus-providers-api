using InfraLayer.Models;

namespace DomainLayer.DTOs
{
    public class ServicesByCountry
    {
        public string NameCountry { get; set; } = string.Empty;
        public List<Services> Services { get; set; } = [];
    }
}
