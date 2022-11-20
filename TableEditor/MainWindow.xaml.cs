using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Http;
using Newtonsoft.Json;
using System.Windows.Media.Animation;
using Newtonsoft.Json.Linq;
using Microsoft.VisualBasic;
using TableEditor;

namespace SchoolProj1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        void Button_Reg_Click(object sender, RoutedEventArgs e)
        {
            string login = TextBoxLogin.Text.Trim();
            string password = PasswordBox.Password.Trim();
            string repeatPassword = RepeatPasswordBox.Password.Trim();
            string keyWord = TextBoxKeyWord.Text.ToLower().Trim();

            if (Check(TextBoxLogin) && Check(PasswordBox, RepeatPasswordBox) && Check(TextBoxKeyWord)) RegistrationUser(login, password, keyWord);

        }
        void Button_Auth_Click(object sender, RoutedEventArgs e)
        {
            if (Check(LoginField) && Check(PasswordField)) AuthUser(LoginField.Text, PasswordField.Password);
        }
        void Button_Reg_Form_Click(object sender, RoutedEventArgs e)
        {
            if (AuthForm.Visibility == Visibility.Visible) AuthForm.Visibility = Visibility.Collapsed;
            RegistrationForm.Visibility = Visibility.Visible;
            
        }
        void Button_Auth_Form_Click(object sender, RoutedEventArgs e)
        {
            if (RegistrationForm.Visibility == Visibility.Visible) RegistrationForm.Visibility = Visibility.Collapsed;
            AuthForm.Visibility = Visibility.Visible;
        }
        void RegistrationUser(string login, string password, string keyWord)
        {
            RegistrationUserForm user = new()
            {
                username = login,
                password = password,
                keyword = keyWord,
            };

            string information = JsonConvert.SerializeObject(user, Formatting.Indented);

            string response = SendContent(GlobalInforamtion.uriRegistration,information).Result.ToString();
            Console.WriteLine(response);

        }
        void AuthUser(string login, string password)
        {
            AuthUserForm user = new()
            {
                username = login,
                password = password,
            };
            string information = JsonConvert.SerializeObject(user, Formatting.Indented);

            string response = SendContent(GlobalInforamtion.uriAuth, information).Result.ToString();
            Console.WriteLine(response);
        }
        void GoToWork()
        {
            Console.WriteLine("Validate user");
        }
        async Task<string> SendContent(Uri uriAdress,string value)
        {
            if (value == null) return"Error";
            var response = await GlobalInforamtion.client.PostAsync(uriAdress, new StringContent(value, Encoding.UTF8, "application/json"));
            return await response.Content.ReadAsStringAsync();
        }
        

        bool Check(TextBox field)
        {
            if (field.Text.Length < 6)
            {
                field.ToolTip = "Uncorrect";
                field.Background = Brushes.IndianRed;
                return false;
            }
            field.ToolTip = "Correct";
            field.Background = Brushes.White;
            return true;
        }
        bool Check(PasswordBox field)
        {
            string login = LoginField.Text.Trim();

            if (field.Password.Length < 6) return false;
            return true;
        }
        bool Check(PasswordBox field1, PasswordBox field2)
        {
            if (field1.Password.Length < 6 || field1.Password != field2.Password)
            {
                field1.ToolTip = "Uncorrect";
                field1.Background = Brushes.IndianRed;
                return false;
            }
            field1.ToolTip = "Correct";
            field1.Background = Brushes.White;
            return true;
        }


        class RegistrationUserForm
        {
            public string username { get; set; }
            public string password { get; set; }
            public string keyword { get; set; }
        }
        class AuthUserForm
        {
            public string username { get; set; }
            public string password { get; set; }
        }
    }
}
