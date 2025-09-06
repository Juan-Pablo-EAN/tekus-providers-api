using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.DTOs
{
    public class RequestProvider
    {
        public string Name { get; set; } = string.Empty;

        public string Nit { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public List<RequestService> Services { get; set; } = new List<RequestService>();

        public List<RequestCustomField> CustomFields { get; set; } = new List<RequestCustomField>();
    }

    public class RequestService
    {
        public string Name { get; set; } = string.Empty;

        public string ValuePerHourUsd { get; set; } = string.Empty;

        public List<RequestCountry> Countries { get; set; } = new List<RequestCountry>();
    }

    public class RequestCountry
    {
        public int Id { get; set; }

        public string Isocode { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string FlagImage { get; set; } = string.Empty;
        public List<object> ServicesCountries { get; set; } = new List<object>();
    }

    public class RequestCustomField
    {
        public string FieldName { get; set; } = string.Empty;

        public string FieldValue { get; set; } = string.Empty;
    }
}
