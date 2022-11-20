using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TableEditor
{
    public static class GlobalInforamtion
    {
        //URI's
        public static Uri uriDefault = new Uri("localhost");
        public static string uriRegistration = "api/auth.registration";
        public static string uriAuth = "api/auth.login";

        public static readonly HttpClient client = new HttpClient()
        {
            BaseAddress = new Uri("https://localhost"),
        };
        private static string _userToken = "undefined";
        public static string userToken {get { return _userToken; } set{ _userToken= value;} }

        public static async Task<string> SendRequest(string uriAdress, string value)
        {
            Console.WriteLine($"=================================>{uriAdress}<===================================");
            if (value == null) return "Error";
            var response = await client.PostAsync(uriAdress, new StringContent(value, Encoding.UTF8, "application/json"));
            return await response.Content.ReadAsStringAsync();
        }

    }
}
