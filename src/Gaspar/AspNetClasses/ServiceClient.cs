using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using WCKDRZR.Gaspar.Models;

namespace WCKDRZR.Gaspar
{
    public static class ServiceClient
    {
        public static async Task<ServiceResponse<T>> FetchAsync<T>(ServiceHttpMethod method, string url, dynamic body, Type logReceiver, Type serializer)
        {
            try
            {
                HttpResponseMessage httpResponse = await Load(method, url, body);
                if (httpResponse.IsSuccessStatusCode)
                {
                    return Success<T>(httpResponse, serializer, url, logReceiver);
                }
                else
                {
                    return ServerError<T>(httpResponse, url, logReceiver);
                }
            }
            catch (Exception e)
            {
                return LoadException<T>(e, url, logReceiver);
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

        private static ServiceResponse<T> Success<T>(HttpResponseMessage httpResponse, Type serializer, string url, Type logReceiver)
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
                Log($"Unable to deserialize sucessful response from {url}\n{e.Message}", logReceiver);

                return Error<T>($"Unable to deserialize sucessful response from {url}", e.Message, 0);
            }
        }

        private static ServiceResponse<T> ServerError<T>(HttpResponseMessage httpResponse, string url, Type logReceiver)
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

            Log($"Service call to {url} failed with status code {(int)httpResponse.StatusCode} - {httpResponse.StatusCode}" +
                $"{(response.Error != null && !string.IsNullOrEmpty(response.Error.Detail) ? $"\n{response.Error.Detail}" : "")}", logReceiver);

            return response;
        }

        private static ServiceResponse<T> LoadException<T>(Exception exception, string url, Type logReceiver)
        {
            string message = exception.Message;
            if (exception.InnerException is TimeoutException)
            {
                message = "Connection timed out";
            }

            Log($"Service call to {url} failed to connect\n{message}", logReceiver);

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

        private static void Log(string message, Type logReceiver)
        {
            try
            {
                if (logReceiver != null)
                {
                    MethodInfo logMethod = logReceiver.GetMethod("GasparError");
                    if (logMethod != null)
                    {
                        logMethod.Invoke(logReceiver, new[] { message });
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Cannot use your LoggingReceiver ({logReceiver.Name}): no GasparError method found.  Communicaiton error below.");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error trying to use your LoggingReceiver ({logReceiver.Name}): {e.Message}  Communicaiton error below.");
            }
            Console.WriteLine("Gaspar: " + message);
        }
    }
}