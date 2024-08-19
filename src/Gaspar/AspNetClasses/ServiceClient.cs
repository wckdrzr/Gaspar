using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WCKDRZR.Gaspar.Models;

namespace WCKDRZR.Gaspar
{
    public static class ServiceClient
    {
        public static async Task<ServiceResponse> FetchVoidAsync(HttpMethod method, string url, dynamic body, Dictionary<string, string> headers, TimeSpan? timeout, Type logReceiver, Type serializer)
        {
            return await FetchAsync<VoidObject>(method, url, body, headers, timeout, logReceiver, serializer);
        }

        public static async Task<ServiceResponse<T>> FetchAsync<T>(HttpMethod method, string url, dynamic body, Dictionary<string, string> headers, TimeSpan? timeout, Type logReceiver, Type serializer)
        {
            try
            {
                HttpResponseMessage httpResponse = await Load(method, url, body, headers, timeout);
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

        private static async Task<HttpResponseMessage> Load(HttpMethod method, string url, dynamic body, Dictionary<string, string> headers, TimeSpan? timeout)
        {
            HttpClient httpClient = new();
            if (timeout != null)
            {
                httpClient.Timeout = (TimeSpan)timeout;
            }

            HttpRequestMessage request = new()
            {
                Method = method,
                RequestUri = new Uri(url),
                Content = body == null ? null : JsonContent.Create(body),
            };
            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            return await httpClient.SendAsync(request);
        }

        private static ServiceResponse<T> Success<T>(HttpResponseMessage httpResponse, Type serializer, string url, Type logReceiver)
        {
            try
            {
                if (serializer == null)
                {
                    string responseString = httpResponse.Content.ReadAsStringAsync().Result;
                    return new ServiceResponse<T>
                    {
                        Data = typeof(T) == typeof(string) ? (T)(object)responseString : JsonConvert.DeserializeObject<T>(responseString),
                        Error = null
                    };
                }
                else
                {
                    MethodInfo? deserializeMethod = serializer.GetMethod("Deserialize");
                    if (deserializeMethod != null)
                    {
                        MethodInfo deserializeMethodGeneric = deserializeMethod.MakeGenericMethod(typeof(T));
                        return new ServiceResponse<T>
                        {
                            Data = (T?)deserializeMethodGeneric.Invoke(serializer, new[] { httpResponse.Content.ReadAsStringAsync().Result }),
                            Error = null
                        };
                    }
                    else
                    {
                        throw new Exception($"Gaspar: Custom serializer ({serializer.Name}) has no 'Deserialize' method");
                    }
                }
            }
            catch (Exception e)
            {
                Log($"Gaspar: Unable to deserialize successful response from {url}\n{e.Message}", logReceiver);

                return Error<T>($"Gaspar: Unable to deserialize successful response from {url}", e.Message, 0);
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
                response = Error<T>($"Gaspar: Service call to {url} failed to connect", httpResponse.ReasonPhrase, (int)httpResponse.StatusCode);
            }

            Log($"Gaspar: Service call to {url} failed with status code {(int)httpResponse.StatusCode} - {httpResponse.StatusCode}" +
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

            Log($"Gaspar: Service call to {url} failed to connect\n{message}", logReceiver);

            return Error<T>($"Gaspar: Service call to {url} failed to connect", message, 0);
        }

        private static ServiceResponse<T> Error<T>(string title, string? detail, int status)
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
                    MethodInfo? logMethod = logReceiver.GetMethod("GasparError");
                    if (logMethod != null)
                    {
                        logMethod.Invoke(logReceiver, new[] { message });
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Gaspar: Cannot use your LoggingReceiver ({logReceiver.Name}): no GasparError method found.  Communication error below.");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Gaspar: Error trying to use your LoggingReceiver ({logReceiver.Name}): {e.Message}  Communication error below.");
            }
            Console.WriteLine("Gaspar: " + message);
        }
    }
}