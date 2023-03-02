using System;
using System.Collections.Generic;

namespace TableEditor.Models {
  internal class TableModel {
    private readonly Dictionary<string, TableLanguage.Lang.Runtime.Reference> TableModuleProps = new();
    private void AddMethodsToLangEngine() {
      TableModuleProps.Add("cell", new(new TableLanguage.Lang.Runtime.NativeFunction((env, @this, args) => {
        var row = args[0];
        var col = args[1];
        return GetValue((int)row - 1, (int)col - 1) ?? "";
      }, "cell"), true, TableLanguage.Lang.Runtime.Reference.RefType.lvalue, null, true));
    }

    public readonly string name;

    public TableModel(string name) {
      AddMethodsToLangEngine();
      _engine = new(new() { new(TableModuleProps) });
      this.name = name;
    }

    private readonly TableLanguage.Lang.Engine _engine;
    /// <summary>
    /// Asserts that row and column indexes are valid
    /// </summary>
    /// <param name="row"></param>
    /// <param name="column"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private void CheckIndex(int row, int column) {
      if (!(0 <= row && row < RowsCount)) throw new ArgumentOutOfRangeException(nameof(row), row, $"must be 0 <= row < {RowsCount}");
      if (!(0 <= column && column < ColumnsCount)) throw new ArgumentOutOfRangeException(nameof(column), column, $"must be 0 <= row < {ColumnsCount}");
    }
    private string?[,] Formulas = new string?[0, 0];
    private string?[,] Values = new string?[0, 0];
    private bool[,] IsExecuted = new bool[0, 0];
    public string?[,] RawValues => Values;
    public string?[,] RawFormuals => Formulas;

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
          if (GetExecStatus(row, col)) continue;
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


    public byte[] ToBytes() => Raw.ToBytes(this);
    public static TableModel FromBytes(byte[] bytes) => Raw.FromBytes(bytes);

    private static class Raw {
      private class TableHeader {
        public const int HeaderReservedBytesCount =
          /*     Signatue     */ 8 +
          /*   headerLength   */ 4 +
          /*   count of rows  */ 4 +
          /* count of columns */ 4;

        //=================================================
        public static string signature = "PSPT";          // 8 bytes
        public const int header = 4;                      // 4 bytes
        public readonly string name;                      // 2 bytes * nameLength // variable length
        public readonly int rowCount;                     // 4 bytes
        public readonly int columnCount;                  // 4 bytes
        //=================================================

        public TableHeader(string name, int row, int column) {
          this.name = name;
          rowCount = row;
          columnCount = column;
        }
        public byte[] ToBytes(out ulong headerLength) {
          ulong ptr = 0;
          headerLength = (ulong)(name.RawLength() + HeaderReservedBytesCount);
          byte[] bytes = new byte[headerLength];
          bytes.PlaceString(signature, ref ptr);
          bytes.PlaceInt((int)headerLength, ref ptr);
          bytes.PlaceString(name, ref ptr);
          bytes.PlaceInt(rowCount, ref ptr);
          bytes.PlaceInt(columnCount, ref ptr);
          return bytes;
        }
        public static TableHeader FromBytes(in byte[] bytes, ref ulong ptr) {
          if (bytes.GetString(8, ref ptr) != signature) throw new Exception("Cannot read table: invalid signature");
          int headerLength = bytes.GetInt(ref ptr);
          string name = bytes.GetString(headerLength - HeaderReservedBytesCount, ref ptr);
          int row = bytes.GetInt(ref ptr);
          int column = bytes.GetInt(ref ptr);
          return new TableHeader(name, row, column);
        }
      }

      interface ITable {
        public byte[] ToBytes(out ulong len);
      }

      private class StringTable : ITable {

        /*========================================
         Data is an array of cells that contains:
            int rowIndex    | 4 bytes
            int columnIndex | 4 bytes
            int dataLength  | 4 bytes
         string data        | 2 bytes * dataLength
         =========================================
        After table data where are 3 nul bytes
        that ends table
        ==========================================*/

        public readonly string?[,] data;
        public readonly int rowCount;
        public readonly int columnCount;

        public StringTable(in string?[,] data, int rowCount, int columnCount) {
          this.data = data;
          this.rowCount = rowCount;
          this.columnCount = columnCount;
        }

        public byte[] ToBytes(out ulong length) {
          ulong ptr = 0;
          const int _100kb = 10_240;
          var currentArrayLength = 524_288; // 512 kb
          byte[] bytes = new byte[currentArrayLength];
          var CheckLength = (ulong required) => {
            while ((ptr + 1ul + required) > (ulong)currentArrayLength) currentArrayLength += _100kb;
            Array.Resize(ref bytes, currentArrayLength);
          };
          bytes.PlaceInt(rowCount, ref ptr);
          bytes.PlaceInt(columnCount, ref ptr);
          for (int row = 0; row < rowCount; row++) {
            for (int column = 0; column < columnCount; column++) {
              string? str = data[row, column];
              if (string.IsNullOrEmpty(str)) continue;
              CheckLength(4);
              bytes.PlaceInt(row, ref ptr);
              CheckLength(4);
              bytes.PlaceInt(column, ref ptr);
              var len = str.RawLength();
              CheckLength(4);
              bytes.PlaceInt(len, ref ptr);
              CheckLength((ulong)len);
              bytes.PlaceString(str, ref ptr);
            }
          }
          for (int i = 0; i < 3; i++) {
            CheckLength(4);
            bytes.PlaceInt(0, ref ptr);
          }
          Array.Resize(ref bytes, (int)ptr);
          length = ptr;
          return bytes;
        }

        public static StringTable FromBytes(in byte[] bytes, ref ulong ptr) {
          int rowCount = bytes.GetInt(ref ptr);
          int columnCount = bytes.GetInt(ref ptr);
          string?[,] data = new string?[rowCount, columnCount];

          while (true) {
            int row = bytes.GetInt(ref ptr);
            int col = bytes.GetInt(ref ptr);
            int strLen = bytes.GetInt(ref ptr);
            if (row == 0 && col == 0 && strLen == 0) break;
            string str = bytes.GetString(strLen, ref ptr);
            data[row, col] = str;
          }
          return new(data, rowCount, columnCount);
        }
      }

      private class BoolTable : ITable {
        public int rowCount;
        public int columnCount;
        public bool[,] data;

        public BoolTable(bool[,] data, int rowCount, int columnCount) {
          this.rowCount = rowCount;
          this.columnCount = columnCount;
          this.data = data;
        }

        public byte[] ToBytes(out ulong length) {
          ulong ptr = 0;
          length = 4 + 4 + (ulong)Math.Ceiling(rowCount * columnCount / 8.0);
          byte[] bytes = new byte[length];
          bytes.PlaceInt(rowCount, ref ptr);
          bytes.PlaceInt(columnCount, ref ptr);
          for (int i = 0; i < (int)(length - 8); i++, ptr++) {
            byte b = 0;
            for (int offset = 0; offset < 8; offset++) {
              int row = Math.DivRem(i * 8 + offset, columnCount, out int col);
              byte v;
              if (row < rowCount && col < columnCount) v = data[row, col] ? (byte)1 : (byte)0;
              else continue;
              b |= (byte)(v << (7 - offset));
            }
            bytes[ptr] = b;
          }
          return bytes; ;
        }

        public static BoolTable FromBytes(in byte[] bytes, ref ulong ptr) {
          var rowCount = bytes.GetInt(ref ptr);
          var columnCount = bytes.GetInt(ref ptr);
          var totalBitCount = rowCount * columnCount;
          var byteCount = (int)Math.Ceiling(totalBitCount / 8.0);
          bool[,] data = new bool[rowCount, columnCount];

          bool isEnd = false;
          for (int i = 0; i < byteCount && !isEnd; i++, ptr++) {
            byte b = bytes[ptr];
            for (int offset = 0; offset < 8; offset++) {
              bool bit = (b >> (7 - offset) & 1) == 1;
              int row = Math.DivRem(i * 8 + offset, columnCount, out int col);
              if (i * 8 + offset >= totalBitCount) {
                isEnd = true; break;
              }
                data[row, col] = bit;
            }
          }
          return new(data, rowCount, columnCount);
        }

      }


      public static byte[] ToBytes(TableModel table) {
        var rowCnt = table.RowsCount;
        int colCnt = table.ColumnsCount;
        TableHeader header = new(table.name, rowCnt, colCnt);
        StringTable values = new(table.Values, rowCnt, colCnt);
        StringTable formulas = new(table.Formulas, rowCnt, colCnt);
        BoolTable execStatus = new(table.IsExecuted, rowCnt, colCnt);
        byte[] pHeader = header.ToBytes(out var headerLength);
        byte[] pValues = values.ToBytes(out var valuesLength);
        byte[] pFormula = formulas.ToBytes(out var formulasLength);
        byte[] pExecTable = execStatus.ToBytes(out var execTableLength);
        ulong ptr = 0;
        var bytes = new byte[headerLength + valuesLength + formulasLength + execTableLength];
        for (ulong i = 0; i < headerLength; i++, ptr++) bytes[ptr] = pHeader[i];
        for (ulong i = 0; i < valuesLength; i++, ptr++) bytes[ptr] = pValues[i];
        for (ulong i = 0; i < formulasLength; i++, ptr++) bytes[ptr] = pFormula[i];
        for (ulong i = 0; i < execTableLength; i++, ptr++) bytes[ptr] = pExecTable[i];
        GC.Collect();
        return bytes;
      }

      public static TableModel FromBytes(byte[] bytes) {
        ulong ptr = 0;
        TableHeader head = TableHeader.FromBytes(bytes, ref ptr);
        StringTable values = StringTable.FromBytes(bytes, ref ptr);
        StringTable formulas = StringTable.FromBytes(bytes, ref ptr);
        BoolTable execTable = BoolTable.FromBytes(bytes, ref ptr);

        var rows = head.rowCount;
        var columns = head.columnCount;

        TableModel model = new(head.name);
        for (int i = 0; i < rows; i++) model.AddRow();
        for (int i = 0; i < columns; i++) model.AddColumn();
        for (int i = 0; i < rows; i++) {
          for (int j = 0; j < columns; j++) {
            model.SetValue(i, j, values.data[i, j]);
            model.SetFormula(i, j, formulas.data[i, j]);
            model.SetExecStatus(i, j, execTable.data[i, j]);
          }
        }
        return model;

      }

    }

  }

  static class ByteArrayExtensions {
    public static byte[] ToBytes(this int n) {
      var bytes = new byte[4];
      for (int i = 0; i < 4; i++) {
        bytes[i] = (byte)((n >> (8 * (3 - i))) & 0xff);
      }
      return bytes;
    }
    public static int GetInt(this byte[] bytes) {
      ulong _ = 0;
      return bytes.GetInt(ref _);
    }
    public static int GetInt(this byte[] bytes, ref ulong ptr) {
      int res = 0;
      for (int i = 0; i < 4; i++) res |= bytes[ptr++] << (8 * (3 - i));
      return res;
    }

    public static void PlaceInt(this byte[] bytes, int n, ref ulong ptr) {
      byte[] rawInt = n.ToBytes();
      for (int i = 0; i < 4; i++, ptr++) bytes[ptr] = rawInt[i];
    }

    public static void PlaceString(this byte[] bytes, string str, ref ulong ptr) {
      var charArray = str.ToCharArray();
      for (int i = 0; i < charArray.Length; i++) {
        var sh = (short)charArray[i];
        bytes[ptr++] = (byte)(sh >> 8);
        bytes[ptr++] = (byte)(sh & 0xFF);
      }
    }

    public static string GetString(this byte[] bytes, int len, ref ulong ptr) {
      char[] chars = new char[len / 2];
      for (int i = 0; i < len / 2; i++) {
        byte high = bytes[ptr++];
        byte low = bytes[ptr++];
        chars[i] = (char)((high << 8) | low);
      }
      return new string(chars);
    }

    public static int RawLength(this string str) => str.ToCharArray().Length * 2;

  }
}
