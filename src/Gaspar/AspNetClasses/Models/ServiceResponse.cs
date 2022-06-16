using Microsoft.AspNetCore.Mvc;

namespace WCKDRZR.Gaspar.Models
{
    public class ServiceResponse<T>
    {
        public T Data { get; set; }
        public ActionResultError Error { get; set; }

        public bool Success => Error == null;
        public bool HasError => Error != null;
        public ObjectResult Problem => Error == null ? null : new ObjectResult(Error) { StatusCode = Error.Status };
    }
}