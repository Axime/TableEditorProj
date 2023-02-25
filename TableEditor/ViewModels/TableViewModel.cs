using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using TableEditor.Models;

namespace TableEditor.VM {
  public class TableViewModel : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    private readonly TableModel model;
    private DataTable _table = new();
    public DataTable Table {
      get => _table;
      set => _table = value;
    }

    public TableViewModel(string tableName) {
      _title = tableName;
      model= new TableModel(tableName);
      for (int i = 0; i < 10; i++) {
        AddColumn();
        AddRow();
      }
      AddRow();
      Update();
    }

    private bool isFormulaShows = false;
    private string _title;
    public string Title {
      get => _title;
      set => _title = value;
    }

    private string _modeTitle = "Значения";
    public string ModeTitle {
      get => _modeTitle;
      set {
        _modeTitle = value;
        ToggleView();
        Update();
        OnPropertyChanged(nameof(ModeTitle));
      }
    }

    public void ExecuteFormulas() {
      model.Execute();
      Update();
      //OnPropertyChanged(nameof(Table));
    }

    public void ToggleView() => isFormulaShows = !isFormulaShows;

    public void AddColumn() {
      model.AddColumn();
      Table.Columns.Add(new DataColumn() { AllowDBNull = true, Caption = $"{model.ColumnsCount}"});
      OnPropertyChanged(nameof(Table));
    }
    public void RemoveColumn() {
      model.RemoveColumn();
      Table.Columns.RemoveAt(model.ColumnsCount - 1);
    }

    public void AddRow() {
      model.AddRow();
      Table.Rows.Add();
    }
    public void RemoveRow() {
      model.RemoveRow();
      Table.Rows.RemoveAt(model.RowsCount - 1);
    }

    public string GetCellContent(int row, int column) => model.GetValue(row, column);
    public void SetCellContent(int row, int column, string content) {
      model.SetValue(row, column, content);
      Table.Rows[row][column] = content;
    }

    public string GetCellFormula(int row, int column) => model.GetFormula(row, column);
    public void SetCellFormula(int row, int column, string formula) {
      model.SetFormula(row, column, formula);
      Table.Rows[row][column] = formula;
    }

    private void Update() {
      string?[,] raw = isFormulaShows ? model.RawFormuals : model.RawValues;
      for (int i = 0; i < model.RowsCount; i++) {
        for (int j = 0; j < model.ColumnsCount; j++) {
          Table.Rows[i][j] = raw[i, j] ?? "";
        }
      }
      OnPropertyChanged(nameof(Table));
    }
  }
}
