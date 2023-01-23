using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TableEditor.Models;
using TableEditor.ViewModels;

namespace TableEditor.VM {
  public class WorkWindowViewModel : INotifyPropertyChanged {

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    #region Секция свойств
    public string UserName {
      get => _model.Username;
      set { _model.Username = value; OnPropertyChanged(); }
    }

    private ObservableCollection<TableViewModel> _dataTables;
    public ObservableCollection<TableViewModel> DataTables {
      get => _dataTables;
      set { _dataTables = value; OnPropertyChanged(); }
    }

    private string _tableName;
    private int _selectTableNumber;
    public string TableName {
      get => _tableName; set { _tableName = value; OnPropertyChanged(); }
    }
    public int SelectTableNumber {
      get => _selectTableNumber; set { _selectTableNumber = value; OnPropertyChanged(); }
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


    public ICommand CreateTableCommand => _createTableCommand ?? (_createTableCommand = new RelayCommand(parameter => { CreateTable(); }));
    public ICommand LoadTableCommand => _loadTableCommand ?? (_loadTableCommand = new RelayCommand(parameter => { _model.LoadTable(GetPathToLoad()); }));
    public ICommand SaveTableCommand => _saveTableCommand ?? (_saveTableCommand = new RelayCommand(parameter => { _model.SaveTable(SelectTableNumber, GetPathToSave()); }));


    public ICommand AddColumnCommand => _addColumnCommand ?? (_addColumnCommand = new RelayCommand(parameter => { _model.AddColumn(SelectTableNumber, 1); }));
    public ICommand RemoveColumnCommand => _removeColumnCommand ?? (_removeColumnCommand = new RelayCommand(parameter => { _model.RemoveColumn(SelectTableNumber, 1); }));
    public ICommand AddRowCommand => _addRowCommand ?? (_addRowCommand = new RelayCommand(parameter => { _model.AddRow(SelectTableNumber, 1); }));
    public ICommand RemoveRowCommand => _removeRowCommand ?? (_removeRowCommand = new RelayCommand(parameter => { _model.RemoveRow(SelectTableNumber, 1); }));
    #endregion

    #region private methods
    private void CreateTable() {
      TableViewModel table = new TableViewModel(TableName);
      DataTables.Add(table);
    }
    private void LoadTable(string path) {
      try {
        if (!File.Exists(path) || path == "") return;
        var data = JsonConvert.DeserializeObject<TableViewModel>(File.ReadAllText(path));
        DataTables.Add(data);

      } catch (Exception ex) { Console.WriteLine(ex.Message); }
    }
    private void CloseTable(int tableNumber) => DataTables.Remove(DataTables[tableNumber]);

    #endregion

    private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e) {
      if (e.PropertyName == "ResultChange")
        OnPropertyChanged("Result");
    }
    readonly EditorModel _model;
    public WorkWindowViewModel() {
      DataTables = new ObservableCollection<TableViewModel>();
      _model = EditorModel.Model;
      _model.PropertyChanged += Model_PropertyChanged;
    }
  }
}