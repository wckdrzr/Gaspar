using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using CSharpExporter.AspNetCore.ServiceCommunciation.Models;

namespace CSharpExporter.AspNetCore.ServiceCommunciation
{
    public class ServiceResponse<T>
    {
        public T? Data { get; set; }
        public ActionResultError? Error { get; set; }

        public ServiceResponse(ServiceHttpMethod method, string url, dynamic body, Type? serializer = null)
        {
            try
            {
                HttpResponseMessage httpResponse = Load(method, url, body).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    Success(httpResponse, serializer, url);
                }
                else
                {
                    ServerError(httpResponse, url);
                }
            }
            catch (Exception e)
            {
                LoadException(e, url);
            }
        }

        private async Task<HttpResponseMessage> Load(ServiceHttpMethod method, string url, dynamic body)
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

        private void Success(HttpResponseMessage httpResponse, Type? serializer, string url)
        {
            try
            {
                if (serializer == null)
                {
                    Data = httpResponse.Content.ReadFromJsonAsync<T>().Result;
                }
                else
                {
                    MethodInfo? deserializeMethod = serializer.GetMethod("Deserialize");
                    if (deserializeMethod != null)
                    {
                        MethodInfo? deserializeMethodGeneric = deserializeMethod.MakeGenericMethod(typeof(T));
                        Data = (T?)deserializeMethodGeneric.Invoke(serializer, new[] { httpResponse.Content.ReadAsStringAsync().Result });
                    }
                    else
                    {
                        throw new Exception($"Custom serializer ({serializer.Name}) has no 'Deserialize' method");
                    }
                }
                Error = null;
            }
            catch (Exception e)
            {
                Data = default(T);
                Error = new() { Title = $"Unable to deserialize sucessful response from {url}", Detail = e.Message, Status = 0 };
                //LoggingV2.Log($"Unable to deserialize sucessful response from {url}", LogLevel.Error);
                //LoggingV2.Log($"{e.Message}", LogLevel.Info);
            }
        }

        private void ServerError(HttpResponseMessage httpResponse, string url)
        {
            Data = default(T);
            try
            {
                Error = httpResponse.Content.ReadFromJsonAsync<ActionResultError>().Result;
            }
            catch
            {
                Error = new() { Title = $"Service call to {url} failed to connect", Detail = httpResponse.ReasonPhrase, Status = (int)httpResponse.StatusCode };
            }

            //LoggingV2.Log($"Service call to {url} failed with status code {(int)httpResponse.StatusCode} - {httpResponse.StatusCode}", LogLevel.Error);
            if (Error != null && !string.IsNullOrEmpty(Error.Detail))
            {
                //LoggingV2.Log($"{Error.Detail}", LogLevel.Info);
            }
        }

        private void LoadException(Exception exception, string url)
        {
            string message = exception.Message;
            if (exception.InnerException is TimeoutException)
            {
                message = "Connection timed out";
            }

            Data = default(T);
            Error = new() { Title = $"Service call to {url} failed to connect", Detail = message, Status = 0 };

            //LoggingV2.Log($"Service call to {url} failed to connect", LogLevel.Error);
            //LoggingV2.Log($"{message}", LogLevel.Info);
        }
    }
}