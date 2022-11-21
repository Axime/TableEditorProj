
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System;

namespace API
{
    static class HTTP
    {
        public static Uri baseUri = new("http://localhost:3000"); //"https://jsonplaceholder.typicode.com");
        //public static string uriRegistration = "api/auth.registration";
        //public static string uriAuth = "api/auth.login";
        public static readonly HttpClient client = new();

        private static string _userToken = "undefined";
        public static string userToken
        {
            get => _userToken;
            set => _userToken = value;
        }
        public record APIResponse<T>
        {
            public bool ok;
            public int? error;
            public string? errorDescription;
            public T? response;
        }
        public static async Task<APIResponse<Res>?> SendRequest<Req, Res>(string uriAddress, Req value)
            where Req : class
            where Res : class
        {
            //Console.WriteLine($"=================================>{uriAddress}<===================================");
            if (value == null) throw new ArgumentNullException(nameof(value));
            var response = await client.PostAsync(
                new Uri(baseUri, uriAddress),
                new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json")
            );
            string jsonString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeAnonymousType(jsonString, new APIResponse<Res>());

        }

    }
    static class Auth
    {
        public static class Registration
        {
            public record Request
            {
                public string username;
                public string password;
                public string passwordRepeat;
                public string keyword;
            }
            public record Response
            {
                public bool success;
            }
            public static async Task<HTTP.APIResponse<Response>?> Call(Request req)
            {
                return await HTTP.SendRequest<Request, Response>("api/auth.registration", req);
            }
        }
        public static class Login
        {
            public record Request
            {
                public string username;
                public string password;
            }
            public record Response
            {
                public string token;
                public byte accessType;
            }
            public static async Task<HTTP.APIResponse<Response>?> Call(Request req)
            {
                return await HTTP.SendRequest<Request, Response>("api/auth.login", req);
            }
        }
    }
}
