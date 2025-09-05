using System;
using System.Collections.Generic;

namespace InfraLayer.Models;

public partial class Services
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string ValuePerHourUsd { get; set; } = null!;

    public virtual ICollection<ProvidersServices> ProvidersServices { get; set; } = new List<ProvidersServices>();

    public virtual ICollection<ServicesCountries> ServicesCountries { get; set; } = new List<ServicesCountries>();
}
