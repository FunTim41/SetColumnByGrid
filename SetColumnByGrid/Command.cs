using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Revit.Async;

namespace SetColumnByGrid
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        UIApplication uiapp;
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements
        )
        {
            try
            {
                 uiapp = commandData.Application;
                //uiapp.DialogBoxShowing += Uiapp_DialogBoxShowing;
                
                RevitTask.Initialize(uiapp);
                MainView mainView = new MainView(commandData);
                mainView.Show();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Tip", ex.Message);
                return Result.Failed;
            }
        }

        //private void Uiapp_DialogBoxShowing(object sender, DialogBoxShowingEventArgs e)
        //{
        //    try
        //    {
        //        var istrue =e.OverrideResult(1);
        //        if (istrue)
        //        {
        //            MessageBox.Show("666");
        //        }
        //        uiapp.DialogBoxShowing -= Uiapp_DialogBoxShowing;

        //    }
        //    catch (Exception ex)
        //    {

        //        TaskDialog.Show("Tip", ex.Message);
        //        return;
        //    }

        //}
    }
}
