using System.Collections.Generic;
using System.Data;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TableEditor.Models;
using TableEditor.Views;
using TableEditor;
using System.Windows;

namespace TableEditor {
  partial class WorkWindowViewModel : NotifyPropertyChanged {
    public WorkWindowViewModel() {
      Username = HTTP.UserNickname;
    }

    private ICommand _createTableCommand;

    private List<TabItem> _tabControl = new();
    private string _userName;


    public List<TabItem> TableListControl {
      get {
        return _tabControl;
      }
      set {
        _tabControl = value; OnPropertyChanged();
      }
    }
    public string Username //привязка никнейма
    {
      get { return _userName; }
      set { _userName = value; OnPropertyChanged(); }
    }


    public ICommand CreateTableCommand => _createTableCommand ?? (_createTableCommand = new RelayCommand(parameter => { CreateTable(); }));

    private void CreateTable() {
      BaseWorkField DataTable = new BaseWorkField("gg");
      DataGrid DG = new();

      Binding bind = new Binding("Data"); bind.Mode = BindingMode.TwoWay;
      bind.Source = DataTable;

      BindingOperations.SetBinding(DG, DataGrid.ItemsSourceProperty, bind);
      
      TableListControl.Add(new TabItem { Header = new TextBlock { Text = DataTable.TableName }, Content = DG });

    }


    public class BaseWorkField : NotifyPropertyChanged {

      private ICommand _addColumnCommand;
      private ICommand _addRowCommand;
      private ICommand _removeColumnCommand;
      private ICommand _removeRowCommand;


      private DataTable _dataTable = new();
      private int _rowCount;
      private int _columnCount;
      private string _tableName;


      public DataTable Data {
        get { return _dataTable; }
        set { _dataTable = value; OnPropertyChanged(); }
      }  //контейнер таблицы
      public int RowCount {
        get { return _rowCount; }
        set { _rowCount = value; OnPropertyChanged(); }
      } //привязка колличества строк
      public int ColumnCount {
        get { return _columnCount; }
        set { _columnCount = value; OnPropertyChanged(); }
      } //привязка колличества столбцов
      public string TableName {
        get { return _tableName; }
        set { _tableName = value; OnPropertyChanged(); }
      }


      private void AddColumn(int num) {
        for (int i = 0; i <= num; i++) {
          _dataTable.Columns.Add(new DataColumn(_dataTable.Columns.Count.ToString(), typeof(string)) {
            AllowDBNull = true
          });
        }
        _dataTable = _dataTable.Copy();
        ColumnCount = _dataTable.Columns.Count;
      }
      private void AddRow(int num) {
        for (int i = 0; i <= num; i++) _dataTable.Rows.Add();
        RowCount = _dataTable.Rows.Count;
      }
      private void RemoveColumn(int num) {
        for (int i = 1; i <= num && _dataTable.Columns.Count > 1; i++)
          _dataTable.Columns.RemoveAt(_dataTable.Columns.Count - 1);
        Data = _dataTable.Copy();
        ColumnCount = _dataTable.Columns.Count;
      }
      private void RemoveRow(int num) {
        for (int i = 1; i <= num && _dataTable.Rows.Count > 1; i++)
          _dataTable.Rows.RemoveAt(_dataTable.Rows.Count - 1);
        RowCount = _dataTable.Rows.Count;
      }



      public ICommand AddColumnCommand => _addColumnCommand ?? (_addColumnCommand = new RelayCommand(parameter => { AddColumn(1); }));
      public ICommand AddRowCommand => _addRowCommand ?? (_addRowCommand = new RelayCommand(parameter => { AddRow(1); }));
      public ICommand RemoveColumnCommand => _removeColumnCommand ?? (_removeColumnCommand = new RelayCommand(parameter => { RemoveColumn(1); }));
      public ICommand RemoveRowCommand => _removeRowCommand ?? (_removeRowCommand = new RelayCommand(parameter => { RemoveRow(1); }));

      public BaseWorkField(string tablename = "Table") {
        TableName = tablename;
        AddColumn(10);
        AddRow(20);
      }
    }
  }
}