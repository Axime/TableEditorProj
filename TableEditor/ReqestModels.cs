using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReqestModels
{

    namespace Models
    {
        public struct RegistrationUserForm
        {
            public string username;
            public string passwordRepeat;
            public string password;
            public string keyword;

        }
        public struct AuthUserForm
        {
            public string username;
            public string password;

        }
    }
}
