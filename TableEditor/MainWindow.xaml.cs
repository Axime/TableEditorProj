﻿using System;
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

namespace SchoolProj1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
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

        void RegistrationUser(string login, string password, string keyWord)
        {
           
        }
    }
}