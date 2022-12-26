using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System;

namespace TableEditor.Models
{
    public static class HTTP
    {
        public readonly static Uri baseUri = new("http://localhost:3000"); //"https://jsonplaceholder.typicode.com");
        private static readonly HttpClient client = new();
        private static string _userToken = "undefined";
        public static string UserToken
        {
            get => _userToken;
            set => _userToken = value;
        }
        private static string _userNickname = "undefined";
        public static string UserNickname
        {
            get => _userNickname;
            set => _userNickname = value;
        }


        public record APIResponse<T>
        {
            public bool ok;
            public uint? error;
            public string? errorDescription;
            public T? response;
        }
        public static async Task<APIResponse<Res>?> SendAPIRequest<Req, Res>(string uriAddress, Req value)
            where Req : class
            where Res : class
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var response = await client.PostAsync(
                new Uri(baseUri, uriAddress),
                new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json")
            );
            string jsonString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeAnonymousType(jsonString, new APIResponse<Res>());

        }

    }

    public static class Method
    {
        public interface IAPIRequestModel<Req, Res>
        where Req : class, new()
        where Res : class, new()
        {
            public Req Request { get => new(); }
            public Res Response { get => new(); }
            public string Uri { get; }
        }
        public async static Task<MethodResponse<HTTP.APIResponse<Res>, Res>> Call<
          Req, Res
        >(IAPIRequestModel<Req, Res> cl, Req value)
          where Req : class, new()
          where Res : class, new()
          => new MethodResponse<HTTP.APIResponse<Res>, Res>(await HTTP.SendAPIRequest<Req, Res>(cl.Uri, value));
        public class MethodResponse<Res, T> where Res : HTTP.APIResponse<T>
        {
            private readonly Res? result;
            public MethodResponse(Res? result)
            {
                this.result = result;
            }
            public record Error
            {
                public uint Code;
                public string Description;
            }
            public bool Ok => result != null && result.ok;
            public MethodResponse<Res, T> IfOk(Action<T> cb)
            {
                if (Ok) cb(result!.response!);
                return this;
            }

            public MethodResponse<Res, T> IfError(Action<Error> cb)
            {
                if (result != null && !Ok) cb(new()
                {
                    Code = (uint)result.error!,
                    Description = result.errorDescription!,
                });
                return this;
            }
            public MethodResponse<Res, T> IfHTTPError(Action cb)
            {
                if (result == null) cb();
                return this;
            }
        }

        public static class Auth
        {
            public static readonly RegistrationModel Registration = new();
            public class RegistrationModel : IAPIRequestModel<RegistrationModel.Request, RegistrationModel.Response>
            {
                public class Request
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
                public string Uri => "api/auth.registration";

                Request IAPIRequestModel<Request, Response>.Request => new();
                Response IAPIRequestModel<Request, Response>.Response => new();
            }
            public static readonly LoginModel Login = new();
            public class LoginModel : IAPIRequestModel<LoginModel.Request, LoginModel.Response>
            {
                string IAPIRequestModel<Request, Response>.Uri => "api/auth.login";
                public record Request
                {
                    public string username;
                    public string password;
                }
                public record Response
                {
                    public string token;
                    public uint accessType;
                }
            }
        }
    }
}
