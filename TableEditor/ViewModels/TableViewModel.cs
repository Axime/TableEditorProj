using Aspose.Cells;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Markup;
using TableEditor.Models;

namespace TableEditor.ViewModels {
  public class TableViewModel : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private DataTable _table = new();
    private string _title;
    private Cell _currentCell;
    public DataTable Table {
      get => _table;
      set => _table = value;
    }
    public string Title {
      get => _title;
      set => _title = value;
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
    }
    public void RemoveColumn(int count) {
      for (int i = 1; i <= count && Table.Columns.Count > 1; i++)
        Table.Columns.RemoveAt(Table.Columns.Count - 1);
      Table = Table.Copy();
    }
    public void AddRow(int count) {
      for (int i = 0; i < count; i++) Table.Rows.Add();
    }
    public void RemoveRow(int count) {
      for (int i = 1; i <= count && Table.Rows.Count > 1; i++)
        Table.Rows.RemoveAt(Table.Rows.Count - 1);
    }
    public string GetCellContent(int column, int row) {
      string content = Table.Rows[row][column].ToString();
      return content;
    }
    public string[] GetColumnContent(int column) {
      string[] columnContent = new string[Table.Rows.Count];
      for (int i = 0; i < Table.Rows.Count; i++) {
        columnContent[i] = Convert.ToString(Table.Rows[i][column]);
      }
      return columnContent;
    }
    public string[] GetRowContent(int rowNumber) {
      string[] rowContent = new string[Table.Columns.Count];
      for (int i = 0; i < Table.Columns.Count; i++) {
        rowContent[i] = Convert.ToString(Table.Rows[rowNumber][i]);
      }
      return rowContent;
    }

    public void SetCellContent(int column, int row, string content) => Table.Rows[row][column] = content;

    public TableViewModel(string tableName) {
      this.Title = tableName;
      AddColumn(20);
      AddRow(20);
    }
  }
}
