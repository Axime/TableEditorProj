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
    }

    void OpenField(Grid grid) {

      List<Grid> fields = new List<Grid>() { Left_TableField, Settings, PersonalClient };

      for (int i = 0; i < fields.Count; i++) { fields[i].Visibility = Visibility.Collapsed; }
      grid.Visibility = Visibility.Visible;

      if (ColumnFunctionsField.ActualWidth == 0) {
        ColumnFunctionsField.Width = new(270d);
        return;
      }
      ColumnFunctionsField.Width = new(0);
    }

    void BTableList(object sender, RoutedEventArgs e) => OpenField(Left_TableField);
    
    void BSettings(object sender, RoutedEventArgs e) => OpenField(Settings);
    
    void BPersonalClient(object sender, RoutedEventArgs e) => OpenField(PersonalClient);
  }
}
