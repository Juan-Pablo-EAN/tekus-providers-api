using InfraLayer.Models;
using Microsoft.AspNetCore.Mvc;

namespace TekusProvidersAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicesController : Controller
    {
        public ServicesController(TekusProvidersContext Context)
        {
            //Crear servicio
            //Editar servicio
            //Eliminar servicio
            //Obtener servicios por proveedor
        }
    }
}
