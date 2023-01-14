using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace TableEditor.Models {
  public class EditorModel : INotifyPropertyChanged {

    #region Реализация интерфейса INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion

    static EditorModel _model;
    public static EditorModel Model => _model ?? (_model = new EditorModel());

    #region Закрытые поля
    private string _username;
    #endregion
    public ObservableCollection<Table> Tables;
    public string Username {
      get => _username;
      set { _username = value; OnPropertyChanged(); }
    }

    public EditorModel() {
      Tables = new ObservableCollection<Table>();
      Username = HTTP.UserNickname;
    }

    public void CreateTable(string tableName, int column = 30, int row = 30) {
      if (tableName == "") tableName = $"New Table {Tables.Count.ToString()}";
      Table table = new(column, row) { Header = tableName };
      Tables.Add(table);
    }
    public void SaveTable(int tableNumber, string path) {
      Table toSave = Tables[tableNumber];
    }
    public void LoadTable(string path) {

    }

    public void AddColumn(int tableNumder, int coumn) { 
      Tables[tableNumder].AddColumn(coumn);
      OnPropertyChanged();
    }
    public void RemoveColumn(int tableNumber, int count) => Tables[tableNumber].RemoveColumn(count);
    public void AddRow(int tableNumder, int count) => Tables[tableNumder].AddRow(count);
    public void RemoveRow(int tableNumber, int count) => Tables[tableNumber].RemoveRow(count);



    public class Table {
      string? _header;
      DataTable _data;

      public string Header {
        get => _header;
        set { _header = value; _editorModel.OnPropertyChanged(); }
      }
      public DataTable Data {
        get => _data;
        set { _data = value; _editorModel.OnPropertyChanged(); }
      }

      public void AddColumn(int count) {
        for (int i = 0; i < count; i++) {
          Data.Columns.Add(new DataColumn($"Col {Data.Columns.Count.ToString()}", typeof(string)) { AllowDBNull = true });
        }
        Data = Data.Copy();
      }
      public void RemoveColumn(int count) {
        for (int i = 1; i <= count && Data.Columns.Count > 1; i++)
          Data.Columns.RemoveAt(Data.Columns.Count - 1);
        Data = Data.Copy();
      }
      public void AddRow(int count) {
        for (int i = 0; i < count; i++) Data.Rows.Add();
      }
      public void RemoveRow(int count) {
        for (int i = 1; i <= count && Data.Rows.Count > 1; i++)
          Data.Rows.RemoveAt(Data.Rows.Count - 1);
      }

      readonly EditorModel _editorModel;
      public Table(int column, int row) {
        _editorModel = EditorModel.Model;
        _data = new();
        AddColumn(column); AddRow(row);
      }
    }
  }
}
