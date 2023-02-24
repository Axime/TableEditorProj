using Aspose.Cells;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Xaml;
using TableEditor.Models;
using static TableLanguage.Lang;

namespace TableEditor.ViewModels {
  public class TableViewModel : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    private readonly TableModel model = new();
    private DataTable _table = new();
    public DataTable Table {
      get => _table;
      set => _table = value;
    }

    public TableViewModel(string tableName) {
      _title = tableName;
      for (int i = 0; i < 3; i++) {
        AddColumn();
        AddRow();
      }
      Update();
    }

    private bool isFormulaShows = false;
    private string _title;
    public string Title {
      get => _title;
      set => _title = value;
    }
    /*
        private DataTable _formulasTable = new();
        private Cell _currentCell;

        public bool isExecuted;


        public DataTable FormulasTable {
          get => _formulasTable;
          set => _formulasTable = value;
        }
        public Cell CurrentCell {
          get => _currentCell;
          set => _currentCell = value;
        }

        public void AddColumn(int count) {
          for (int i = 0; i < count; i++) {
            Table.Columns.Add(new DataColumn($"Col {Table.Columns.Count.ToString()}", typeof(string)) { AllowDBNull = true });
          }
          Table = Table.Copy();
          for (int i = 0; i < count; i++) {
            FormulasTable.Columns.Add(new DataColumn($"Col {FormulasTable.Columns.Count.ToString()}", typeof(string)) { AllowDBNull = true });
          }
          FormulasTable = Table.Copy();
        }
        public void RemoveColumn(int count) {
          for (int i = 1; i <= count && Table.Columns.Count > 1; i++)
            Table.Columns.RemoveAt(Table.Columns.Count - 1);
          Table = Table.Copy();
          for (int i = 1; i <= count && FormulasTable.Columns.Count > 1; i++)
            FormulasTable.Columns.RemoveAt(FormulasTable.Columns.Count - 1);
          FormulasTable = Table.Copy();
        }
        public void AddRow(int count) {
          for (int i = 0; i < count; i++) Table.Rows.Add();
          for (int i = 0; i < count; i++) FormulasTable.Rows.Add();
        }
        public void RemoveRow(int count) {
          for (int i = 1; i <= count && Table.Rows.Count > 1; i++) Table.Rows.RemoveAt(Table.Rows.Count - 1);
          for (int i = 1; i <= count && FormulasTable.Rows.Count > 1; i++) FormulasTable.Rows.RemoveAt(FormulasTable.Rows.Count - 1);
        }
        public string GetCellContent(int row, int column) => Table.Rows[row][column].ToString() ?? "";

        public string GetCellFormula(int row, int column) => FormulasTable.Rows[row][column].ToString() ?? "";

        public string[] GetColumnContent(int column) {
          string[] columnContent = new string[Table.Rows.Count];
          for (int i = 0; i < Table.Rows.Count; i++) {
            columnContent[i] = Convert.ToString(Table.Rows[i][column]) ?? "";
          }
          return columnContent;
        }
        public string[] GetRowContent(int rowNumber) {
          string[] rowContent = new string[Table.Columns.Count];
          for (int i = 0; i < Table.Columns.Count; i++) {
            rowContent[i] = Convert.ToString(Table.Rows[rowNumber][i]) ?? "";
          }
          return rowContent;
        }

        public void SetCellContent(int row, int column, string content) => Table.Rows[row][column] = content;
        public void SetCellFormula(int row, int column, string formula) {
          try {
            FormulasTable.Rows[row][column] = formula;
          } catch {
            return;
          }
        }

        
    */

    public void ExecuteFormulas() {
      model.Execute();
      OnPropertyChanged(nameof(Table));
    }

    public void ToggleView() => isFormulaShows = !isFormulaShows;

    public void AddColumn() {
      model.AddColumn();
      Table.Columns.Add(new DataColumn($"Col {Table.Columns.Count}", typeof(string)) { AllowDBNull = true });
    }
    public void RemoveColumn() {
      model.RemoveColumn();
      Table.Columns.RemoveAt(model.ColumnsCount);
    }

    public void AddRow() {
      model.AddRow();
      Table.Rows.Add();
    }
    public void RemoveRow() {
      model.RemoveRow();
      Table.Rows.RemoveAt(model.ColumnsCount);
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
