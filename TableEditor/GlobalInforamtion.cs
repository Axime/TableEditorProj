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
        public static Uri uriRegistration = new Uri("https://ivanyudin.alwaysdata.net/api/auth.registration");
        public static Uri uriAuth = new Uri("https://ivanyudin.alwaysdata.net/api/auth.login");

        public static readonly HttpClient client = new HttpClient()
        {
            BaseAddress = new Uri("localhost"),
        };
        private static string _userToren = "undefined";
        public static string userToken {get { return _userToren; } set{ _userToren= value;} }

        public static async Task<string> SendRequest(Uri uriAdress, string value)
        {
            if (value == null) return "Error";
            var response = await client.PostAsync(uriAdress, new StringContent(value, Encoding.UTF8, "application/json"));
            return await response.Content.ReadAsStringAsync();
        }

    }
}
