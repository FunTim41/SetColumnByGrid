using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace SetColumnByGrid.Models
{
    public class NewColumn
    {/// <summary>
     /// 顶部标高
     /// </summary>
        public Level TopLevel { get; set; }

        /// <summary>
        /// 底部标高
        /// </summary>
        public Level BottomLevel { get; set; }
        /// <summary>
        /// 是否按层分割
        /// </summary>
        public bool IsDiv { get; set; }
       /// <summary>
       /// 水平偏移
       /// </summary>
        public double HorOffset { get; set; }
        /// <summary>
        /// 竖向偏移
        /// </summary>
        public double VerOffset { get; set; }
        /// <summary>
        /// 角度偏移
        /// </summary>
        public double AngOffset { get; set; }
        /// <summary>
        /// 柱的类型
        /// </summary>
        public FamilySymbol ColumnSymbol { get; set; }
    }
}
