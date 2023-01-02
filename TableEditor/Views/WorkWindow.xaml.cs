using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using TableEditor.Models;
using TableEditor;

namespace TableEditor {
  public partial class WorkWindow : Window {
    private float _funcFieldWeight;
    public WorkWindow() {
      InitializeComponent();
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

    public void BCreateTable(object sender, RoutedEventArgs e) {
      if (FieldLastFilesTutorialsOther.Visibility == Visibility.Visible) {
        FieldLastFilesTutorialsOther.Visibility = Visibility.Collapsed;
        TableMainGrid.Visibility = Visibility.Visible;
        return;
      }
    }
  }
}