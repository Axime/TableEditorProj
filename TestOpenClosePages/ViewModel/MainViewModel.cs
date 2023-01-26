using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestOpenClosePages.ViewModel
{
    public class MainViewModel
    {
    public ObservableCollection<TabViewModel> Tabs { get; set; } = new ObservableCollection<TabViewModel>();
    }
}
