using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.Exceptions;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Revit.Async;
using SetColumnByGrid.Models;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

namespace SetColumnByGrid
{
    public partial class MainViewModel : ObservableObject
    {
        private ExternalCommandData commandData;
        UIApplication uiapp;
        UIDocument uidoc;
        Document doc;

        [ObservableProperty]
        ObservableCollection<TreeCategory> treeCategories;

        [ObservableProperty]
        bool viewEnabled = true;

        [ObservableProperty]
        TreeFamily selectedSymbol;

        [ObservableProperty]
        List<string> xoffsetDistance;

        [ObservableProperty]
        List<string> yoffsetDistance;

        [ObservableProperty]
        List<string> offsetAngle;

        [ObservableProperty]
        List<Level> topLevel;

        [ObservableProperty]
        List<Level> bottomLevel;

        [ObservableProperty]
        Level selectedTopLevel;

        [ObservableProperty]
        Level selectedBottomLevel;

        [ObservableProperty]
        bool isArchColumn = true;

        [ObservableProperty]
        bool isStruColumn = false;

        [ObservableProperty]
        string selectedAngOffset;

        [ObservableProperty]
        string selectedhorOffset;

        [ObservableProperty]
        string selectedverOffset;

        /// <summary>
        /// 是否按层分割
        /// </summary>
        [ObservableProperty]
        bool isDivByLevel;

        Level curLevel;
        Level nextLevel;
        List<Level> allLevel;

        public  MainViewModel(ExternalCommandData commandData)
        {
            this.commandData = commandData;
            uiapp = commandData.Application;
            uidoc = uiapp.ActiveUIDocument;
            doc = uidoc.Document;
             
            IniData();
        }

        private void IniData()
        {
            LoadComboboxData();
            LoadAllColumnType();
        }

        private void LoadComboboxData()
        {
            XoffsetDistance = new List<string>()
            {
                "中心对齐",
                "左对齐",
                "右对齐",
                "10",
                "20",
                "50",
                "100",
                "200",
                "300",
            };
            SelectedhorOffset = XoffsetDistance[0];
            YoffsetDistance = new List<string>()
            {
                "中心对齐",
                "上对齐",
                "下对齐",
                "10",
                "20",
                "50",
                "100",
                "200",
                "300",
            };
            SelectedverOffset = YoffsetDistance[0];
            OffsetAngle = new List<string>() { "0", "30", "45", "60", "90" };
            SelectedAngOffset = OffsetAngle[0];
            LoadRevitAllLevel();
        }

        private void LoadRevitAllLevel()
        {
            allLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();
            
            using (Transaction t = new Transaction(doc, "标高"))
            {
                t.Start();
                curLevel = Level.Create(doc, doc.ActiveView.GenLevel.Elevation);
                curLevel.Name = "当前标高";
                var level = allLevel.FirstOrDefault(i => i.Elevation > curLevel.Elevation);
                if (level != null)
                {
                    nextLevel = Level.Create(doc, level.Elevation);
                    nextLevel.Name = "当前标高之上一标高";
                }
                else
                {
                    nextLevel = Level.Create(doc, curLevel.Elevation);
                    nextLevel.Name = "当前标高之上一标高";
                }
                t.Commit();
            }
            BottomLevel = allLevel.Where(i => i.Elevation <= curLevel.Elevation).ToList();
            BottomLevel.Add(curLevel);
            BottomLevel.OrderBy(i => i.Name).ToList();
            TopLevel = allLevel.Where(i => i.Elevation > curLevel.Elevation).ToList();
            TopLevel.Add(nextLevel);
            TopLevel.OrderBy(i => i.Name).ToList();
            SelectedTopLevel = TopLevel[0];
            SelectedBottomLevel = BottomLevel[0];
        }

        private void LoadAllColumnType()
        {
            //结构柱familysmybol
            var collector1 = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .ToList();
            //建筑柱familysmybol
            var collector2 = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Columns)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .ToList();
            collector1.AddRange(collector2);
            var ColumnTypes = collector1.GroupBy(i => i.FamilyName).ToList();
            TreeCategories = new ObservableCollection<TreeCategory>();
            foreach (var item in ColumnTypes)
            {
                TreeCategory treeCategory = new TreeCategory();
                treeCategory.Name = item.Key;
                treeCategory.TreeFamilies = new List<TreeFamily>();
                foreach (var symbol in item)
                {
                    TreeFamily treeFamily = new TreeFamily();
                    treeFamily.Name = symbol.Name;
                    treeFamily.MySymbol = symbol;
                    treeCategory.TreeFamilies.Add(treeFamily);
                }
                TreeCategories.Add(treeCategory);
            }
            SelectedSymbol = TreeCategories.First().TreeFamilies[0];
        }

        [RelayCommand]
        void SelChange(object selectedItem)
        {
            SelectedSymbol = selectedItem as TreeFamily;
        }

        [RelayCommand]
        async Task LoadFamily()
        {
            try
            {
                await RevitTask.RunAsync(
                    (uiapp) =>
                    {
                        //uiapp.PostCommand(RevitCommandId.LookupCommandId("ID_FAMILY_LOAD"));
                        OpenFileDialog loadFamilypath = new OpenFileDialog();
                        loadFamilypath.Filter = "Revit Family Files (*.rfa)|*.rfa";
                        if (loadFamilypath.ShowDialog() == true)
                        {
                            var path = loadFamilypath.FileName;
                            using (Transaction t = new Transaction(doc, "载入族"))
                            {
                                t.Start();
                                bool result = doc.LoadFamily(path);
                                if (result)
                                {
                                    TreeCategories.Clear();
                                    LoadAllColumnType();
                                }
                                t.Commit();
                            }
                        }
                    }
                );
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Tip", ex.Message);
                return;
            }
        }

        /// <summary>
        /// 选点创建柱
        /// </summary>
        [RelayCommand]
        void SelectPoint()
        {
            RevitTask.RunAsync(() =>
            {
                using (Transaction t = new Transaction(doc, "新建柱"))
                {
                    t.Start();
                    while (true)
                    {
                        try
                        {
                            ViewEnabled = false;
                            XYZ selPoint = uidoc.Selection.PickPoint(
                                ObjectSnapTypes.Intersections,
                                "请选择轴线交点"
                            );
                            // TaskDialog.Show("Tip", selPoint.ToString());
                            CreateNewColumn(selPoint);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("Tip", ex.Message);
                            t.RollBack();
                            return;
                        }
                        finally
                        {
                            ViewEnabled = true;
                        }
                    }
                    t.Commit();
                }
            });
        }

        /// <summary>
        /// 选轴线创建柱
        /// </summary>
        [RelayCommand]
        void SelectedLine()
        {
            try
            {
                ViewEnabled = false;
                Reference reference = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    new GridFilter()
                );
                Grid grid = doc.GetElement(reference) as Grid;
                var gridcurve = grid.Curve as Line;

                var grids = new FilteredElementCollector(doc)
                    .OfClass(typeof(Grid))
                    .Cast<Grid>()
                    .ToList();
                grids.Remove(grid);
                RevitTask.RunAsync(() =>
                {
                    using (Transaction t = new Transaction(doc, "新建柱"))
                    {
                        t.Start();

                        foreach (var item in grids)
                        {
                            var itemcurve = item.Curve as Line;

                            var point = GetLineIntersection(gridcurve, itemcurve);
                            if (point != null)
                            {
                                try
                                {
                                    CreateNewColumn(point);
                                }
                                catch (Exception ex)
                                {
                                    TaskDialog.Show("Tip", ex.Message);
                                    return;
                                }
                                finally
                                {
                                    ViewEnabled = true;
                                }
                            }
                        }
                        t.Commit();
                    }
                });
            }
            catch (OperationCanceledException)
            {

                return;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Tip", ex.Message);
                return;
            }
            finally { ViewEnabled = true; }
        }

        /// <summary>
        /// 框选轴线
        /// </summary>
        [RelayCommand]
        void SelectedWindow()
        {
            List<Grid> grids = uidoc
                .Selection.PickElementsByRectangle(new GridFilter())
                .Cast<Grid>()
                .ToList();
            List<XYZ> points = new List<XYZ>();
            int count = grids.Count;
            Grid current;
            Grid next;
            for (int i = 0; i < count - 1; i++)
            {
                current = grids[i];
                for (int j = i + 1; j < count; j++)
                {
                    next = grids[j];
                    points.Add(GetLineIntersection(current.Curve as Line, next.Curve as Line));
                }
            }
            points.RemoveAll(i => i == null);
            RevitTask.RunAsync(() =>
            {
                using (Transaction t = new Transaction(doc, "新建柱"))
                {
                    t.Start();

                    foreach (var point in points)
                    {
                        try
                        {
                            ViewEnabled = false;
                            CreateNewColumn(point);
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("Tip", ex.Message);
                            return;
                        }
                        finally
                        {
                            ViewEnabled = true;
                        }
                    }
                    t.Commit();
                }
            });
        }

        public XYZ GetLineIntersection(Line line1, Line line2)
        { // 获取直线的方向向量
            XYZ direction1 = line1.Direction;
            XYZ direction2 = line2.Direction;
            // 获取直线的起点
            XYZ point1 = line1.GetEndPoint(0);
            XYZ point2 = line2.GetEndPoint(0);
            //计算两条直线的参数方程
            //  line1: P1 + t * D1
            // line2: P2 + s * D2
            // 解方程 P1 +t * D1 = P2 + s * D2
            double a = direction1.X;
            double b = -direction2.X;
            double c = direction1.Y;
            double d = -direction2.Y;
            double det = a * d - b * c;
            if (Math.Abs(det) < 1e-9)
            { // 平行或重合，无交点
                return null;
            }
            double dx = point2.X - point1.X;
            double dy = point2.Y - point1.Y;
            double t = (dx * d - dy * b) / det;
            //double s = (dx * c - dy * a) / det;
            // 计算交点
            XYZ intersection = point1 + t * direction1;
            //TaskDialog.Show(
            //    "交点",
            //   $"两条直线的交点为: ({intersection.X}, {intersection.Y}, {intersection.Z})"
            // );
            return intersection;
        }

        private void CreateNewColumn(XYZ selPoint)
        { //结构类型
            StructuralType structuraltype;
            if (IsArchColumn == false)
            {
                structuraltype = StructuralType.Column;
            }
            else
            {
                structuraltype = StructuralType.NonStructural;
            }
            selPoint = SetColumnPosition(selPoint);
            if (IsDivByLevel)
            {
                List<Level> Levels = new List<Level>();
                foreach (Level level in allLevel)
                {
                    if (
                        level.Elevation >= SelectedBottomLevel.Elevation
                        && level.Elevation <= SelectedTopLevel.Elevation
                    )
                    {
                        Levels.Add(level);
                    }
                }
                Levels.ForEach(i => CreatColumnWithIsDiv(i, selPoint, structuraltype));
            }
            else
            {
                CreatColumnWithIsDiv(SelectedBottomLevel, selPoint, structuraltype);
            }
        }

        /// <summary>
        /// 是否按层分割
        /// </summary>
        /// <param name="i"></param>
        /// <param name="selPoint"></param>
        /// <param name="structuraltype"></param>
        private void CreatColumnWithIsDiv(Level i, XYZ selPoint, StructuralType structuraltype)
        {
            SelectedSymbol.MySymbol.Activate();
            FamilyInstance newcolumn = doc.Create.NewFamilyInstance(
                selPoint,
                SelectedSymbol.MySymbol,
                i,
                structuraltype
            );
            ResetColumn(newcolumn, selPoint, i);
        }

        /// <summary>
        /// 偏移设置
        /// </summary>
        /// <param name="selPoint"></param>
        /// <returns></returns>
        private XYZ SetColumnPosition(XYZ selPoint)
        {
            var Xisnumber = double.TryParse(SelectedhorOffset, out double xoffset);
            var Yisnumber = double.TryParse(SelectedverOffset, out double yoffset);
            if (Xisnumber && Yisnumber)
            {
                selPoint +=
                    new XYZ(
                        UnitUtils.ConvertToInternalUnits(
                            xoffset,
                            DisplayUnitType.DUT_MILLIMETERS
                        ),
                        0,
                        0
                    )
                    + new XYZ(
                        0,
                        UnitUtils.ConvertToInternalUnits(
                            yoffset,
                            DisplayUnitType.DUT_MILLIMETERS
                        ),
                        0
                    );
                return selPoint;
            }
            Parameter parameter = SelectedSymbol.MySymbol.LookupParameter("宽度");
            if (parameter == null)
            {
                parameter = SelectedSymbol.MySymbol.LookupParameter("b");
            }

            switch (SelectedhorOffset)
            {
                case "中心对齐":
                    break;
                case "左对齐":

                    selPoint += new XYZ(parameter.AsDouble() / 2, 0, 0);
                    break;
                case "右对齐":

                    selPoint += new XYZ(-parameter.AsDouble() / 2, 0, 0);
                    break;
            }
            Parameter parameter2 = SelectedSymbol.MySymbol.LookupParameter("高度");
            if (parameter2 == null)
            {
                parameter2 = SelectedSymbol.MySymbol.LookupParameter("深度");
            }
            if (parameter2 == null)
            {
                parameter2 = SelectedSymbol.MySymbol.LookupParameter("h");
            }
            switch (SelectedverOffset)
            {
                case "中心对齐":
                    break;
                case "上对齐":
                    selPoint += new XYZ(0, -parameter2.AsDouble() / 2, 0);
                    break;
                case "下对齐":
                    selPoint += new XYZ(0, +parameter2.AsDouble() / 2, 0);
                    break;
            }
            return selPoint;
        }

        /// <summary>
        /// 柱顶、底标高设置，旋转设置
        /// </summary>
        /// <param name="newcolumn"></param>
        /// <param name="selPoint"></param>
        private void ResetColumn(FamilyInstance newcolumn, XYZ selPoint, Level i)
        {
            Parameter parameter1 = newcolumn.LookupParameter("顶部标高");
            if (SelectedTopLevel.Name == "当前标高之上一标高")
            {
                var level = allLevel.FirstOrDefault(i => i.Elevation == SelectedTopLevel.Elevation);
                parameter1.Set(level.Id);
            }
            else if (IsDivByLevel != true)
            {
                parameter1.Set(SelectedTopLevel.Id);
            }

            Parameter parameter2 = newcolumn.LookupParameter("底部标高");
            if (SelectedBottomLevel.Name == "当前标高")
            {
                var level = allLevel.FirstOrDefault(i =>
                    i.Elevation == SelectedBottomLevel.Elevation
                );

                parameter2.Set(level.Id);
            }
            else if (IsDivByLevel != true)
            {
                parameter2.Set(SelectedBottomLevel.Id);
            }
            else
            {
                parameter2.Set(i.Id);
            }
            ElementTransformUtils.RotateElement(
                doc,
                newcolumn.Id,
                Line.CreateBound(selPoint, selPoint + new XYZ(0, 0, 1)),
                Math.PI * (double.Parse(SelectedAngOffset)) / 180
            );
        }

        [RelayCommand]
        async Task DelLevel()
        {
            try
            {
                await RevitTask.RunAsync(() =>
                {
                    using (Transaction t = new Transaction(doc, "删除标高"))
                    {
                        t.Start();
                        List<Level> allLevel = new FilteredElementCollector(doc)
                            .OfClass(typeof(Level))
                            .Cast<Level>()
                            .ToList();
                        allLevel.ForEach(i =>
                        {
                            if (i.Name == "当前标高" || i.Name == "当前标高之上一标高")
                                doc.Delete(i.Id);
                        });
                        t.Commit();
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }
    }

    class GridFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem as Grid != null)
            {
                return true;
            }
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
