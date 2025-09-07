using System.Text.Json.Serialization;

namespace DomainLayer.DTOs
{
    public class RestCountriesResponse
    {
        [JsonPropertyName("flags")]
        public Flags Flags { get; set; } = null!;

        [JsonPropertyName("name")]
        public Name Name { get; set; } = null!;

        [JsonPropertyName("cca3")]
        public string Cca3 { get; set; } = null!;

        [JsonPropertyName("translations")]
        public Translations Translations { get; set; } = null!;
    }

    public class Flags
    {
        [JsonPropertyName("png")]
        public string Png { get; set; } = null!;
    }

    public class Name
    {
        [JsonPropertyName("common")]
        public string Common { get; set; } = null!;
    }

    public class Translations
    {
        [JsonPropertyName("spa")]
        public Name? Spa { get; set; }
    }
}