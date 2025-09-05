using System;
using System.Collections.Generic;

namespace InfraLayer.Models;

public partial class ServicesCountries
{
    public int Id { get; set; }

    public int IdService { get; set; }

    public int IdCountry { get; set; }

    public virtual Countries IdCountryNavigation { get; set; } = null!;

    public virtual Services IdServiceNavigation { get; set; } = null!;
}
