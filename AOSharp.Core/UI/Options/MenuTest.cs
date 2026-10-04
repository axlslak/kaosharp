using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.Unmanaged.Imports;

namespace AOSharp.Core.UI.Options
{
    public class MenuTest : MenuItem
    {
        public bool Value { get; set; }

        public MenuTest(string name, string displayName) : base(name, displayName)
        {
        }

        internal override View CreateView()
        {
            //RadioButtonGroup radioGroup = RadioButtonGroup.Create(Name);

            IntPtr pView = XMLObject_c.LoadXMLObject(StdString.Create(@"C:\Users\tagyo\Desktop\Test.xml").Pointer, StdString.Create().Pointer);

            return new View(pView);
        }
    }
}
