using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboSharp.BackupApp.ViewModels
{
    internal class RadioButtonHelper
    {
        public RadioButtonHelper(object name) { Name = name.ToString(); }
        public string Name { get; set; }
        public bool IsChecked { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }
}
