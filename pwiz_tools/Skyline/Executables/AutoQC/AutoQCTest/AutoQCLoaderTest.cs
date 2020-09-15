using AutoQC;
using AutoQC.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.SkylineTestUtil;

namespace AutoQCTest
{
    [TestClass]
     public class AutoQcLoaderTest : AbstractAutoQcFunctionalTest
    {
         [TestMethod]
         public void CreateConfigsTest()
         {
             TestFilesZip = @"AutoQCTest\AutoQCLoaderTest.zip";
             RunFunctionalTest();
         }

         protected override void DoTest()
         {
             var mainWindow = MainFormWindow();
             var mainForm = mainWindow as MainForm;
             Assert.IsNotNull(mainForm, "Main program window is not an instance of MainForm.");
             Assert.AreEqual(0, mainForm.ConfigCount());

             var newConfigForm = ShowDialog<AutoQcConfigForm>(() => mainForm.ShowNewConfigForm());

             RunUI(() =>
             {
                 PopulateNewConfigForm(newConfigForm);
                 newConfigForm.Save();
             });

             WaitForClosedForm(newConfigForm);
             Assert.AreEqual(1, mainForm.ConfigCount());
             
             var newConfigForm2 = ShowDialog<AutoQcConfigForm>(() => Program.MainWindow.ShowNewConfigForm());
             RunUI(() =>
             {
                 PopulateNewConfigForm(newConfigForm2);
             });

             RunDlg<AlertDlg>(() => newConfigForm2.Save(),
                 dlg =>
                 {
                     Assert.AreEqual(Resources.AutoQcConfigForm_ValidateConfigName_A_configuration_with_this_name_already_exists_,
                         dlg.Message);
                     dlg.OkDialog();
                 });

             RunUI(() => { newConfigForm2.CancelButton.PerformClick(); });
             WaitForClosedForm(newConfigForm2);
         }
        

         private void PopulateNewConfigForm(AutoQcConfigForm newConfigForm)
         {
             var testFilesDir = new TestFilesDir(TestContext, TestFilesZip);
             newConfigForm.ConfigName = @"This is a test config";
             newConfigForm.SkylineFilePath = testFilesDir.GetTestPath(@"QEP_2015_0424_RJ.sky");
             newConfigForm.FolderToWatch = testFilesDir.FullPath;
         }
     }
}