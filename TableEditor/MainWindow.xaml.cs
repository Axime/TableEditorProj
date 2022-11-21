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
using ReqestModels.Models;
using System.Threading;
using MaterialDesignThemes.Wpf;

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
            RegistrationUserForm a = new()
            {
                /*string */
                username = TextBoxLogin.Text.Trim(),
                /*string */
                password = PasswordBox.Password.Trim(),
                /*string */
                passwordRepeat = RepeatPasswordBox.Password.Trim(),
                /*string */
                keyword = TextBoxKeyWord.Text.ToLower().Trim(),
            };

            if (Check(TextBoxLogin) && Check(PasswordBox, RepeatPasswordBox) && Check(TextBoxKeyWord)) RegistrationUser(a);

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
        async void RegistrationUser(RegistrationUserForm user)
        {
            string information = JsonConvert.SerializeObject(user, Formatting.Indented);
            try
            {
                var response = await GlobalInformation.SendRequest(GlobalInformation.uriRegistration, information);
                Console.WriteLine(response);

            }
            catch (Exception e)
            {
                Console.WriteLine("Error " + e.ToString());
            }


        }
        async void AuthUser(string login, string password)
        {
            AuthUserForm user = new()
            {
                username = login,
                password = password,
            };
            string information = JsonConvert.SerializeObject(user, Formatting.Indented);


            try
            {
                string response = await GlobalInformation.SendRequest(GlobalInformation.uriAuth, information);
                Console.WriteLine(response);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        void GoToWork()
        {
            Console.WriteLine("Validate user");
        }


        bool Check(TextBox field)
        {
            if (field.Text.Length < 6)
            {
                field.ToolTip = "Incorrect";
                field.Background = Brushes.IndianRed;
                return false;
            }
            field.ToolTip = "Correct";
            field.Background = Brushes.White;
            return true;
        }
        bool Check(PasswordBox field)
        {
            //string login = LoginField.Text.Trim();
            return field.Password.Length >= 6;
        }
        bool Check(PasswordBox field1, PasswordBox field2)
        {
            if (field1.Password.Length < 6 || field1.Password != field2.Password)
            {
                field1.ToolTip = "Incorrect";
                field1.Background = Brushes.IndianRed;
                return false;
            }
            field1.ToolTip = "Correct";
            field1.Background = Brushes.White;
            return true;
        }
    }
}
