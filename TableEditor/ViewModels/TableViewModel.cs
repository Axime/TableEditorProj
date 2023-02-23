﻿using Aspose.Cells;
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
using TableEditor.Models;

namespace TableEditor.ViewModels {
  public class TableViewModel : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private DataTable _table = new();
    private DataTable _formulasTable = new();
    private string _title;
    private Cell _currentCell;
    public DataTable Table {
      get => _table;
      set => _table = value;
    }
    public DataTable FormulasTable {
      get => _formulasTable;
      set => _formulasTable = value;
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

    public TableViewModel(string tableName) {
      this.Title = tableName;
      AddColumn(3);
      AddRow(3);
    }
  }
}
