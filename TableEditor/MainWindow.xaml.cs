﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace Main {
  public partial class MainWindow : Window {
    public MainWindow() {
      InitializeComponent();
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
    async void RegistrationUser(API.Method.Auth.RegistrationModel.Request user) {

      try {
        var response = await API.Method.Call(API.Method.Auth.Registration, user);
        response.IfOk(res => {
          Console.WriteLine("Регистрация прошла успешно");
          Console.WriteLine("Загрузка...");
        }).IfError(err => {
          ErrorField.Visibility = Visibility.Visible;
          ErrorField.Text = "Неправильный логин или пароль";
        });
      } catch (Exception e) {
        Console.WriteLine("Error " + e.ToString());
      }


    }
    async void AuthUser(API.Method.Auth.LoginModel.Request data) {
      try {
        var response = await API.Method.Call(API.Method.Auth.Login, data);
        Console.WriteLine(response);
      } catch (Exception e) {
        Console.WriteLine(e.ToString());
      }
    }

    bool Check(TextBox field) {
      if (field.Text.Length < 6) {
        field.ToolTip = "Incorrect";
        field.Background = Brushes.IndianRed;
        return false;
      }
      field.ToolTip = "Correct";
      field.Background = Brushes.White;
      return true;
    }
    bool Check(PasswordBox field) => field.Password.Length >= 6;
    bool Check(PasswordBox field1, PasswordBox field2) {
      if (field1.Password.Length < 6 || field1.Password != field2.Password) {
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
