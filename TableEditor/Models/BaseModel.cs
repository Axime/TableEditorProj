using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using TableEditor.ViewModels;

namespace TableEditor.Models {
  public class EditorModel : INotifyPropertyChanged {

    #region Реализация интерфейса INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion

    #region SingleTon
    static EditorModel _model;
    public static EditorModel Model => _model ?? (_model = new EditorModel());
    #endregion

    #region Закрытые поля
    private string _username;
    #endregion

    public ObservableCollection<Table> Tables;

    #region Публичныые свойства
    public string Username {
      get => _username;
      set { _username = value; OnPropertyChanged(); }
    }
    #endregion

    public EditorModel() {
      Tables = new ObservableCollection<Table>();
      Username = HTTP.UserNickname;
    }

    #region Методы для работы с файлами
    public DataTable CreateTable(string tableName, int column = 30, int row = 30) {
      if (tableName == "") tableName = $"New Table {Tables.Count.ToString()}";
      DataTable _table = new DataTable();
      return _table;
    }
    public void SaveTable(int tableNumber, string path) {
      try {
      Table toSave = Tables[tableNumber];
      string jsonData = JsonConvert.SerializeObject(toSave);
      File.WriteAllText(path, jsonData);
      }catch(Exception ex) { Console.WriteLine(ex.Message); }
    }
    public void LoadTable(string path) {
      try {
      if (!File.Exists(path) || path == "") return;
      var data = JsonConvert.DeserializeObject<Table>(File.ReadAllText(path));
      Tables.Add(data);

      }catch (Exception ex) { Console.WriteLine(ex.Message); }
    }
    public void CloseTable(int tableNumber) {
      Tables.Remove(Tables[tableNumber]);
    }
    
    #endregion

    #region Методы для работы с контентом таблиц
/*    public void AddColumn(int tableNumder, int coumn) {
      Tables[tableNumder].AddColumn(coumn);
      OnPropertyChanged();
    }
    public void RemoveColumn(int tableNumber, int count) => Tables[tableNumber].RemoveColumn(count);
    public void AddRow(int tableNumder, int count) => Tables[tableNumder].AddRow(count);
    public void RemoveRow(int tableNumber, int count) => Tables[tableNumber].RemoveRow(count);

    public string GetCellContent(int tableNumber, int column, int row) {
      string content = Tables[tableNumber].Data.Rows[row][column].ToString();
      return content;
    }
    public string[] GetColumnContent(int tableNumber, int column) {
      string[] columnContent = new string[Tables[tableNumber].Data.Rows.Count];
      for (int i = 0; i < Tables[tableNumber].Data.Rows.Count; i++) {
        columnContent[i] = Convert.ToString(Tables[tableNumber].Data.Rows[i][column]);
      }
      return columnContent;
    }
    public string[] GetRowContent(int tableNumber, int rowNumber) {
      string[] rowContent = new string[Tables[tableNumber].Data.Columns.Count];
      for (int i = 0; i < Tables[tableNumber].Data.Columns.Count; i++) {
        rowContent[i] = Convert.ToString(Tables[tableNumber].Data.Rows[rowNumber][i]);
      }
      return rowContent;
    }

    public void SetCellContent(int tableNumber, int column, int row, string content) => Tables[tableNumber].Data.Rows[row][column] = content;*/
    #endregion

    #region Tools
    //public void Bench(int tableNumber) {
    //  for (int i = 0; i < Tables[i].Data.Rows.Count; i++) {
    //    SetCellContent(tableNumber, 1, i, i.ToString());
    //  }
    //}
    #endregion


    public class Table {
      string? _header;
      DataTable _data;
      string _loction;

      public string Header {
        get => _header;
        set { _header = value; _editorModel.OnPropertyChanged(); }
      }
      public DataTable Data {
        get => _data;
        set { _data = value; _editorModel.OnPropertyChanged(); }
      }
      public string Location {
        get => _loction;
        set { _loction = value; _editorModel.OnPropertyChanged(); }
      }

/*      public void AddColumn(int count) {
        for (int i = 0; i < count; i++) {
          Data.Columns.Add(new DataColumn($"Col {Data.Columns.Count}", typeof(string)) { AllowDBNull = true });
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
*/
      readonly EditorModel _editorModel;
      public Table(int column, int row) {
        _editorModel = EditorModel.Model;
        _data = new();
        //AddColumn(column); AddRow(row);
      }
    }
  }
}
