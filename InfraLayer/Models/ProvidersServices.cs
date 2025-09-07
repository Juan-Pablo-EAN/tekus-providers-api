namespace InfraLayer.Models;

public partial class ProvidersServices
{
    public int Id { get; set; }

    public int IdProvider { get; set; }

    public int IdService { get; set; }

    public virtual Providers IdProviderNavigation { get; set; } = null!;

    public virtual Services IdServiceNavigation { get; set; } = null!;
}
