namespace InfraLayer.Models;

public partial class Countries
{
    public int Id { get; set; }

    public string Isocode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string FlagImage { get; set; } = null!;

    public virtual ICollection<ServicesCountries> ServicesCountries { get; set; } = new List<ServicesCountries>();
}
