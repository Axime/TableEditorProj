using System.Collections.Generic;
using System.Data;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TableEditor.Models;
using TableEditor;
using System.Windows;
using System;
using System.Linq;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static TableEditor.Models.EditorModel;
using Microsoft.Win32;
using Prism.Services.Dialogs;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

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
    public ObservableCollection<Table> DataTables {
      get => _model.Tables;
      set { _model.Tables = value; }
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
      ovd.Filter = "Text documents (*.txt)|*.txt|All files (*.*)|*.*";
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

    private ICommand _addColumnCommand;
    private ICommand _removeColumnCommand;
    private ICommand _addRowCommand;
    private ICommand _removeRowCommand;


    public ICommand CreateTableCommand => _createTableCommand ?? (_createTableCommand = new RelayCommand(parameter => { _model.CreateTable(TableName); }));
    public ICommand LoadTableCommand => _loadTableCommand ?? (_loadTableCommand = new RelayCommand(parameter => { _model.LoadTable(GetPathToLoad()); }));
    public ICommand SaveTableCommand => _saveTableCommand ?? (_saveTableCommand = new RelayCommand(parameter => { _model.SaveTable(SelectTableNumber, GetPathToSave()); }));

    public ICommand AddColumnCommand => _addColumnCommand ?? (_addColumnCommand = new RelayCommand(parameter => { _model.AddColumn(SelectTableNumber, 1); }));
    public ICommand RemoveColumnCommand => _removeColumnCommand ?? (_removeColumnCommand = new RelayCommand(parameter => { _model.RemoveColumn(SelectTableNumber, 1); }));
    public ICommand AddRowCommand => _addRowCommand ?? (_addRowCommand = new RelayCommand(parameter => { _model.AddRow(SelectTableNumber, 1); }));
    public ICommand RemoveRowCommand => _removeRowCommand ?? (_removeRowCommand = new RelayCommand(parameter => { _model.RemoveRow(SelectTableNumber, 1); }));
    #endregion

    private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e) {
      if (e.PropertyName == "ResultChange")
        OnPropertyChanged("Result");
    }

    readonly EditorModel _model;
    public WorkWindowViewModel() {
      _model = EditorModel.Model;
      _model.PropertyChanged += Model_PropertyChanged;
    }
  }
}