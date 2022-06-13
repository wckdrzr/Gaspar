using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using WCKDRZR.CSharpExporter.Models;

namespace WCKDRZR.CSharpExporter
{
    public static class ServiceClient
    {
        public static ServiceResponse<T> Fetch<T>(ServiceHttpMethod method, string url, dynamic body, Type serializer = null)
        {
            return FetchAsync<T>(method, url, body, serializer).Result;
        }

        public static async Task<ServiceResponse<T>> FetchAsync<T>(ServiceHttpMethod method, string url, dynamic body, Type serializer = null)
        {
            try
            {
                HttpResponseMessage httpResponse = await Load(method, url, body);
                if (httpResponse.IsSuccessStatusCode)
                {
                    return Success<T>(httpResponse, serializer, url);
                }
                else
                {
                    return ServerError<T>(httpResponse, url);
                }
            }
            catch (Exception e)
            {
                return LoadException<T>(e, url);
            }
        }

        private static async Task<HttpResponseMessage> Load(ServiceHttpMethod method, string url, dynamic body)
        {
            HttpClient httpClient = new();

            switch (method)
            {
                case ServiceHttpMethod.Get:
                    return await httpClient.GetAsync(url);
                case ServiceHttpMethod.Post:
                    return await httpClient.PostAsync(url, JsonContent.Create(body));
                case ServiceHttpMethod.Put:
                    return await httpClient.PutAsync(url, JsonContent.Create(body));
                case ServiceHttpMethod.Delete:
                    return await httpClient.DeleteAsync(url, JsonContent.Create(body));
                default:
                    throw new NotImplementedException();
            }
        }

        private static ServiceResponse<T> Success<T>(HttpResponseMessage httpResponse, Type serializer, string url)
        {
            try
            {
                if (serializer == null)
                {
                    return new ServiceResponse<T>
                    {
                        Data = httpResponse.Content.ReadFromJsonAsync<T>().Result,
                        Error = null
                    };
                }
                else
                {
                    MethodInfo deserializeMethod = serializer.GetMethod("Deserialize");
                    if (deserializeMethod != null)
                    {
                        MethodInfo deserializeMethodGeneric = deserializeMethod.MakeGenericMethod(typeof(T));
                        return new ServiceResponse<T>
                        {
                            Data = (T)deserializeMethodGeneric.Invoke(serializer, new[] { httpResponse.Content.ReadAsStringAsync().Result }),
                            Error = null
                        };
                    }
                    else
                    {
                        throw new Exception($"Custom serializer ({serializer.Name}) has no 'Deserialize' method");
                    }
                }
            }
            catch (Exception e)
            {
                //LoggingV2.Log($"Unable to deserialize sucessful response from {url}", LogLevel.Error);
                //LoggingV2.Log($"{e.Message}", LogLevel.Info);

                return Error<T>($"Unable to deserialize sucessful response from {url}", e.Message, 0);
            }
        }

        private static ServiceResponse<T> ServerError<T>(HttpResponseMessage httpResponse, string url)
        {
            ServiceResponse<T> response = new ServiceResponse<T> { Data = default(T) };
            try
            {
                response.Error = httpResponse.Content.ReadFromJsonAsync<ActionResultError>().Result;
            }
            catch
            {
                response = Error<T>($"Service call to {url} failed to connect", httpResponse.ReasonPhrase, (int)httpResponse.StatusCode);
            }

            //LoggingV2.Log($"Service call to {url} failed with status code {(int)httpResponse.StatusCode} - {httpResponse.StatusCode}", LogLevel.Error);
            if (response.Error != null && !string.IsNullOrEmpty(response.Error.Detail))
            {
                //LoggingV2.Log($"{Error.Detail}", LogLevel.Info);
            }

            return response;
        }

        private static ServiceResponse<T> LoadException<T>(Exception exception, string url)
        {
            string message = exception.Message;
            if (exception.InnerException is TimeoutException)
            {
                message = "Connection timed out";
            }

            //LoggingV2.Log($"Service call to {url} failed to connect", LogLevel.Error);
            //LoggingV2.Log($"{message}", LogLevel.Info);

            return Error<T>($"Service call to {url} failed to connect", message, 0);
        }

        private static ServiceResponse<T> Error<T>(string title, string detail, int status)
        {
            return new()
            {
                Data = default(T),
                Error = new() { Title = title, Detail = detail, Status = status }
            };
        }
    }
}