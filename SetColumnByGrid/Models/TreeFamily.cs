using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace SetColumnByGrid.Models
{
    public class TreeFamily
    {
        public string Name { get; set; }
        public FamilySymbol MySymbol { get; set; }
    }

    public class TreeCategory
    {
        public string Name { get; set; }
        public List<TreeFamily> TreeFamilies { get; set; }

        public TreeCategory()
        {
            TreeFamilies = new List<TreeFamily>();
        }
    }
}
