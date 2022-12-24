using System.Windows.Controls;
using System.Windows;
using System;
using System.Windows.Data;

namespace API {
  public static class APITable {
    
    public enum Types {
      @int,
      @float,
      @duble,
      @byte,
      @string,
      @bool,
    }

    public class Cell {
      public Cell(int collumn, int row, object value = null) {
        this.collumn = collumn; this.row = row; this.value = value;
      }

      public int collumn; public int row;
      public object value;

      public static Cell GetCell(int collumn, int row) {
        Cell cell = new(collumn, row);
        
        return cell;
      }
      public bool SetParam(Cell cell) {
        return true;
      }

      public class Parameters {
        
        
      }
    }

    public class Collumn {
      public Collumn(string head) {
        this._head = head; 
      }
      string _head;
      public string head {
        get => _head;
        set {
          if (value != null) { _head = value; } else return;
        }
      }

      

      

      public static Collumn GetCollunm() {
        
        Collumn coll = new("test");
        return coll;
      }

    }

    public static class Methods {
      public static bool SetValue<T>(Cell cell, T value) => true;

      public static bool AddColumn(string header, DataGrid dataGrid) {
        
        return true;
      }
    }
  }
}
