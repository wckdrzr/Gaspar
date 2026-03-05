using System.Threading.Tasks;

namespace WCKDRZR.Gaspar.Models;

public static class ServiceResponseExtensions
{
    public static void ThrowOnError(this ServiceResponse response)
    {
        if (response.HasError)
        {
            throw new GasparException($"Service call failed with error: {response.Error.Title} - {response.Error.Detail}");
        }
    }

    public static T ThrowOnError<T>(this ServiceResponse<T> response)
    {
        if (response.HasError)
        {
            throw new GasparException($"Service call failed with error: {response.Error.Title} - {response.Error.Detail}");
        }
        return response.Data;
    }

    public static async Task ThrowOnError(this Task<ServiceResponse> responseTask)
    {
        var response = await responseTask;
        if (response.HasError)
        {
            throw new GasparException($"Service call failed with error: {response.Error.Title} - {response.Error.Detail}");
        }
    }

    public static async Task<T> ThrowOnError<T>(this Task<ServiceResponse<T>> responseTask)
    {
        var response = await responseTask;
        if (response.HasError)
        {
            throw new GasparException($"Service call failed with error: {response.Error.Title} - {response.Error.Detail}");
        }
        return response.Data;
    }

}