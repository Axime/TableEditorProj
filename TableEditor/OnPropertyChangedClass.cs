using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace INPCinModel
{
    /// <summary>Базовый класс с реализацией INPC </summary>
    public class OnPropertyChangedClass : INotifyPropertyChanged
    {
        #region Событие PropertyChanged
        /// <summary>Событие для извещения об изменения свойства</summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Методы вызова события PropertyChanged
        /// <summary>Метод для вызова события извещения об изменении свойства</summary>
        /// <param name="prop">Изменившееся свойство</param>
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            string[] names = prop.Split("\\/\r \n()\"\'-".ToArray(), StringSplitOptions.RemoveEmptyEntries);
            switch (names.Length)
            {
                case 0: break;
                case 1:
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
                    break;
                default:
                    OnPropertyChanged(names);
                    break;
            }

        }

        /// <summary>Метод для вызова события извещения об изменении списка свойств</summary>
        /// <param name="propList">Список имён свойств</param>
        public void OnPropertyChanged(IEnumerable<string> propList)
        {
            foreach (string prop in propList.Where(name => !string.IsNullOrWhiteSpace(name)))
                OnPropertyChanged(prop);
        }

        /// <summary>Метод для вызова события извещения об изменении списка свойств</summary>
        /// <param name="propList">Список свойств</param>
        public void OnPropertyChanged(IEnumerable<PropertyInfo> propList)
        {
            foreach (PropertyInfo prop in propList)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop.Name));
        }

        /// <summary>Метод для вызова события извещения об изменении всех свойств</summary>
        /// <param name="propList">Список свойств</param>
        public void OnAllPropertyChanged()
        {
            OnPropertyChanged(GetType().GetProperties());
        }
        #endregion


        /// <summary>Виртуальный метод изменяющий сначение поля значения свойсва</summary>
        /// <param name="fieldProperty">Ссылка на поле значения свойсва</param>
        /// <param name="newValue">Новое значение</param>
        /// <param name="nameProperty">Название свойства</param>
        public virtual void SetProperty<T>(ref T fieldProperty, T newValue, [CallerMemberName]string nameProperty = "") where T : IComparable<T>
        {
            if ((fieldProperty != null && fieldProperty.CompareTo(newValue) != 0) || (fieldProperty == null && newValue != null))
            {
                fieldProperty = newValue;
                OnPropertyChanged(nameProperty);
            }
        }

        /// <summary>Виртуальный метод изменяющий сначение поля значения свойсва</summary>
        /// <param name="fieldProperty">Ссылка на поле значения свойсва</param>
        /// <param name="newValue">Новое значение</param>
        /// <param name="nameProperty">Название свойства</param>
        public virtual void SetProperty<T, U>(T fieldProperty, U newValue, [CallerMemberName]string nameProperty = "") where T : class where U : class, T
        {
            if (fieldProperty != newValue)
            {
                fieldProperty = newValue;
                OnPropertyChanged(nameProperty);
            }
        }

    }
}
