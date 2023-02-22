using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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
      set {
        _dataTables = value; OnPropertyChanged();
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


    private ICommand _runFormulsCommand;


    public ICommand CreateTableCommand => _createTableCommand ?? (_createTableCommand = new RelayCommand(parameter => { CreateTable(); }));
    public ICommand LoadTableCommand => _loadTableCommand ?? (_loadTableCommand = new RelayCommand(parameter => { LoadTable(GetPathToLoad()); }));
    public ICommand SaveTableCommand => _saveTableCommand ?? (_saveTableCommand = new RelayCommand(parameter => { SaveTable(GetPathToSave()); }));
    public ICommand CloseTableCommand => _closeTableCommand ?? (_closeTableCommand = new RelayCommand(parameter => { CloseTable(SelectTableNumber); }));


    public ICommand AddColumnCommand => _addColumnCommand ?? (_addColumnCommand = new RelayCommand(parameter => { AddColumn(1); }));
    public ICommand RemoveColumnCommand => _removeColumnCommand ?? (_removeColumnCommand = new RelayCommand(parameter => { RemoveColumn(1); }));
    public ICommand AddRowCommand => _addRowCommand ?? (_addRowCommand = new RelayCommand(parameter => { AddRow(1); }));
    public ICommand RemoveRowCommand => _removeRowCommand ?? (_removeRowCommand = new RelayCommand(parameter => { RemoveRow(1); }));


    public ICommand RunFormulsCommand => _runFormulsCommand ?? (_runFormulsCommand = new RelayCommand(parameter => { RunFormuls(); }));
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
    #endregion

    #region Работа с таблицами

    private TableLanguage.Lang.Engine _engine = new();

    private void CloseTable(int tableNumber) {
      try { DataTables.Remove(DataTables[tableNumber]); } catch (Exception ex) { Console.WriteLine(ex.Message); }
    }
    private void CreateTable() {
      TableViewModel table = new TableViewModel(TableName);
      DataTables.Add(table);
    }

    private bool CheckTableIndex() => !(SelectTableNumber < 0 || SelectTableNumber > DataTables.Count);

    public void AddColumn(int coumn) {
      if (!CheckTableIndex()) return;
      DataTables[SelectTableNumber].AddColumn(coumn);
      OnPropertyChanged("Table");
    }
    public void RemoveColumn(int count) {
      if (CheckTableIndex()) DataTables[SelectTableNumber].RemoveColumn(count);
    }
    public void AddRow(int count) {
      if (!CheckTableIndex()) return;
      DataTables[SelectTableNumber].AddRow(count);
    }
    public void RemoveRow(int count) {
      if (!CheckTableIndex()) return;
      DataTables[SelectTableNumber].RemoveRow(count);
    }

    public string? GetCellContent(int row, int column) {
      if (!CheckTableIndex()) return null;
      return DataTables[SelectTableNumber].Table.Rows[row][column] as string;
    }
    public string[] GetColumnContent(int column) {
      if (!CheckTableIndex() || column < 0) return null;
      string[] columnContent = new string[DataTables[SelectTableNumber].Table.Rows.Count];
      for (int i = 0; i < DataTables[SelectTableNumber].Table.Rows.Count; i++) {
        columnContent[i] = Convert.ToString(DataTables[SelectTableNumber].Table.Rows[i][column]);
      }
      return columnContent;
    }
    public string[] GetRowContent(int rowNumber) {
      if (!CheckTableIndex() || rowNumber < 0) return null;
      string[] rowContent = new string[DataTables[SelectTableNumber].Table.Columns.Count];
      for (int i = 0; i < DataTables[SelectTableNumber].Table.Columns.Count; i++) {
        rowContent[i] = Convert.ToString(DataTables[SelectTableNumber].Table.Rows[rowNumber][i]);
      }
      return rowContent;
    }

    public void SetCellContent(int row, int column, string content) {
      if (!CheckTableIndex()) return;
      DataTables[SelectTableNumber].Table.Rows[row][column] = content;
    }
    public void RunFormuls() {
      for (int row = 0; row < DataTables[SelectTableNumber].Table.Rows.Count; row++) {
        for (int col = 0; col < DataTables[SelectTableNumber].Table.Columns.Count; col++) {
          string formula = GetCellContent(row, col);
          if (formula == null || !formula.StartsWith("=")) continue;
          formula = formula[1..];
          formula += ";";
          formula = new Regex(@"(\d+):(\d+)", RegexOptions.Compiled).Replace(formula, match => $"cell({match.Groups[1].Value},{match.Groups[2].Value})");
          DataTables[SelectTableNumber].SetCellForula(row, col, formula);
          SetCellContent(row, col, (string)_engine.ExecOneOperation(formula));
        }
      }
    }
    private string ConvertAddressToValue(string adr) {
      int index = adr.IndexOf(":");
      if (index == -1) return null;
      int col = Int32.Parse(adr.Substring(index++, adr.Length - index));
      int row = Int32.Parse(adr.Substring(0, index--));
      if (col < 0 || row < 0) return null;
      return GetCellContent(row,col);
    }
    private string MultStatementFormula(string formula) {
      
    }

    #endregion

    #region singleton
    private static WorkWindowViewModel _modelv;
    public static WorkWindowViewModel ModelV() {
      if (_modelv == null) {
        _modelv = new WorkWindowViewModel();
      }
      return _modelv;
    }
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
      CreateTable();
    }
  }
}