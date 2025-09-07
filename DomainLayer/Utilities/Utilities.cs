using DomainLayer.DTOs;

namespace DomainLayer.Utilities
{
    public class Utilities
    {
        public static ProvidersResponse SetFormatResponse(string data, bool status)
        {
            return new ProvidersResponse() { Message = data, Status = status };
        }
    }
}
