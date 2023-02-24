using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
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


    private ICommand _runFormulasCommand;

    private ICommand _invertTableStatusCommand;


    public ICommand CreateTableCommand => _createTableCommand ??= new RelayCommand(parameter => { CreateTable(); });
    public ICommand LoadTableCommand => _loadTableCommand ??= new RelayCommand(parameter => { LoadTable(GetPathToLoad()); });
    public ICommand SaveTableCommand => _saveTableCommand ??= new RelayCommand(parameter => { SaveTable(GetPathToSave()); });
    public ICommand CloseTableCommand => _closeTableCommand ??= new RelayCommand(parameter => { CloseTable(SelectTableNumber); });


    public ICommand AddColumnCommand => _addColumnCommand ??= new RelayCommand(parameter => { AddColumn(); });
    public ICommand RemoveColumnCommand => _removeColumnCommand ??= new RelayCommand(parameter => { RemoveColumn(); });
    public ICommand AddRowCommand => _addRowCommand ??= new RelayCommand(parameter => { AddRow(); });
    public ICommand RemoveRowCommand => _removeRowCommand ??= new RelayCommand(parameter => { RemoveRow(); });


    public ICommand RunFormulasCommand => _runFormulasCommand ??= new RelayCommand(parameter => { RunFormulas(); });

    public ICommand InvertTableStatusCommand => _invertTableStatusCommand ??= new RelayCommand(parameter => { ToggleFormulaView(); });
    #endregion


    #region Секция свойств
    private ObservableCollection<TableViewModel> _dataTables;
    public ObservableCollection<TableViewModel> DataTables {
      get => _dataTables;
      set {
        _dataTables = value; OnPropertyChanged();
      }
    }

    public TableViewModel CurrentTable {
      get => DataTables[SelectTableNumber];
      set => DataTables[SelectTableNumber] = value;
    }
    public string CurrentMode {
      get => CurrentTable.ModeTitle;
      set { CurrentTable.ModeTitle = value; OnPropertyChanged(nameof(CurrentMode)); }
    }
    private bool _formulaShow = false;
    private string _tableName = "Новая таблица";
    private int _selectTableNumber;
    private Visibility _tabControlVisStatus = Visibility.Hidden;
    private DataGridCell _selectedItemTabControl = new();
    public string UserName {
      get => _model.Username;
      set { _model.Username = value; OnPropertyChanged(); }
    }
    public bool IsFormulaView {
      get => _formulaShow;
      set {
        _formulaShow = value;
        OnPropertyChanged(nameof(IsFormulaView));
      }
    }
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




    #region Работа с таблицами

    private void CloseTable(int tableNumber) {
      try { DataTables.Remove(DataTables[tableNumber]); } catch (Exception ex) { Console.WriteLine(ex.Message); }
    }
    private void CreateTable() {
      TableViewModel table = new TableViewModel(TableName);
      DataTables.Add(table);
    }

    private bool CheckTableIndex() => !(SelectTableNumber < 0 || SelectTableNumber > DataTables.Count);

    public void AddColumn(int count = 1) {
      if (!CheckTableIndex()) return;
      for (int i = 0; i < count; i++) CurrentTable.AddColumn();
    }
    public void RemoveColumn(int count = 1) {
      if (!CheckTableIndex()) return;
      for (int i = 0; i < count; i++) CurrentTable.RemoveColumn();
    }
    public void AddRow(int count = 1) {
      if (!CheckTableIndex()) return;
      for (int i = 0; i < count; i++) CurrentTable.AddRow();
    }
    public void RemoveRow(int count = 1) {
      if (!CheckTableIndex()) return;
      for (int i = 0; i < count; i++) CurrentTable.RemoveRow();
    }

    public string? GetCellContent(int row, int column) {
      if (!CheckTableIndex()) return null;
      return CurrentTable.GetCellContent(row, column);
    }
    public string[] GetColumnContent(int column) {
      if (!CheckTableIndex() || column < 0) return null;
      string[] columnContent = new string[CurrentTable.Table.Rows.Count];
      for (int i = 0; i < CurrentTable.Table.Rows.Count; i++) {
        columnContent[i] = Convert.ToString(CurrentTable.Table.Rows[i][column]) ?? "";
      }
      return columnContent;
    }
    public string[] GetRowContent(int rowNumber) {
      if (!CheckTableIndex() || rowNumber < 0) return null;
      string[] rowContent = new string[CurrentTable.Table.Columns.Count];
      for (int i = 0; i < CurrentTable.Table.Columns.Count; i++) {
        rowContent[i] = Convert.ToString(CurrentTable.Table.Rows[rowNumber][i]) ?? "";
      }
      return rowContent;
    }

    public void SetCellContent(int row, int column, string content) {
      if (!CheckTableIndex()) return;
      CurrentTable.SetCellContent(row, column, content);
      OnPropertyChanged("Table");
    }
    public void RunFormulas() {
      // Formula
      // =return value;
      // == function body
      CurrentTable.ExecuteFormulas();
      OnPropertyChanged("Table");
    }

    public void ToggleFormulaView() {

      //(CurrentTable.Table, CurrentTable.FormulasTable) = (CurrentTable.FormulasTable, CurrentTable.Table);
      CurrentMode = IsFormulaView ? "Значения" : "Формулы";
      IsFormulaView = !IsFormulaView;
      OnPropertyChanged(nameof(CurrentMode));
    }
    private static readonly Regex CellRegex = new("(\\d+):(\\d+)", RegexOptions.Compiled);
    public void UpdateModel(int row, int column, string content) {
      if (!CheckTableIndex()) return;
      if (IsFormulaView) {
        string formula = content;
        if (formula == null || formula == "" || formula[0] != '=') formula = $"={formula}";
        formula += ";";
        formula = CellRegex.Replace(formula, match => $"cell({match.Groups[1].Value},{match.Groups[2].Value})");
        formula = formula[1..];
        if (formula[0] == '=') {
          formula = $"(function(){{{formula[1..]}}})();";
        }
        CurrentTable.SetCellFormula(row, column, formula);
      } else CurrentTable.SetCellContent(row, column, content);

    }
    #endregion

    #region singleton
    private static WorkWindowViewModel _inst;
    public static WorkWindowViewModel Instance {
      get {
        _inst ??= new();
        return _inst;
      }
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
      _inst = this;
      CreateTable();
      //}
    }
  }
}