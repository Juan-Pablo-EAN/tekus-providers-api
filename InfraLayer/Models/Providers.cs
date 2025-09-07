namespace InfraLayer.Models;

public partial class Providers
{
    public int Id { get; set; }

    public string Nit { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public virtual ICollection<CustomFields> CustomFields { get; set; } = new List<CustomFields>();

    public virtual ICollection<ProvidersServices> ProvidersServices { get; set; } = new List<ProvidersServices>();
}
