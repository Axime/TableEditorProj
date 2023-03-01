using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using TableEditor;
using TableEditor.Models;
using TableEditor.VM;

namespace Main {
  public partial class MainWindow : Window {
    public MainWindow() {
      InitializeComponent();
      //WorkWindowViewModel MVVM = new();
      string[] userData = File.ReadAllText("User/nickname.txt").Split(' ');
      if (userData.Length == 1) return;
      if (userData[1] == "true") {
        WorkWindow workWindow = new WorkWindow();
        workWindow.Show();
        this.Close();
      }
    }

    void Button_Reg_Click(object sender, RoutedEventArgs e) {
      if (Check(TextBoxLogin) && Check(PasswordBox, RepeatPasswordBox) && Check(TextBoxKeyword))
        RegistrationUser(new() {
          username = TextBoxLogin.Text.Trim(),
          password = PasswordBox.Password.Trim(),
          passwordRepeat = RepeatPasswordBox.Password.Trim(),
          keyword = TextBoxKeyword.Text.ToLower().Trim(),
        });
    }


    void Button_Auth_Click(object sender, RoutedEventArgs e) {
      if (Check(LoginField) && Check(PasswordField)) AuthUser(new() {
        username = LoginField.Text,
        password = PasswordField.Password
      });
    }


    void Button_Reg_Form_Click(object sender, RoutedEventArgs e) {
      if (AuthForm.Visibility == Visibility.Visible) AuthForm.Visibility = Visibility.Collapsed;
      RegistrationForm.Visibility = Visibility.Visible;
    }


    void Button_Auth_Form_Click(object sender, RoutedEventArgs e) {
      if (RegistrationForm.Visibility == Visibility.Visible) RegistrationForm.Visibility = Visibility.Collapsed;
      if (RegistrationForm.Visibility == Visibility.Visible) RegistrationForm.Visibility = Visibility.Collapsed;
      AuthForm.Visibility = Visibility.Visible;
    }


    async void RegistrationUser(Method.Auth.RegistrationModel.Request user) {

      try {
#if NO_SERVER
        Method.MethodResponse<
          HTTP.APIResponse<Method.Auth.RegistrationModel.Response>,
          Method.Auth.RegistrationModel.Response
        > response = new(new() { error = null, errorDescription = null, ok = true, response = new() { success = true} });
#else
        var response = await Method.Call(Method.Auth.Registration, user);
#endif
        response.IfOk(res => {
          ErrorField.Text = "Регистрация прошла успешно!";
        }).IfError(err => {
          ErrorField.Visibility = Visibility.Visible;
          ErrorField.Text = "Неправильный логин или пароль";
        });
      } catch (Exception e) {
        Console.WriteLine("Error " + e.ToString());
      }
    }


    async void AuthUser(Method.Auth.LoginModel.Request data) {
      try {
#if NO_SERVER
        Method.MethodResponse<
          HTTP.APIResponse<Method.Auth.LoginModel.Response>,
          Method.Auth.LoginModel.Response
        > response = new(new() {error = null, errorDescription = null, ok = true, response = new() { accessType = 0, token = ""} });
#else
        var response = await Method.Call(Method.Auth.Login, data);
#endif
        Console.WriteLine(response);
        response.IfOk(res => {
          HTTP.UserNickname = LoginField.Text;
          File.WriteAllText("User/nickname.txt", LoginField.Text);
          if (RememberAuth.IsChecked == true) {
            File.WriteAllText("User/nickname.txt", LoginField.Text + " true");
          }
          WorkWindow workWindow = new WorkWindow();
          workWindow.Show();
          this.Close();
        }).IfError(err => {
          MessageBox.Show("Неправильный логин или пароль!", "Ошибка");
        });
        
      } catch (Exception e) {
        Console.WriteLine(e.ToString());
        MessageBox.Show("Неправильный логин или пароль!","Ошибка");
        ErrorField.Text = "Неправильный логин или пароль";
      }
    }


    bool Check(TextBox field) {
      if (field.Text.Length < 6) {
        field.ToolTip = "Incorrect";
        return false;
      }
      field.ToolTip = "Correct";
      return true;
    }


    bool Check(PasswordBox field) => field.Password.Length >= 6;


    bool Check(PasswordBox field1, PasswordBox field2) {
      if (field1.Password.Length < 6 || field1.Password != field2.Password) {
        field1.ToolTip = "Incorrect";
        return false;
      }
      field1.ToolTip = "Correct";
      return true;
    }
  }
}
