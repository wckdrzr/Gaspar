namespace WCKDRZR.CSharpExporter.Models
{
    public class ServiceResponse<T>
    {
        public T Data { get; set; }
        public ActionResultError Error { get; set; }
    }
}