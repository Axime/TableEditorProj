using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TableEditor.Models;
using TableEditor.ViewModels;

namespace TableEditor
{
    public partial class WorkWindow : Window {
    public WorkWindow() {
      InitializeComponent();
      userNickname.Content = HTTP.UserNickname;
      status_bar.Content = "develop moment";
    }

    void ToggleField(ref StackPanel grid) {

      List<StackPanel> fields = new() { TableList, Settings, PersonalClient };
      bool isAlreadyShown = grid.Visibility == Visibility.Visible;
      for (int i = 0; i < fields.Count; i++) { fields[i].Visibility = Visibility.Collapsed; }
      if (!isAlreadyShown) {
        grid.Visibility = Visibility.Visible;
        FuncFieldBody.Width = 250;
        return;
      }
      FuncFieldBody.Width = 0;
    }

    void BTableList(object sender, RoutedEventArgs e) => ToggleField(ref TableList);

    void BSettings(object sender, RoutedEventArgs e) => ToggleField(ref Settings);

    void BPersonalClient(object sender, RoutedEventArgs e) => ToggleField(ref PersonalClient);


    public class MyData {
      public List<string> DATA = new();
    }

    public void BCreateTable(object sender, RoutedEventArgs e) {
      if (FieldLastFilesTutorialsOther.Visibility == Visibility.Visible) {
        FieldLastFilesTutorialsOther.Visibility = Visibility.Collapsed; 
        TableMainGrid.Visibility = Visibility.Visible;
        return;
      }

      DataGrid dg = new() { 
        AutoGenerateColumns = true, 
        CanUserAddRows = true, 
        CanUserDeleteRows = true, 
        CanUserReorderColumns = true, 
        CanUserResizeColumns = true, 
        GridLinesVisibility = DataGridGridLinesVisibility.All 
      };



      List<MyData> myDataItems= new List<MyData>();

      for (int i = 0; i < 20; i++) {
        myDataItems.Add(new MyData());
      }

      for (int i = 0; i < 20; i++) {
        DataGridTextColumn col = new() { Header = $"Колонка {i}"};
        col.Binding = new Binding() {Mode = BindingMode.TwoWay, IsAsync = true};
        
        dg.Columns.Insert(dg.Columns.Count, col);
      }


      dg.ItemsSource = myDataItems;
      
      
      DataGridTabControll.Items.Add(new TabItem { Header = new TextBlock { Text = (DataGridTabControll.Items.Count+1).ToString() }, Content = dg});
    }

    
  }
}