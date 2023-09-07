namespace WCKDRZR.Gaspar.Models
{
    public class ServiceResponse
    {
        public ActionResultError? Error { get; set; }

        public bool Success => Error == null;
        public bool HasError => Error != null;
    }

    public class ServiceResponse<T> : ServiceResponse
    {
        public T? Data { get; set; }
    }

    public class VoidObject { }
}