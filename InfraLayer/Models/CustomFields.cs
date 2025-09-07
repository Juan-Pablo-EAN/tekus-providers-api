namespace InfraLayer.Models;

public partial class CustomFields
{
    public int Id { get; set; }

    public int IdProvider { get; set; }

    public string FieldName { get; set; } = null!;

    public string FieldValue { get; set; } = null!;

    public virtual Providers IdProviderNavigation { get; set; } = null!;
}
