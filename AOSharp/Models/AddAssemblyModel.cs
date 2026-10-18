using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AOSharp.Models
{
    public class AddAssemblyModel : INotifyPropertyChanged
    {
        private List<string> _dllPaths = new List<string>();
        public List<string> DllPaths
        {
            get => _dllPaths;
            set
            {
                _dllPaths = value;
                OnPropertyChanged("DllPaths");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
