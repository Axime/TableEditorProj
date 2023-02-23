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

    private bool _formulaShow = false;
    private string _tableName = "Новая таблица";
    private int _selectTableNumber;
    private Visibility _tabControlVisStatus = Visibility.Hidden;
    private DataGridCell _selectedItemTabControl = new();
    public string UserName {
      get => _model.Username;
      set { _model.Username = value; OnPropertyChanged(); }
    }
    public bool FormulaShow {
      get => _formulaShow;
      set {
        _formulaShow = value;
        OnPropertyChanged(nameof(FormulaShow));
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


    public ICommand CreateTableCommand => _createTableCommand ?? (_createTableCommand = new RelayCommand(parameter => { CreateTable(); }));
    public ICommand LoadTableCommand => _loadTableCommand ?? (_loadTableCommand = new RelayCommand(parameter => { LoadTable(GetPathToLoad()); }));
    public ICommand SaveTableCommand => _saveTableCommand ?? (_saveTableCommand = new RelayCommand(parameter => { SaveTable(GetPathToSave()); }));
    public ICommand CloseTableCommand => _closeTableCommand ?? (_closeTableCommand = new RelayCommand(parameter => { CloseTable(SelectTableNumber); }));


    public ICommand AddColumnCommand => _addColumnCommand ?? (_addColumnCommand = new RelayCommand(parameter => { AddColumn(1); }));
    public ICommand RemoveColumnCommand => _removeColumnCommand ?? (_removeColumnCommand = new RelayCommand(parameter => { RemoveColumn(1); }));
    public ICommand AddRowCommand => _addRowCommand ?? (_addRowCommand = new RelayCommand(parameter => { AddRow(1); }));
    public ICommand RemoveRowCommand => _removeRowCommand ?? (_removeRowCommand = new RelayCommand(parameter => { RemoveRow(1); }));


    public ICommand RunFormulasCommand => _runFormulasCommand ?? (_runFormulasCommand = new RelayCommand(parameter => { RunFormulas(); }));

    public ICommand InvertTableStatusCommand => _invertTableStatusCommand ?? (_invertTableStatusCommand = new RelayCommand(parameter => { ToggleFormulaView(); }));
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
    private static readonly Regex CellRegex = new("(\\d+):(\\d+)", RegexOptions.Compiled);
    private readonly TableLanguage.Lang.Engine _engine = new(new() { new(new() {
      ["cell"] = new(new TableLanguage.Lang.Runtime.NativeFunction((env, @this, args) => {
        var row = args[0];
        var col = args[1];
        //if (row.Val?.Type != TableLanguage.Lang.Runtime.RuntimeEntityType.Number)
        return _inst.CurrentTable.GetCellContent((int)row, (int)col) ?? "";
      }, "cell"), true, TableLanguage.Lang.Runtime.Reference.RefType.lvalue, null, true)
    }) });

    private void CloseTable(int tableNumber) {
      try { DataTables.Remove(DataTables[tableNumber]); } catch (Exception ex) { Console.WriteLine(ex.Message); }
    }
    private void CreateTable() {
      TableViewModel table = new TableViewModel(TableName);
      DataTables.Add(table);
    }

    private bool CheckTableIndex() => !(SelectTableNumber < 0 || SelectTableNumber > DataTables.Count);

    public void AddColumn(int column) {
      if (!CheckTableIndex()) return;
      CurrentTable.AddColumn(column);
      OnPropertyChanged("Table");
    }
    public void RemoveColumn(int count) {
      if (CheckTableIndex()) CurrentTable.RemoveColumn(count);
    }
    public void AddRow(int count) {
      if (!CheckTableIndex()) return;
      CurrentTable.AddRow(count);
    }
    public void RemoveRow(int count) {
      if (!CheckTableIndex()) return;
      CurrentTable.RemoveRow(count);
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
    }
    public void RunFormulas() {
      // Formula
      // =return value;
      // == function body
      for (int row = 0; row < CurrentTable.Table.Rows.Count; row++) {
        for (int col = 0; col < CurrentTable.Table.Columns.Count; col++) {
          string formula = GetCellContent(row, col) ?? "";
          if (formula == null || formula == "" || formula[0] != '=') continue;
          formula += ";";
          string result = "";
          formula = CellRegex.Replace(formula, match => $"cell({match.Groups[1].Value},{match.Groups[2].Value})");
          formula = formula[1..];
          if (formula[0] == '=') {
            formula = $"(function(){{{formula[1..]}}})();";
          }
          CurrentTable.SetCellFormula(row, col, formula);
          result = (string)_engine.ExecOneOperation(formula);
          SetCellContent(row, col, result);
        }
      }
    }

    public void ToggleFormulaView() {
      (CurrentTable.Table, CurrentTable.FormulasTable) = (CurrentTable.FormulasTable, CurrentTable.Table);
      FormulaShow = !FormulaShow;
      OnPropertyChanged(nameof(DataTables));
    }
    #endregion

    #region singleton
    private static WorkWindowViewModel _inst = null;
    public static WorkWindowViewModel Instance {
      get {
        if (_inst == null) _inst = new();
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
      if (_inst != null) {
        DataTables = _inst.DataTables;
        _model = _inst._model;
        _engine = _inst._engine;
        _tableName = _inst._tableName;
        _tabControlVisStatus = _inst._tabControlVisStatus;
        _selectTableNumber = _inst._selectTableNumber;
        _selectedItemTabControl = _inst._selectedItemTabControl;
        _saveTableCommand = _inst._saveTableCommand;
        _runFormulasCommand = _inst._runFormulasCommand;
        _removeRowCommand = _inst._removeRowCommand;
        _removeColumnCommand = _inst._removeColumnCommand;
        _loadTableCommand = _inst._loadTableCommand;
        _createTableCommand = _inst._createTableCommand;
        _closeTableCommand = _inst._closeTableCommand;
        _addRowCommand = _inst._addRowCommand;
        _addColumnCommand = _inst._addColumnCommand;
        UserName = _inst.UserName;
      } else {
        DataTables = new ObservableCollection<TableViewModel>();
        _model = EditorModel.Model;
        _model.PropertyChanged += Model_PropertyChanged;
        UserName = File.ReadAllText("User/nickname.txt");
        _inst = this;
        CreateTable();
      }
    }
  }
}