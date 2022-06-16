using Microsoft.AspNetCore.Mvc;

namespace WCKDRZR.Gaspar.Models
{
    [ApiController]
    public class ServiceResponse<T> : ControllerBase
    {
        public T Data { get; set; }
        public ActionResultError Error { get; set; }

        public ObjectResult Problem() => Error == null ? null : Problem(Error.Detail, Error.Instance, Error.Status, Error.Title, Error.Type);
    }
}