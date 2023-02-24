using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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

    #region Работа с файлами
    private void LoadTable(string path) {
      try {
        if (!File.Exists(path) || path == "") return;
        var data = JsonConvert.DeserializeObject<TableViewModel>(File.ReadAllText(path));
        DataTables.Add(data);
      } catch (Exception ex) {
        Debug.WriteLine(ex.Message);
      }
    }
    private void SaveTable(string path) {
      try {
        if (!CheckTableIndex()) return;
        TableViewModel toSave = CurrentTable;
        string jsonData = JsonConvert.SerializeObject(toSave);
        File.WriteAllText(path, jsonData);
      } catch (Exception ex) {
        Debug.WriteLine(ex.Message);
      }
    }
    #endregion

    private const string OpenDialogFilter = "PSP TableEditor Table (*.pspt)|*.pspt";
    private static string GetPathToLoad() {
      OpenFileDialog dlg = new() {
        Filter = OpenDialogFilter
      };
      bool? result = dlg.ShowDialog();
      if (result == true) return dlg.FileName;
      return "";
    }
    private static string GetPathToSave() {
      SaveFileDialog dlg = new() {
        Filter = OpenDialogFilter
      };
      bool? result = dlg.ShowDialog();
      if (result == true) return dlg.FileName;
      return "";
    }

    #region Секция команд
    // table command
    private readonly ICommand _createTableCommand;
    private readonly ICommand _loadTableCommand;
    private readonly ICommand _saveTableCommand;
    private readonly ICommand _closeTableCommand;
    // table row/column command
    private readonly ICommand _addColumnCommand;
    private readonly ICommand _removeColumnCommand;
    private readonly ICommand _addRowCommand;
    private readonly ICommand _removeRowCommand;
    // modes command
    private readonly ICommand _runFormulasCommand;
    private readonly ICommand _toggleViewMode;
    // properties
    public ICommand CreateTableCommand => _createTableCommand;
    public ICommand LoadTableCommand => _loadTableCommand;
    public ICommand SaveTableCommand => _saveTableCommand;
    public ICommand CloseTableCommand => _closeTableCommand;


    public ICommand AddColumnCommand => _addColumnCommand;
    public ICommand RemoveColumnCommand => _removeColumnCommand;
    public ICommand AddRowCommand => _addRowCommand;
    public ICommand RemoveRowCommand => _removeRowCommand;


    public ICommand RunFormulasCommand => _runFormulasCommand;
    public ICommand ToggleViewMode => _toggleViewMode;
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
      get {
        if (!CheckTableIndex()) return "Значения";
        return CurrentTable.ModeTitle;
      }
      set {
        if (!CheckTableIndex()) return;
        CurrentTable.ModeTitle = value;
        OnPropertyChanged(nameof(CurrentMode));
      }
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
      TableViewModel table = new(TableName);
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
      if (!CheckTableIndex() || rowNumber < 0) return Array.Empty<string>();
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
      if (!CheckTableIndex()) return;
      CurrentTable.ExecuteFormulas();
      OnPropertyChanged("Table");
    }

    public void ToggleFormulaView() {
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
      _createTableCommand = new RelayCommand(parameter => { CreateTable(); });
      _loadTableCommand = new RelayCommand(parameter => { LoadTable(GetPathToLoad()); });
      _saveTableCommand = new RelayCommand(parameter => { SaveTable(GetPathToSave()); });
      _closeTableCommand = new RelayCommand(parameter => { CloseTable(SelectTableNumber); });
      _addColumnCommand = new RelayCommand(parameter => { AddColumn(); });
      _removeColumnCommand = new RelayCommand(parameter => { RemoveColumn(); });
      _addRowCommand = new RelayCommand(parameter => { AddRow(); });
      _removeRowCommand = new RelayCommand(parameter => { RemoveRow(); });
      _runFormulasCommand = new RelayCommand(parameter => { RunFormulas(); });
      _toggleViewMode = new RelayCommand(parameter => { ToggleFormulaView(); });

    }
  }
}