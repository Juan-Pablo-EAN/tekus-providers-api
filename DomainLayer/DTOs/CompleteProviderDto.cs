namespace DomainLayer.DTOs
{
    public class CompleteProviderDto
    {
        public int Id { get; set; }
        public string Nit { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<CustomFieldCompleteDto> CustomFields { get; set; } = new List<CustomFieldCompleteDto>();
        public List<ServiceCompleteDto> Services { get; set; } = new List<ServiceCompleteDto>();
    }

    public class CustomFieldCompleteDto
    {
        public int Id { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string FieldValue { get; set; } = string.Empty;
    }

    public class ServiceCompleteDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ValuePerHourUsd { get; set; } = string.Empty;
        public List<CountryCompleteDto> Countries { get; set; } = new List<CountryCompleteDto>();
    }
    public class CountryCompleteDto
    {
        public int Id { get; set; }
        public string Isocode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FlagImage { get; set; } = string.Empty;
    }
}