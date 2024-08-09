using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;
using WCKDRZR.Gaspar.Models;

namespace WCKDRZR.Gaspar
{
    public static class GasparService
    {
        public static bool TryGet<T>(string url, out ServiceResponse<T> response) => TryGet(url, new(), out response, out _);
        public static bool TryGet<T>(string url, GasparServiceOptions options, out ServiceResponse<T> response) => TryGet(url, options, out response, out _);
        public static bool TryGet<T>(string url, out ServiceResponse<T> response, [NotNullWhen(true)] out T? data) => TryGet(url, new(), out response, out data);
        public static bool TryGet<T>(string url, GasparServiceOptions options, out ServiceResponse<T> response, [NotNullWhen(true)] out T? data)
        {
            response = GetAsync<T>(url, options).Result;
            data = response.Data;
            return response.Success;
        }
        public static ServiceResponse<VoidObject> Get(string url, GasparServiceOptions? options = null)
            => GetAsync<VoidObject>(url, options).Result;
        public static async Task<ServiceResponse<VoidObject>> GetAsync(string url, GasparServiceOptions? options = null)
            => await GetAsync<VoidObject>(url, options);
        public static ServiceResponse<T> Get<T>(string url, GasparServiceOptions? options = null)
            => GetAsync<T>(url, options).Result;
        public static async Task<ServiceResponse<T>> GetAsync<T>(string url, GasparServiceOptions? options = null)
            => await FetchAsync<T>(HttpMethod.Get, url, options);

        public static bool TryPost<T>(string url, out ServiceResponse<T> response) => TryPost(url, new(), out response, out _);
        public static bool TryPost<T>(string url, GasparServiceOptions options, out ServiceResponse<T> response) => TryPost(url, options, out response, out _);
        public static bool TryPost<T>(string url, out ServiceResponse<T> response, [NotNullWhen(true)] out T? data) => TryPost(url, new(), out response, out data);
        public static bool TryPost<T>(string url, GasparServiceOptions options, out ServiceResponse<T> response, [NotNullWhen(true)] out T? data)
        {
            response = PostAsync<T>(url, options).Result;
            data = response.Data;
            return response.Success;
        }
        public static ServiceResponse<VoidObject> Post(string url, GasparServiceOptions? options = null)
            => PostAsync<VoidObject>(url, options).Result;
        public static async Task<ServiceResponse<VoidObject>> PostAsync(string url, GasparServiceOptions? options = null)
            => await PostAsync<VoidObject>(url, options);
        public static ServiceResponse<T> Post<T>(string url, GasparServiceOptions? options = null)
            => PostAsync<T>(url, options).Result;
        public static async Task<ServiceResponse<T>> PostAsync<T>(string url, GasparServiceOptions? options = null)
            => await FetchAsync<T>(HttpMethod.Post, url, options);

        public static bool TryPut<T>(string url, out ServiceResponse<T> response) => TryPut(url, new(), out response, out _);
        public static bool TryPut<T>(string url, GasparServiceOptions options, out ServiceResponse<T> response) => TryPut(url, options, out response, out _);
        public static bool TryPut<T>(string url, out ServiceResponse<T> response, [NotNullWhen(true)] out T? data) => TryPut(url, new(), out response, out data);
        public static bool TryPut<T>(string url, GasparServiceOptions options, out ServiceResponse<T> response, [NotNullWhen(true)] out T? data)
        {
            response = PutAsync<T>(url, options).Result;
            data = response.Data;
            return response.Success;
        }
        public static ServiceResponse<VoidObject> Put(string url, GasparServiceOptions? options = null)
            => PutAsync<VoidObject>(url, options).Result;
        public static async Task<ServiceResponse<VoidObject>> PutAsync(string url, GasparServiceOptions? options = null)
            => await PutAsync<VoidObject>(url, options);
        public static ServiceResponse<T> Put<T>(string url, GasparServiceOptions? options = null)
            => PutAsync<T>(url, options).Result;
        public static async Task<ServiceResponse<T>> PutAsync<T>(string url, GasparServiceOptions? options = null)
            => await FetchAsync<T>(HttpMethod.Put, url, options);

        public static bool TryDelete<T>(string url, out ServiceResponse<T> response) => TryDelete(url, new(), out response, out _);
        public static bool TryDelete<T>(string url, GasparServiceOptions options, out ServiceResponse<T> response) => TryDelete(url, options, out response, out _);
        public static bool TryDelete<T>(string url, out ServiceResponse<T> response, [NotNullWhen(true)] out T? data) => TryDelete(url, new(), out response, out data);
        public static bool TryDelete<T>(string url, GasparServiceOptions options, out ServiceResponse<T> response, [NotNullWhen(true)] out T? data)
        {
            response = DeleteAsync<T>(url, options).Result;
            data = response.Data;
            return response.Success;
        }
        public static ServiceResponse<VoidObject> Delete(string url, GasparServiceOptions? options = null)
            => DeleteAsync<VoidObject>(url, options).Result;
        public static async Task<ServiceResponse<VoidObject>> DeleteAsync(string url, GasparServiceOptions? options = null)
            => await DeleteAsync<VoidObject>(url, options);
        public static ServiceResponse<T> Delete<T>(string url, GasparServiceOptions? options = null)
            => DeleteAsync<T>(url, options).Result;
        public static async Task<ServiceResponse<T>> DeleteAsync<T>(string url, GasparServiceOptions? options = null)
            => await FetchAsync<T>(HttpMethod.Delete, url, options);

        public static ServiceResponse<T> Fetch<T>(HttpMethod method, string url, GasparServiceOptions? options = null)
            => FetchAsync<T>(method, url, options).Result;

        public static async Task<ServiceResponse<T>> FetchAsync<T>(HttpMethod method, string url, GasparServiceOptions? options = null)
        {
            options ??= new();
            return await ServiceClient.FetchAsync<T>(method, url, options.Body, options.Headers, options.Timeout, options.LogReceiver, options.Serializer);
        }
    }

    public class GasparServiceOptions
    {
        public dynamic? Body { get; set; } = null;
        public Dictionary<string, string> Headers { get; set; } = new();
        public TimeSpan? Timeout { get; set; } = null;
        public Type? LogReceiver { get; set; } = null;
        public Type? Serializer { get; set; } = null;
    }
}