using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TableEditor.Models;
using TableEditor.ViewModels;

namespace TableEditor.VM {
  public class WorkWindowViewModel : INotifyPropertyChanged {

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    #region Секция свойств
    public string UserName {
      get => _model.Username;
      set { _model.Username = value; OnPropertyChanged(); }
    }
    private ObservableCollection<TableViewModel> _dataTables;
    public ObservableCollection<TableViewModel> DataTables {
      get => _dataTables;
      set { _dataTables = value; OnPropertyChanged();
      }
    }


    private string _tableName = "Новая таблица";
    private int _selectTableNumber;
    private Visibility _tabControlVisStatus = Visibility.Hidden;
    private DataGridCell _selectedItemTabControll = new();
    public string TableName {
      get => _tableName; set { _tableName = value; OnPropertyChanged(); }
    }
    public int SelectTableNumber {
      get => _selectTableNumber; set { _selectTableNumber = value; OnPropertyChanged(); }
    }
    public int TabControlVisStatus {
      get {
        return DataTables.Count;
      }
    }
    #endregion

    #region Секция команд
    private string GetPathToLoad() {
      OpenFileDialog ovd = new OpenFileDialog();
      ovd.Filter = "Text documents (*.pspt)|*.pspt";
      Nullable<bool> result = ovd.ShowDialog();
      if (result == true) {
        // Open document
        string filename = ovd.FileName;
        return filename;
      }
      return "";
    }
    private string GetPathToSave() {
      SaveFileDialog svd = new SaveFileDialog();
      svd.Filter = "Text documents (*.pspt)|*.pspt";
      Nullable<bool> result = svd.ShowDialog();
      if (result == true) {
        // Open document
        string filename = svd.FileName;
        return filename;
      }
      return "";
    }


    private ICommand _createTableCommand;
    private ICommand _loadTableCommand;
    private ICommand _saveTableCommand;
    private ICommand _closeTableCommand;


    private ICommand _addColumnCommand;
    private ICommand _removeColumnCommand;
    private ICommand _addRowCommand;
    private ICommand _removeRowCommand;


    public ICommand CreateTableCommand => _createTableCommand ?? (_createTableCommand = new RelayCommand(parameter => {
      CreateTable();
      
    }));
    public ICommand LoadTableCommand => _loadTableCommand ?? (_loadTableCommand = new RelayCommand(parameter => { LoadTable(GetPathToLoad()); }));
    public ICommand SaveTableCommand => _saveTableCommand ?? (_saveTableCommand = new RelayCommand(parameter => { SaveTable(GetPathToSave()); }));
    public ICommand CloseTableCommand => _closeTableCommand ?? (_closeTableCommand = new RelayCommand(parameter => {
      CloseTable(SelectTableNumber);
      
    }));


    public ICommand AddColumnCommand => _addColumnCommand ?? (_addColumnCommand = new RelayCommand(parameter => { AddColumn(1); }));
    public ICommand RemoveColumnCommand => _removeColumnCommand ?? (_removeColumnCommand = new RelayCommand(parameter => { RemoveColumn(1); }));
    public ICommand AddRowCommand => _addRowCommand ?? (_addRowCommand = new RelayCommand(parameter => { AddRow(1); }));
    public ICommand RemoveRowCommand => _removeRowCommand ?? (_removeRowCommand = new RelayCommand(parameter => { RemoveRow(1); }));
    #endregion

    #region Работа с файлами
    private void LoadTable(string path) {
      try {
        if (!File.Exists(path) || path == "") return;
        var data = JsonConvert.DeserializeObject<TableViewModel>(File.ReadAllText(path));
        DataTables.Add(data);
      } catch (Exception ex) { Console.WriteLine(ex.Message); }
    }
    private void SaveTable(string path) {
      try {
        TableViewModel toSave = DataTables[SelectTableNumber];
        string jsonData = JsonConvert.SerializeObject(toSave);
        File.WriteAllText(path, jsonData);
      } catch (Exception ex) { Console.WriteLine(ex.Message); }
    }
    private void CloseTable(int tableNumber) {
      try { DataTables.Remove(DataTables[tableNumber]); } catch (Exception ex) { Console.WriteLine(ex.Message); }
    }
    #endregion

    #region Работа с таблицами

    private void CreateTable() {
      TableViewModel table = new TableViewModel(TableName);
      DataTables.Add(table);
    }
    public void AddColumn(int coumn) {
      DataTables[SelectTableNumber].AddColumn(coumn);
      OnPropertyChanged("Table");
    }
    public void RemoveColumn(int count) => DataTables[SelectTableNumber].RemoveColumn(count);
    public void AddRow(int count) => DataTables[SelectTableNumber].AddRow(count);
    public void RemoveRow(int count) => DataTables[SelectTableNumber].RemoveRow(count);

    public string GetCellContent(int column, int row) {
      string content = DataTables[SelectTableNumber].Table.Rows[row][column].ToString();
      return content;
    }
    public string[] GetColumnContent(int column) {
      string[] columnContent = new string[DataTables[SelectTableNumber].Table.Rows.Count];
      for (int i = 0; i < DataTables[SelectTableNumber].Table.Rows.Count; i++) {
        columnContent[i] = Convert.ToString(DataTables[SelectTableNumber].Table.Rows[i][column]);
      }
      return columnContent;
    }
    public string[] GetRowContent(int rowNumber) {
      string[] rowContent = new string[DataTables[SelectTableNumber].Table.Columns.Count];
      for (int i = 0; i < DataTables[SelectTableNumber].Table.Columns.Count; i++) {
        rowContent[i] = Convert.ToString(DataTables[SelectTableNumber].Table.Rows[rowNumber][i]);
      }
      return rowContent;
    }

    public void SetCellContent(int column, int row, string content) => DataTables[SelectTableNumber].Table.Rows[row][column] = content;

    #endregion

    #region singleton
    static WorkWindowViewModel _modelv;
    public static WorkWindowViewModel ModelV => _modelv ?? (_modelv = new WorkWindowViewModel());
    #endregion

    private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e) {
      if (e.PropertyName == "ResultChange")
        OnPropertyChanged("Result");
    }
    readonly EditorModel _model;
    public WorkWindowViewModel() {
      DataTables = new ObservableCollection<TableViewModel>();
      _model = EditorModel.Model;
      _model.PropertyChanged += Model_PropertyChanged;
      UserName = File.ReadAllText("User/nickname.txt");
    }
  }
}