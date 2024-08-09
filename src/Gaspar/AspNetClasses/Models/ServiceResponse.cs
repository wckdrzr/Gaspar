using System.Diagnostics.CodeAnalysis;

namespace WCKDRZR.Gaspar.Models
{
    public class ServiceResponse
    {
        public ActionResultError? Error { get; set; }

        [MemberNotNullWhen(false, nameof(Error))]
        public bool Success => Error == null;

        [MemberNotNullWhen(true, nameof(Error))]
        public bool HasError => Error != null;
    }

    public class ServiceResponse<T> : ServiceResponse
    {
        [MemberNotNullWhen(false, nameof(Error))]
        public T? Data { get; set; }
        
        public new ActionResultError? Error { get; set; }

        [MemberNotNullWhen(true, nameof(Data))]
        [MemberNotNullWhen(false, nameof(Error))]
        public new bool Success => Error == null;

        [MemberNotNullWhen(false, nameof(Data))]
        [MemberNotNullWhen(true, nameof(Error))]
        public new bool HasError => Error != null;
    }

    public class VoidObject { }
}