namespace WCKDRZR.Gaspar.Models
{
    public class ServiceResponse<T>
    {
        public T Data { get; set; }
        public ActionResultError Error { get; set; }
    }
}