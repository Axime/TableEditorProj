using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace TableEditor
{
    public static class GlobalInformation
    {
        //URI's
        public static Uri baseUri = new Uri("http://localhost:3000"); //"https://jsonplaceholder.typicode.com");
        public static string uriRegistration = "api/auth.registration";
        public static string uriAuth = "api/auth.login";
        public static readonly HttpClient client = new HttpClient();

        private static string _userToken = "undefined";
        public static string userToken { get { return _userToken; } set { _userToken = value; } }

        public static async Task<string> SendRequest(string uriAddress, string value)
        {
            Console.WriteLine($"=================================>{uriAddress}<===================================");
            if (value == null) return "Error";

            var response = await client.PostAsync(
                new Uri(baseUri, uriAddress),
                new StringContent(value, Encoding.UTF8, "application/json")
            );
            return await response.Content.ReadAsStringAsync();
        }

    }
}
