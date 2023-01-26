using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestOpenClosePages.ViewModel {
  public class TabViewModel {
    public TabViewModel(string title, object content = default) => (Title, Content) = (title, content);
    public string Title { get; set; }
    public object Content { get; set; }
  }
}
