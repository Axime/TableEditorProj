using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Documents;


namespace TableEditor.Models {
  internal class TableModel {
    private Dictionary<string, TableLanguage.Lang.Runtime.Reference> TableModuleProps = new();
    private void AddMethodsToLangEngine() {
      TableModuleProps.Add("cell", new(new TableLanguage.Lang.Runtime.NativeFunction((env, @this, args) => {
        var row = args[0];
        var col = args[1];
        //if (row.Val?.Type != TableLanguage.Lang.Runtime.RuntimeEntityType.Number)
        return GetValue((int)row, (int)col) ?? "";
      }, "cell"), true, TableLanguage.Lang.Runtime.Reference.RefType.lvalue, null, true));
    }

    public TableModel() {
      AddMethodsToLangEngine();
      _engine = new(new() { new(TableModuleProps) });
    }

    private readonly TableLanguage.Lang.Engine _engine;
    private void CheckIndex(int row, int column) {
      if (!(
      0 <= row && row < RowsCount &&
      0 <= column && column < ColumnsCount
      )) throw new ArgumentOutOfRangeException(
        $"Indexes must be: 0 <= row < {RowsCount} and 0 <= column < {ColumnsCount}," +
        $"but row = {row} and column = {column}"
      );
    }
    private string?[,] Formulas = new string?[0, 0];
    private string?[,] Values = new string?[0, 0];
    private bool[,] IsExecuted = new bool[0, 0];

    private void SetExecStatus(int row, int column, bool value) {
      CheckIndex(row, column);
      IsExecuted[row, column] = value;
    }

    private bool GetExecStatus(int row, int column) {
      CheckIndex(row, column);
      return IsExecuted[row, column];
    }


    public void Execute() {
      for (int row = 0; row < RowsCount; row++) {
        for (int col = 0; col < ColumnsCount; col++) {
          string formula = GetFormula(row, col) ?? "";
          if (formula == "") continue;
          string result = (string)_engine.ExecOneOperation(formula);
          SetExecStatus(row, col, true);
          internalSetValue(row, col, result);
        }
      }
    }
    private void internalSetValue(int row, int column, string value) {
      Values[row, column] = value;
    }
    public void SetValue(int row, int column, string? value) {
      CheckIndex(row, column);
      Values[row, column] = value;
      SetFormula(row, column, null);
      SetExecStatus(row, column, false);
    }

    public void SetFormula(int row, int column, string? value) {
      Formulas[row, column] = value;
      SetExecStatus(row, column, false);
    }

    public void AddRow() {
      var olds = (Values, Formulas, IsExecuted);
      RowsCount++;
      var n = (new string?[RowsCount, ColumnsCount], new string?[RowsCount, ColumnsCount], new bool[RowsCount, ColumnsCount]);
      for (int i = 0; i < RowsCount; i++) {
        for (int j = 0; j < ColumnsCount; j++) {
          if (i == RowsCount - 1) {
            n.Item1[i, j] = null;
            n.Item2[i, j] = null;
            n.Item3[i, j] = false;
            continue;
          }
          n.Item1[i, j] = olds.Item1[i, j];
          n.Item2[i, j] = olds.Item2[i, j];
          n.Item3[i, j] = olds.Item3[i, j];
        }
      }
      (Values, Formulas, IsExecuted) = n;
      GC.Collect();
    }

    public void RemoveRow() {
      if (RowsCount == 0) return;
      var olds = (Values, Formulas, IsExecuted);
      RowsCount--;
      var n = (new string?[RowsCount, ColumnsCount], new string?[RowsCount, ColumnsCount], new bool[RowsCount, ColumnsCount]);
      for (int i = 0; i < RowsCount; i++) {
        for (int j = 0; j < ColumnsCount; j++) {
          n.Item1[i, j] = olds.Item1[i, j];
          n.Item2[i, j] = olds.Item2[i, j];
          n.Item3[i, j] = olds.Item3[i, j];
        }
      }
      (Values, Formulas, IsExecuted) = n;
      GC.Collect();
    }

    public void AddColumn() {
      var olds = (Values, Formulas, IsExecuted);
      ColumnsCount++;
      var n = (new string?[RowsCount, ColumnsCount], new string?[RowsCount, ColumnsCount], new bool[RowsCount, ColumnsCount]);
      for (int i = 0; i < RowsCount; i++) {
        for (int j = 0; j < ColumnsCount; j++) {
          if (j == ColumnsCount - 1) {
            n.Item1[i, j] = null;
            n.Item2[i, j] = null;
            n.Item3[i, j] = false;
            continue;
          }
          n.Item1[i, j] = olds.Item1[i, j];
          n.Item2[i, j] = olds.Item2[i, j];
          n.Item3[i, j] = olds.Item3[i, j];
        }
      }
      (Values, Formulas, IsExecuted) = n;
      GC.Collect();
    }

    public void RemoveColumn() {
      if (ColumnsCount == 0) return;
      var olds = (Values, Formulas, IsExecuted);
      ColumnsCount--;
      var n = (new string?[RowsCount, ColumnsCount], new string?[RowsCount, ColumnsCount], new bool[RowsCount, ColumnsCount]);
      for (int i = 0; i < RowsCount; i++) {
        for (int j = 0; j < ColumnsCount; j++) {
          n.Item1[i, j] = olds.Item1[i, j];
          n.Item2[i, j] = olds.Item2[i, j];
          n.Item3[i, j] = olds.Item3[i, j];
        }
      }
      (Values, Formulas, IsExecuted) = n;
      GC.Collect();
    }

    public string GetFormula(int row, int column) {
      CheckIndex(row, column);
      return Formulas[row, column] ?? "";
    }

    public string GetValue(int row, int column) {
      CheckIndex(row, column);
      return Values[row, column] ?? "";
    }
    public int RowsCount {
      get; private set;
    } = 0;
    public int ColumnsCount {
      get; private set;
    } = 0;
    public (int, int) Size => (RowsCount, ColumnsCount);

    public string?[,] RawValues => Values;
    public string?[,] RawFormuals => Formulas;
  }
}
