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
using System.Windows.Shapes;

namespace TableEditor {
  public partial class WorkWindow : Window {
    public WorkWindow() {
      InitializeComponent();
      userNickname.Content = API.HTTP.UserNickname;
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

    

    void BCreateTable(object sender, RoutedEventArgs e) {
      var dg = new DataGrid();
      dg.AutoGenerateColumns = true;
      dg.CanUserAddRows = true;
      dg.CanUserDeleteRows = true;
      dg.CanUserResizeColumns= true;
      dg.GridLinesVisibility = DataGridGridLinesVisibility.All;
      
      for (int i = 0; i < 30; i++) {
        dg.Columns.Insert(dg.Columns.Count, new DataGridTextColumn { Header = $"Колонка{i}" });
        dg.Items.Add(new DataGrid());
      }

      DataGridTabControll.Items.Add(new TabItem { Header = new TextBlock { Text = (DataGridTabControll.Items.Count+1).ToString() }, Content = dg});
    }
  }
}