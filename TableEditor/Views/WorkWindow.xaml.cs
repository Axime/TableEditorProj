using Main;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using TableEditor.VM;

namespace TableEditor {
  public partial class WorkWindow : Window {
    public WorkWindow() {
      InitializeComponent();
      vm = WorkWindowViewModel.Instance;
    }
    public WorkWindowViewModel vm;
    private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) {
      Debug.WriteLine("Editing ended");
      var row = e.Row.GetIndex();
      var column = e.Column.DisplayIndex;
      var content = (e.EditingElement as TextBox)!.Text;
      Debug.WriteLine($"Строка: {row}");
      Debug.WriteLine($"Столбец: {column}");
      Debug.WriteLine($"Ячейка {(e.EditAction == DataGridEditAction.Cancel ? "не " : "")}была отредактирована");
      Debug.WriteLine($"Значение:{content}");
      if (e.EditAction == DataGridEditAction.Commit) {
        vm.UpdateModel(row, column, content);
      }
    }
    #region интерфейс
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

    private void ListBox_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e) {

    }


    private void DataGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e) {
      Debug.WriteLine("Editing started");
      Debug.WriteLine($"Строка: {e.Row.GetIndex()}");
      Debug.WriteLine($"Столбец: {e.Column.DisplayIndex}"); ;
    }

    private void TabContr_Loaded(object sender, RoutedEventArgs e) {
      TabContr.GetBindingExpression(TabControl.ItemsSourceProperty).UpdateTarget();
    }
    #endregion

    private void Button_Click(object sender, RoutedEventArgs e) {
      string[] userData = File.ReadAllText("User/nickname.txt").Split(' ');
      File.WriteAllText("User/nickname.txt", userData[0]);
      var login = new MainWindow();
      login.Show();
      this.Close();
    }
  }
}