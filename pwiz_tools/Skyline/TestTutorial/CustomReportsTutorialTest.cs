﻿/*
 * Original author: Alana Killeen <killea .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2010 University of Washington - Seattle, WA
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Controls;
using pwiz.Common.DataBinding.Controls.Editor;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Controls.Databinding;
using pwiz.Skyline.Controls.Graphs;
using pwiz.Skyline.Controls.SeqNode;
using pwiz.Skyline.EditUI;
using pwiz.Skyline.FileUI;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Hibernate.Query;
using pwiz.Skyline.Properties;
using pwiz.Skyline.SettingsUI;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestTutorial
{
    /// <summary>
    /// Testing the tutorial for Skyline Custom Reports and Results Grid
    /// </summary>
    // ReSharper disable LocalizableElement
    [TestClass]
    public class CustomReportsTutorialTest : AbstractFunctionalTest
    {
        const string customReportName = "Overview";

        [TestMethod]
        public void TestCustomReportsTutorial()
        {
            // Set true to look at tutorial screenshots.
            // IsPauseForScreenShots = true;

            LinkPdf = "https://skyline.gs.washington.edu/labkey/_webdav/home/software/Skyline/%40files/tutorials/CustomReports-2_5.pdf";

            TestFilesZipPaths = new[]
            {
                @"https://skyline.gs.washington.edu/tutorials/CustomReports.zip",
                @"TestTutorial\CustomReportsViews.zip"
            };
            RunFunctionalTest();
        }

        // [TestMethod]  As needed.  This test takes a lot of time
        public void TestCustomReportsTutorialWithOldReports()
        {
            RunTestWithOldReports(TestCustomReportsTutorial);
        }

        private new TestFilesDir TestFilesDir
        {
            get { return TestFilesDirs[0]; }
        }

        protected override void DoTest()
        {
            // Data Overview, p. 2
            RunUI(() => SkylineWindow.OpenFile(TestFilesDir.GetTestPath(@"CustomReports\Study7_example.sky")));
                // Not L10N
            RunDlg<FindNodeDlg>(SkylineWindow.ShowFindNodeDlg, findPeptideDlg =>
            {
                findPeptideDlg.SearchString = "HGFLPR"; // Not L10N
                findPeptideDlg.FindNext();
                findPeptideDlg.Close();
            });
            RunUI(() =>
            {
                Assert.AreEqual("HGFLPR", SkylineWindow.SequenceTree.SelectedNode.Text); // Not L10N
                SkylineWindow.ShowPeakAreaReplicateComparison();
            });
            WaitForCondition(() => !SkylineWindow.GraphPeakArea.IsHidden);
            RunUI(() => SkylineWindow.ShowGraphPeakArea(false));
            WaitForCondition(() => SkylineWindow.GraphPeakArea.IsHidden);

            if (IsEnableLiveReports)
            {
                DoLiveReportsTest();
            }
            else
            {
                DoCustomReportsTest();
            }
        }

        protected void DoCustomReportsTest()
        {
            // Creating a Simple Custom Report, p. 3
            var exportReportDlg = ShowDialog<ExportReportDlg>(SkylineWindow.ShowExportReportDialog);
            PauseForScreenShot("Export Report form", 3, typeof(ExportReportDlg));

            // p. 4
            var editReportListDlg = ShowDialog<EditListDlg<SettingsListBase<ReportSpec>, ReportSpec>>(exportReportDlg.EditList);
            var pivotReportDlg = ShowDialog<PivotReportDlg>(editReportListDlg.AddItem);
            RunUI(() => pivotReportDlg.ReportName = customReportName);
            PauseForScreenShot("Edit Report form (old)", 4, typeof(PivotReportDlg));

            // p. 5
            RunUI(() =>
            {
                Assert.IsTrue(pivotReportDlg.TrySelect(new Identifier("Peptides", "Sequence"))); // Not L10N
                pivotReportDlg.AddSelectedColumn();
                Assert.AreEqual(1, pivotReportDlg.ColumnCount);
                var expectedFields = new[]
                {
                    // Not L10N
                     new Identifier("ProteinName"), new Identifier("ProteinDescription"), 
                     new Identifier("ProteinSequence"), new Identifier("ProteinNote"),
                     new Identifier("Results")
                };
                foreach (Identifier id in expectedFields)
                {
                    Assert.IsTrue(pivotReportDlg.TrySelect(id));
                }
            });
            PauseForScreenShot("Edit Report form (old)", 5, typeof(PivotReportDlg));

            // p. 6
            RunUI(() =>
            {
                var columnsToAdd = new[]
                { 
                    // Not L10N
                    new Identifier("Peptides", "Precursors", "IsotopeLabelType"),
                    new Identifier("Peptides", "Precursors", "PrecursorResults", "BestRetentionTime"),
                    new Identifier("Peptides", "Precursors", "PrecursorResults", "TotalArea") 
                };
                foreach(Identifier id in columnsToAdd)
                {
                    Assert.IsTrue(pivotReportDlg.TrySelect(id));
                    pivotReportDlg.AddSelectedColumn();
                }
                Assert.AreEqual(4, pivotReportDlg.ColumnCount);
                pivotReportDlg.PivotReplicate = true;
            });
            PauseForScreenShot("Edit Report form (old)", 7, typeof(PivotReportDlg));

            // p. 7
            {
                var previewReportDlg = ShowDialog<PreviewReportDlg>(pivotReportDlg.ShowPreview);
                RunUI(() =>
                {
                    Assert.AreEqual(20, previewReportDlg.RowCount);
                    Assert.AreEqual(58, previewReportDlg.ColumnCount);
                });
                PauseForScreenShot("Preview Report form (old)", 8, typeof(PreviewReportDlg));

                OkDialog(previewReportDlg, previewReportDlg.OkDialog);
            }

            // p. 8
            OkDialog(pivotReportDlg, pivotReportDlg.OkDialog);
            PauseForScreenShot("Export Reports form", 9, typeof(EditListDlg<SettingsListBase<ReportSpec>, ReportSpec>));

            OkDialog(editReportListDlg, editReportListDlg.OkDialog);
            PauseForScreenShot("Export Report form", 9, typeof(ExportReportDlg));

            OkDialog(exportReportDlg, exportReportDlg.CancelClick);

            // Exporting Report Data to a File, p. 9
            RunDlg<ExportReportDlg>(SkylineWindow.ShowExportReportDialog, exportReportDlg0 =>
            {
                exportReportDlg0.ReportName = customReportName; // Not L10N
                exportReportDlg0.OkDialog(TestFilesDir.GetTestPath("Overview_Study7_example.csv"), TextUtil.SEPARATOR_CSV); // Not L10N
            });
           
            // Sharing Report Templates, p. 9
            var exportReportDlg1 = ShowDialog<ExportReportDlg>(SkylineWindow.ShowExportReportDialog);

            // p. 10
            {
                var shareListDlg = ShowDialog<ShareListDlg<ReportSpecList, ReportSpec>>(exportReportDlg1.ShowShare);
                PauseForScreenShot("Share Report Definitions form", 11, typeof(ShareListDlg<ReportSpecList, ReportSpec>));

                RunUI(() => shareListDlg.ChosenNames = new[] { customReportName }); // Not L10N
                OkDialog(shareListDlg, () => shareListDlg.OkDialog(TestFilesDir.GetTestPath(@"CustomReports\Overview.skyr"))); // Not L10N
            }

            // Managing Report Templayes in Skyline, p. 10
            var editReportListDlg0 = ShowDialog<EditListDlg<SettingsListBase<ReportSpec>, ReportSpec>>(exportReportDlg1.EditList);
            RunUI(() => editReportListDlg0.SelectItem(customReportName));
            PauseForScreenShot("Edit Reports form", 11, typeof(EditListDlg<SettingsListBase<ReportSpec>, ReportSpec>));   // p. 11

            RunUI(() =>
            {
                editReportListDlg0.MoveItemDown();
                editReportListDlg0.MoveItemDown();
                var listReportSpecs = new List<ReportSpec>(editReportListDlg0.GetAllEdited());
                Assert.AreEqual(3, listReportSpecs.IndexOf(spec => spec.Name == customReportName)); // Not L10N
                editReportListDlg0.MoveItemUp();
                editReportListDlg0.MoveItemUp();
                editReportListDlg0.MoveItemUp();
                listReportSpecs = new List<ReportSpec>(editReportListDlg0.GetAllEdited());
                Assert.AreEqual(0, listReportSpecs.IndexOf(spec => spec.Name == customReportName)); // Not L10N
                editReportListDlg0.RemoveItem();
            });
            OkDialog(editReportListDlg0, editReportListDlg0.OkDialog);
            PauseForScreenShot("Export Report form", 13, typeof(ExportReportDlg));   // p. 13

            RunUI(() =>
            {
                exportReportDlg1.Import(TestFilesDir.GetTestPath(@"CustomReports\Overview.skyr")); // Not L10N
                exportReportDlg1.ReportName = customReportName; // Not L10N
            });
            RunDlg<PreviewReportDlg>(exportReportDlg1.ShowPreview, previewReportDlg =>
            {
                Assert.AreEqual(20, previewReportDlg.RowCount);
                Assert.AreEqual(58, previewReportDlg.ColumnCount);
                previewReportDlg.OkDialog();
            });

            // Modifying Existing Report Templates, p. 13
            var editReportListDlg1 = ShowDialog<EditListDlg<SettingsListBase<ReportSpec>, ReportSpec>>(exportReportDlg1.EditList);
            RunUI(() => editReportListDlg1.SelectItem(customReportName)); // Not L10N
            var pivotReportDlg0 = ShowDialog<PivotReportDlg>(editReportListDlg1.CopyItem);
            PauseForScreenShot("Edit Report form (old)", 14, typeof(PivotReportDlg));

            RunUI(() =>
            {
                pivotReportDlg0.ReportName = "Study 7"; // Not L10N
                // Not L10N
                var columnsToAdd = new[]
                                       {
                                           // Not L10N
                                           new Identifier("Results", "FileName"),
                                           new Identifier("Results", "SampleName"),
                                           new Identifier("Results", "ReplicateName"),
                                           new Identifier("ProteinName"),
                                           new Identifier("Peptides", "AverageMeasuredRetentionTime"),
                                           new Identifier("Peptides", "PeptideResults", "PeptideRetentionTime"),
                                           new Identifier("Peptides", "PeptideResults", "RatioToStandard"),
                                           new Identifier("Peptides", "Precursors", "Charge"),
                                           new Identifier("Peptides", "Precursors", "Mz"),
                                           new Identifier("Peptides", "Precursors", "Transitions", "ProductCharge"),
                                           new Identifier("Peptides", "Precursors", "Transitions", "ProductMz"),
                                           new Identifier("Peptides", "Precursors", "Transitions", "FragmentIon"),
                                           new Identifier("Peptides", "Precursors", "PrecursorResults", "MaxFwhm"),
                                           new Identifier("Peptides", "Precursors", "PrecursorResults", "MinStartTime"),
                                           new Identifier("Peptides", "Precursors", "PrecursorResults", "MaxEndTime"),
                                           new Identifier("Peptides", "Precursors", "Transitions", "TransitionResults", "RetentionTime"),
                                           new Identifier("Peptides", "Precursors", "Transitions", "TransitionResults", "Fwhm"),
                                           new Identifier("Peptides", "Precursors", "Transitions", "TransitionResults", "StartTime"),
                                           new Identifier("Peptides", "Precursors", "Transitions", "TransitionResults", "EndTime"),
                                           new Identifier("Peptides", "Precursors", "Transitions", "TransitionResults", "Area"),
                                           new Identifier("Peptides", "Precursors", "Transitions", "TransitionResults", "Height"),
                                           new Identifier("Peptides", "Precursors", "Transitions", "TransitionResults", "UserSetPeak")
                                       };
                foreach (Identifier id in columnsToAdd)
                {
                    Assert.IsTrue(pivotReportDlg0.TrySelect(id));
                    pivotReportDlg0.AddSelectedColumn();
                }
                pivotReportDlg0.PivotReplicate = false;
            });
            PauseForScreenShot("Edit Report form (old) expanded to show selected columns", 16, typeof(PivotReportDlg));

            int columnCount = 0;
            int rowCount = 0;
            {
                var previewReportDlg = ShowDialog<PreviewReportDlg>(pivotReportDlg0.ShowPreview);
                RunUI(() =>
                {
                    columnCount = previewReportDlg.ColumnCount;
                    rowCount = previewReportDlg.RowCount;
                });
                OkDialog(previewReportDlg, previewReportDlg.OkDialog);
            }
            RunUI(() => pivotReportDlg0.PivotIsotopeLabelType = true);
            {
                var previewReportDlg = ShowDialog<PreviewReportDlg>(pivotReportDlg0.ShowPreview);
                RunUI(() =>
                {
                    Assert.IsTrue(previewReportDlg.ColumnCount > columnCount);
                    Assert.AreEqual(rowCount / 2, previewReportDlg.RowCount);
                });
                PauseForScreenShot("Preview Report form (old)", 17, typeof(PreviewReportDlg));
                OkDialog(previewReportDlg, previewReportDlg.OkDialog);
            }
            RunUI(() =>
            {
                pivotReportDlg0.RemoveColumn("IsotopeLabelType"); // Not L10N
                pivotReportDlg0.OkDialog();
                editReportListDlg1.OkDialog();
            });
            PauseForScreenShot("Export Report form", 18, typeof(ExportReportDlg));

            OkDialog(exportReportDlg1, exportReportDlg1.CancelClick);

            // Quality Control Summary Reports, p. 18
            RunUI(() =>
                      {
                          SkylineWindow.OpenFile(TestFilesDir.GetTestPath(@"CustomReports\study9pilot.sky")); // Not L10N
                          SkylineWindow.ExpandPeptides();
                      });
            var exportReportDlg2 = ShowDialog<ExportReportDlg>(SkylineWindow.ShowExportReportDialog);
            RunUI(() => exportReportDlg2.Import(TestFilesDir.GetTestPath(@"CustomReports\Summary_stats.skyr"))); // Not L10N
            PauseForScreenShot("Export Report form (old)", 19, typeof(ExportReportDlg));

            var editReportListDlg2 = ShowDialog<EditListDlg<SettingsListBase<ReportSpec>, ReportSpec>>(exportReportDlg2.EditList);
            RunUI(() => editReportListDlg2.SelectItem("Summary Statistics")); // Not L10N
            var pivotReportDlg1 = ShowDialog<PivotReportDlg>(editReportListDlg2.EditItem);
            RunUI(() => Assert.AreEqual(11, pivotReportDlg1.ColumnCount));
            PauseForScreenShot("Edit Report form (old)", null, typeof(PivotReportDlg));

            {
                var previewReportDlg = ShowDialog<PreviewReportDlg>(pivotReportDlg1.ShowPreview);
                PauseForScreenShot("Preview Report form (old)", 20, typeof(PreviewReportDlg));

                OkDialog(previewReportDlg, previewReportDlg.OkDialog);
            }
            RunUI(() =>
            {
                pivotReportDlg1.OkDialog();
                editReportListDlg2.OkDialog();
            });
            OkDialog(exportReportDlg2, exportReportDlg2.CancelClick);
            RunDlg<FindNodeDlg>(SkylineWindow.ShowFindNodeDlg, findPeptideDlg =>
            {
                findPeptideDlg.SearchString = "INDISHTQSVSAK"; // Not L10N
                findPeptideDlg.FindNext();
                findPeptideDlg.Close();
            });
            RunUI(SkylineWindow.ShowPeakAreaReplicateComparison);
            WaitForGraphs();
            PauseForScreenShot("Peak Areas view", 24, typeof(AreaGraphController));   // Consider multiple view for Peak Areas and Retention Times

            // Results Grid View, p. 25
            var resultsGridForm = ShowDialog<ResultsGridForm>(() => SkylineWindow.ShowResultsGrid(true));
            PauseForScreenShot("Results Grid over main window (old)", 25, typeof(ResultsGridForm));

            ResultsGrid resultsGrid = null;
            RunUI(() =>
            {
                resultsGrid = resultsGridForm.ResultsGrid;
                SkylineWindow.AutoZoomBestPeak();
                SkylineWindow.SelectedPath = ((SrmTreeNode)SkylineWindow.SequenceTree.SelectedNode.Nodes[0]).Path;
            });
            WaitForGraphs();
            PauseForScreenShot("Main window layout (old)", 26);

            RunUI(() =>
                      {
                          resultsGrid.CurrentCell = resultsGrid.Rows[0].Cells["PrecursorReplicateNote"];
                          resultsGrid.BeginEdit(true);
// ReSharper disable LocalizableElement
                          resultsGrid.EditingControl.Text = "Low signal";   // Not L10N
// ReSharper restore LocalizableElement
                          resultsGrid.EndEdit();
                          resultsGrid.CurrentCell = resultsGrid.Rows[1].Cells[resultsGrid.CurrentCell.ColumnIndex];
                      });
            WaitForGraphs();
            RunUI(() => SkylineWindow.SelectedResultsIndex = 1);
            WaitForGraphs();

            RunDlg<ColumnChooser>(resultsGridForm.ChooseColumns, columnChooser =>
            {
                columnChooser.SetChecked(new Dictionary<string, bool>
                                             {
                                                 // Not L10N
                                                 {"Min Start Time", false},
                                                 {"Max End Time", false},
                                                 {"Library Dot Product", false},
                                                 {"Total Background", false},
                                                 {"Total Area Ratio", false}
                                             });
                columnChooser.DialogResult = DialogResult.OK;
            });
            PauseForScreenShot("Main window with fewer columns in Results Grid (old)");   // p. 24

            RunUI(() => SkylineWindow.SelectedNode.Expand());

            // Custom Annotations, p. 25
            var chooseAnnotationsDlg = ShowDialog<ChooseAnnotationsDlg>(SkylineWindow.ShowAnnotationsDialog);
            var editListDlg = ShowDialog<EditListDlg<SettingsListBase<AnnotationDef>, AnnotationDef>>(chooseAnnotationsDlg.EditList);
            var defineAnnotationDlg = ShowDialog<DefineAnnotationDlg>(editListDlg.AddItem);
            RunUI(() =>
            {
                defineAnnotationDlg.AnnotationName = "Tailing"; // Not L10N
                defineAnnotationDlg.AnnotationType = AnnotationDef.AnnotationType.true_false;
                defineAnnotationDlg.AnnotationTargets = AnnotationDef.AnnotationTargetSet.Singleton(AnnotationDef.AnnotationTarget.precursor_result);
            });
            PauseForScreenShot("Define Annotation form", 28, typeof(DefineAnnotationDlg));   // p. 25

            OkDialog(defineAnnotationDlg, defineAnnotationDlg.OkDialog);
            OkDialog(editListDlg, editListDlg.OkDialog);
            RunUI(() => chooseAnnotationsDlg.AnnotationsCheckedListBox.SetItemChecked(0, true));
            PauseForScreenShot("Annotation Settings form", 29, typeof(ChooseAnnotationsDlg));   // p. 26

            OkDialog(chooseAnnotationsDlg, chooseAnnotationsDlg.OkDialog);

            FindNode((564.7746).ToString(LocalizationHelper.CurrentCulture) + "++");
            WaitForGraphs();
            RunUI(() =>
                      {
                          var colTailing = resultsGrid.Columns[AnnotationDef.GetColumnName("Tailing")];
                          Assert.IsNotNull(colTailing);
                          Assert.AreEqual(typeof(bool), colTailing.ValueType);
                          var colNote = resultsGrid.Columns["PrecursorReplicateNote"];
                          Assert.IsNotNull(colNote);
                          colTailing.DisplayIndex = colNote.DisplayIndex + 1;
                      });
            PauseForScreenShot("Main window with Results Grid showing Tailing column (old)", 31);   // p. 27
        }

        protected void DoLiveReportsTest()
        {
            DoCreatingASimpleReport();
            DoExportingReportDataToAFile();
            DoSharingManagingModifyingReportTemplates();
            DoQualityControlSummaryReports();
            DoResultsGridView();
        }


        protected void DoCreatingASimpleReport()
        {
            // Creating a Simple Custom Report, p. 3
            var exportReportDlg = ShowDialog<ExportLiveReportDlg>(SkylineWindow.ShowExportReportDialog);
            PauseForScreenShot("Export Report form", 3, typeof(ExportLiveReportDlg));

            // p. 4
            var editReportListDlg = ShowDialog<EditListDlg<SettingsListBase<ReportOrViewSpec>, ReportOrViewSpec>>(exportReportDlg.EditList);
            var viewEditor = ShowDialog<ViewEditor>(editReportListDlg.AddItem);
            RunUI(() => viewEditor.ViewName = customReportName);
            PauseForScreenShot("Edit Report form", 4, typeof(ViewEditor));

            // p. 5
            RunUI(() =>
            {
                viewEditor.ChooseColumnsTab.ExpandPropertyPath(PropertyPath.Parse("Proteins!*.Peptides!*"), true);
                viewEditor.ChooseColumnsTab.ExpandPropertyPath(PropertyPath.Parse("Replicates!*"), true);
                // Make the view editor bigger so that these expanded nodes can be seen in the next screenshot
                viewEditor.Height = Math.Max(viewEditor.Height, 600);
                Assert.IsTrue(viewEditor.ChooseColumnsTab.TrySelect(PropertyPath.Parse("Proteins!*.Peptides!*.Sequence"))); // Not L10N
                viewEditor.ChooseColumnsTab.AddSelectedColumn();
            });
            PauseForScreenShot("Edit Report form", 5, typeof(ViewEditor));

            // p. 6
            RunUI(() =>
            {
                var columnsToAdd = new[]
                { 
                    // Not L10N
                    PropertyPath.Parse("Proteins!*.Peptides!*.Precursors!*.IsotopeLabelType"),
                    PropertyPath.Parse("Proteins!*.Peptides!*.Precursors!*.Results!*.Value.BestRetentionTime"),
                    PropertyPath.Parse("Proteins!*.Peptides!*.Precursors!*.Results!*.Value.TotalArea"),
                };
                foreach (var id in columnsToAdd)
                {
                    Assert.IsTrue(viewEditor.ChooseColumnsTab.TrySelect(id), "Unable to select {0}", id);
                    viewEditor.ChooseColumnsTab.AddSelectedColumn();
                }
                Assert.AreEqual(4, viewEditor.ChooseColumnsTab.ColumnCount);
                viewEditor.ViewEditorWidgets.OfType<PivotReplicateAndIsotopeLabelWidget>().First().SetPivotReplicate(true);
            });
            PauseForScreenShot("Edit Report form", 7, typeof(ViewEditor));
            // p. 7
            {
                var previewReportDlg = ShowDialog<DocumentGridForm>(viewEditor.ShowPreview);
                WaitForConditionUI(() => previewReportDlg.IsComplete);
                RunUI(() =>
                {
                    Assert.AreEqual(20, previewReportDlg.RowCount);
                    Assert.AreEqual(58, previewReportDlg.ColumnCount);
                });
                PauseForScreenShot("Preview form", 8, typeof(DocumentGridForm));

                OkDialog(previewReportDlg, previewReportDlg.Close);
            }

            // p. 8
            OkDialog(viewEditor, viewEditor.OkDialog);
            PauseForScreenShot("Edit Reports form", 9, typeof(EditListDlg<SettingsListBase<ReportOrViewSpec>, ReportOrViewSpec>));

            OkDialog(editReportListDlg, editReportListDlg.OkDialog);
            PauseForScreenShot("Export Report form", 9, typeof(ExportLiveReportDlg));

            OkDialog(exportReportDlg, exportReportDlg.CancelClick);
        }

        protected void DoExportingReportDataToAFile()
        {
            // Exporting Report Data to a File, p. 9
            RunDlg<ExportLiveReportDlg>(SkylineWindow.ShowExportReportDialog, exportReportDlg0 =>
            {
                exportReportDlg0.ReportName = customReportName; // Not L10N
                exportReportDlg0.OkDialog(TestFilesDir.GetTestPath("Overview_Study7_example.csv"), TextUtil.SEPARATOR_CSV); // Not L10N
            });
        }

        protected void DoSharingManagingModifyingReportTemplates()
        {
            // Sharing Report Templates, p. 9
            var exportReportDlg1 = ShowDialog<ExportLiveReportDlg>(SkylineWindow.ShowExportReportDialog);

            // p. 10
            {
                var shareListDlg = ShowDialog<ShareListDlg<ReportOrViewSpecList, ReportOrViewSpec>>(exportReportDlg1.ShowShare);
                PauseForScreenShot("Save Report Definitions form", 11, typeof(ShareListDlg<ReportOrViewSpecList, ReportOrViewSpec>));

                RunUI(() => shareListDlg.ChosenNames = new[] { customReportName }); // Not L10N
                OkDialog(shareListDlg, () => shareListDlg.OkDialog(TestFilesDir.GetTestPath(@"CustomReports\Overview.skyr"))); // Not L10N
            }

            // Managing Report Templayes in Skyline, p. 10
            var editReportListDlg0 = ShowDialog<EditListDlg<SettingsListBase<ReportOrViewSpec>, ReportOrViewSpec>>(exportReportDlg1.EditList);
            RunUI(() => editReportListDlg0.SelectItem(customReportName));
            PauseForScreenShot("Edit Reports form", 12, typeof(EditListDlg<SettingsListBase<ReportOrViewSpec>, ReportOrViewSpec>));   // p. 11

            RunUI(() =>
            {
                editReportListDlg0.MoveItemDown();
                editReportListDlg0.MoveItemDown();
                var listReportSpecs = new List<ReportOrViewSpec>(editReportListDlg0.GetAllEdited());
                Assert.AreEqual(3, listReportSpecs.IndexOf(spec => spec.Name == customReportName)); // Not L10N
                editReportListDlg0.MoveItemUp();
                editReportListDlg0.MoveItemUp();
                editReportListDlg0.MoveItemUp();
                listReportSpecs = new List<ReportOrViewSpec>(editReportListDlg0.GetAllEdited());
                Assert.AreEqual(0, listReportSpecs.IndexOf(spec => spec.Name == customReportName)); // Not L10N
                editReportListDlg0.RemoveItem();
            });
            OkDialog(editReportListDlg0, editReportListDlg0.OkDialog);
            PauseForScreenShot("Export Report form", 13, typeof(ExportLiveReportDlg));

            RunUI(() =>
            {
                exportReportDlg1.Import(TestFilesDir.GetTestPath(@"CustomReports\Overview.skyr")); // Not L10N
                exportReportDlg1.ReportName = customReportName; // Not L10N
            });
            var previewDlg = ShowDialog<DocumentGridForm>(exportReportDlg1.ShowPreview);
            WaitForCondition(() => previewDlg.RowCount == 20);
            Assert.AreEqual(20, previewDlg.RowCount);
            Assert.AreEqual(58, previewDlg.ColumnCount);
            RunUI(previewDlg.Close);

            // Modifying Existing Report Templates, p. 13
            var editReportListDlg1 = ShowDialog<EditListDlg<SettingsListBase<ReportOrViewSpec>, ReportOrViewSpec>>(exportReportDlg1.EditList);
            RunUI(() => editReportListDlg1.SelectItem(customReportName)); // Not L10N
            var viewEditor = ShowDialog<ViewEditor>(editReportListDlg1.CopyItem);
            PauseForScreenShot("Edit Report form", 14, typeof(ViewEditor));

            RunUI(() =>
            {
                viewEditor.ViewName = "Study 7"; // Not L10N
                // Not L10N
                var columnsToAdd = new[]
                                       {
                                           // Not L10N
                                           PropertyPath.Parse("Replicates!*.Files!*.FileName"),
                                           PropertyPath.Parse("Replicates!*.Files!*.SampleName"),
                                           PropertyPath.Parse("Replicates!*.Name"),
                                           PropertyPath.Parse("Proteins!*.Name"),
                                           PropertyPath.Parse("Proteins!*.Peptides!*.AverageMeasuredRetentionTime"),
                                           PropertyPath.Parse("Proteins!*.Peptides!*.Results!*.Value.PeptideRetentionTime"),
                                           PropertyPath.Parse("Proteins!*.Peptides!*.Results!*.Value.RatioToStandard"),
                                           PropertyPath.Parse("Proteins!*.Peptides!*.Precursors!*.Charge"),
                                           PropertyPath.Parse("Proteins!*.Peptides!*.Precursors!*.Mz"),
                                           PropertyPath.Parse("Proteins!*.Peptides!*.Precursors!*.Transitions!*.ProductCharge"),
                                           PropertyPath.Parse("Proteins!*.Peptides!*.Precursors!*.Transitions!*.ProductMz"),
                                           PropertyPath.Parse("Proteins!*.Peptides!*.Precursors!*.Transitions!*.FragmentIon"),
                                           PropertyPath.Parse("Proteins!*.Peptides!*.Precursors!*.Results!*.Value.MaxFwhm"),
                                           PropertyPath.Parse("Proteins!*.Peptides!*.Precursors!*.Results!*.Value.MinStartTime"),
                                           PropertyPath.Parse("Proteins!*.Peptides!*.Precursors!*.Results!*.Value.MaxEndTime"),
                                           PropertyPath.Parse("Proteins!*.Peptides!*.Precursors!*.Transitions!*.Results!*.Value.RetentionTime"),
                                           PropertyPath.Parse("Proteins!*.Peptides!*.Precursors!*.Transitions!*.Results!*.Value.Fwhm"),
                                           PropertyPath.Parse("Proteins!*.Peptides!*.Precursors!*.Transitions!*.Results!*.Value.StartTime"),
                                           PropertyPath.Parse("Proteins!*.Peptides!*.Precursors!*.Transitions!*.Results!*.Value.EndTime"),
                                           PropertyPath.Parse("Proteins!*.Peptides!*.Precursors!*.Transitions!*.Results!*.Value.Area"),
                                           PropertyPath.Parse("Proteins!*.Peptides!*.Precursors!*.Transitions!*.Results!*.Value.Height"),
                                           PropertyPath.Parse("Proteins!*.Peptides!*.Precursors!*.Transitions!*.Results!*.Value.UserSetPeak"),
                                       };
                foreach (var id in columnsToAdd)
                {
                    Assert.IsTrue(viewEditor.ChooseColumnsTab.TrySelect(id), "Unable to select {0}", id);
                    viewEditor.ChooseColumnsTab.AddSelectedColumn();
                }
                var pivotWidget = viewEditor.ViewEditorWidgets.OfType<PivotReplicateAndIsotopeLabelWidget>().First();
                pivotWidget.SetPivotReplicate(false);
                viewEditor.Height = Math.Max(viewEditor.Height, 610);
            });
            PauseForScreenShot("Edit Report form expanded to show selected columns", 16, typeof(ViewEditor));

            int columnCount = 0;
            int rowCount = 0;
            {
                var previewReportDlg = ShowDialog<DocumentGridForm>(viewEditor.ShowPreview);
                WaitForCondition(() => previewReportDlg.ColumnCount > 0);
                RunUI(() =>
                {
                    columnCount = previewReportDlg.ColumnCount;
                    rowCount = previewReportDlg.RowCount;
                });
                OkDialog(previewReportDlg, previewReportDlg.Close);
            }
            RunUI(() => viewEditor.ViewEditorWidgets.OfType<PivotReplicateAndIsotopeLabelWidget>().First().SetPivotIsotopeLabel(true));
            {
                var previewReportDlg = ShowDialog<DocumentGridForm>(viewEditor.ShowPreview);
                WaitForCondition(() => previewReportDlg.ColumnCount > 0);
                RunUI(() =>
                {
                    Assert.IsTrue(previewReportDlg.ColumnCount > columnCount);
                    Assert.AreEqual(rowCount / 2, previewReportDlg.RowCount);
                });
                PauseForScreenShot("Adjust the scrollbar so that the first displayed column is \"light Height\" and the last displayed column is \"heavy Product Mz\"", 17, typeof(DocumentGridForm));
                OkDialog(previewReportDlg, previewReportDlg.Close);
            }
            RunUI(() =>
            {
                viewEditor.ChooseColumnsTab.RemoveColumn(PropertyPath.Parse("IsotopeLabelType")); // Not L10N
                viewEditor.OkDialog();
                editReportListDlg1.OkDialog();
            });
            PauseForScreenShot("Export Report form", 18, typeof(ExportLiveReportDlg));

            OkDialog(exportReportDlg1, exportReportDlg1.CancelClick);
        }
        protected void DoQualityControlSummaryReports()
        {
            // Quality Control Summary Reports, p. 18
            RunUI(() =>
            {
                SkylineWindow.OpenFile(TestFilesDir.GetTestPath(@"CustomReports\study9pilot.sky")); // Not L10N
                SkylineWindow.ExpandPeptides();
            });
            RunUI(() => SkylineWindow.ShowDocumentGrid(true));
            RestoreViewOnScreen(TestFilesDirs[1].GetTestPath(@"WideSummaryStatistics.view"));
            var documentGridForm = FindOpenForm<DocumentGridForm>();
            var manageViewsForm = ShowDialog<ManageViewsForm>(documentGridForm.ManageViews);
            RunUI(() =>
            {
                Assert.AreEqual(1, ((SkylineViewContext)documentGridForm.BindingListSource.ViewContext)
                    .ImportViewsFromFile(TestFilesDir.GetTestPath(@"CustomReports\Summary_stats.skyr")));
                manageViewsForm.RefreshUi(true);
            });
            PauseForScreenShot("Manage Views form", 19, typeof(ManageViewsForm));
            OkDialog(manageViewsForm, manageViewsForm.Close);
            PauseForScreenShot("Click the views dropdown and highlight 'Summary_stats'", 20, typeof(DocumentGridForm));

            RunUI(() => documentGridForm.ChooseView("Summary Statistics"));
            WaitForConditionUI(() => documentGridForm.IsComplete);
            PauseForScreenShot("Document Grid with summary statistics", 20, typeof(DocumentGridForm));

            var viewEditor = ShowDialog<ViewEditor>(documentGridForm.NavBar.CustomizeView);
            RunUI(() => Assert.AreEqual(11, viewEditor.ChooseColumnsTab.ColumnCount));
            RunUI(() =>
            {
                int indexCvTotalArea =
                    viewEditor.ChooseColumnsTab.ColumnNames.ToList().IndexOf("CvTotalArea");
                Assert.IsFalse(indexCvTotalArea < 0, "{0} < 0", indexCvTotalArea);
                viewEditor.ChooseColumnsTab.ActivateColumn(indexCvTotalArea);
            });
            PauseForScreenShot("Customize View forrm", 21, typeof(ViewEditor));
            RunUI(()=>viewEditor.TabControl.SelectTab(1));
            RunUI(() =>
            {
                viewEditor.FilterTab.AddSelectedColumn();
                Assert.IsTrue(viewEditor.FilterTab.SetFilterOperation(0, FilterOperations.OP_IS_GREATER_THAN));
                viewEditor.FilterTab.SetFilterOperand(0, ".2");
            });
            PauseForScreenShot("Customize View - Filter tab", 22, typeof(ViewEditor));
            RunUI(viewEditor.OkDialog);
            PauseForScreenShot("Document Grid filtered", 23, typeof(DocumentGridForm));
            RunUI(documentGridForm.Close);
            RunDlg<FindNodeDlg>(SkylineWindow.ShowFindNodeDlg, findPeptideDlg =>
            {
                findPeptideDlg.SearchString = "INDISHTQSVSAK"; // Not L10N
                findPeptideDlg.FindNext();
                findPeptideDlg.Close();
            });
            PauseForScreenShot("Highlight the menu item 'View>Peak Areas>Replicate Comparison'", 23);
            RunUI(SkylineWindow.ShowPeakAreaReplicateComparison);
            WaitForGraphs();
            PauseForScreenShot("Peak Areas view", 24, typeof(AreaGraphController));
        }
        protected void DoResultsGridView()
        {
            // Results Grid View, p. 22
            RestoreViewOnScreen(TestFilesDirs[1].GetTestPath("ResultsGridInFrontOfPeakAreas.view"));
            PauseForScreenShot("Take full screen capture of floating windows", 25, typeof(LiveResultsGrid));
            RestoreViewOnScreen(TestFilesDirs[1].GetTestPath("DockedResultsGridAndPeakAreas.view"));
            PauseForScreenShot("Main window layout", 26);

            var resultsGridForm = ShowDialog<LiveResultsGrid>(() => SkylineWindow.ShowResultsGrid(true));
            BoundDataGridView resultsGrid = null;
            RunUI(() =>
            {
                resultsGrid = resultsGridForm.DataGridView;
                SkylineWindow.AutoZoomBestPeak();
                SkylineWindow.SelectedPath = ((SrmTreeNode)SkylineWindow.SequenceTree.SelectedNode.Nodes[0]).Path;
            });
            WaitForGraphs();
            PauseForScreenShot("Results Grid view subsection", 27);

            RunUI(() =>
            {
                var precursorNoteColumn =
                    resultsGrid.Columns.Cast<DataGridViewColumn>()
                        .First(col => "PrecursorReplicateNote" == col.HeaderText);
                resultsGrid.CurrentCell = resultsGrid.Rows[0].Cells[precursorNoteColumn.Index];
                resultsGrid.BeginEdit(true);
                // ReSharper disable LocalizableElement
                resultsGrid.EditingControl.Text = "Low signal";   // Not L10N
                // ReSharper restore LocalizableElement
                resultsGrid.EndEdit();
                resultsGrid.CurrentCell = resultsGrid.Rows[1].Cells[resultsGrid.CurrentCell.ColumnIndex];
            });
            WaitForGraphs();
            RunUI(() => SkylineWindow.SelectedResultsIndex = 1);
            WaitForGraphs();

            RunDlg<ViewEditor>(resultsGridForm.NavBar.CustomizeView, resultsGridViewEditor =>
            {
                var chooseColumnTab = resultsGridViewEditor.ChooseColumnsTab;
                foreach (var column in chooseColumnTab.GetSelectedColumns())
                {
                    chooseColumnTab.RemoveColumn(column.PropertyPath);
                }
                foreach (
                    var column in
                        new[]
                        {
                            PropertyPath.Parse("MinStartTime"), PropertyPath.Parse("MaxEndTime"),
                            PropertyPath.Parse("LibraryDotProduct"), PropertyPath.Parse("TotalBackground"),
                            PropertyPath.Parse("TotalAreaRatio")
                        })
                {
                    Assert.IsTrue(chooseColumnTab.TrySelect(column), "Unable to select {0}", column);
                    chooseColumnTab.AddSelectedColumn();
                }
                resultsGridViewEditor.DialogResult = DialogResult.OK;
            });
            PauseForScreenShot("Results grid with fewer columns (missing?)");  // No longer in tutorial?

            RunUI(() => SkylineWindow.SelectedNode.Expand());

            // Custom Annotations, p. 25
            var chooseAnnotationsDlg = ShowDialog<ChooseAnnotationsDlg>(SkylineWindow.ShowAnnotationsDialog);
            var editListDlg = ShowDialog<EditListDlg<SettingsListBase<AnnotationDef>, AnnotationDef>>(chooseAnnotationsDlg.EditList);
            var defineAnnotationDlg = ShowDialog<DefineAnnotationDlg>(editListDlg.AddItem);
            RunUI(() =>
            {
                defineAnnotationDlg.AnnotationName = "Tailing"; // Not L10N
                defineAnnotationDlg.AnnotationType = AnnotationDef.AnnotationType.true_false;
                defineAnnotationDlg.AnnotationTargets = AnnotationDef.AnnotationTargetSet.Singleton(AnnotationDef.AnnotationTarget.precursor_result);
            });
            PauseForScreenShot("Define Annotation form", 28, typeof(DefineAnnotationDlg));

            OkDialog(defineAnnotationDlg, defineAnnotationDlg.OkDialog);
            OkDialog(editListDlg, editListDlg.OkDialog);
            RunUI(() => chooseAnnotationsDlg.AnnotationsCheckedListBox.SetItemChecked(0, true));
            PauseForScreenShot("Annotation Settings form", 29, typeof(ChooseAnnotationsDlg));   // p. 26

            OkDialog(chooseAnnotationsDlg, chooseAnnotationsDlg.OkDialog);

            FindNode((564.7746).ToString(LocalizationHelper.CurrentCulture) + "++");
            var liveResultsGrid = FindOpenForm<LiveResultsGrid>();
            ViewEditor viewEditor = ShowDialog<ViewEditor>(liveResultsGrid.NavBar.CustomizeView);
            RunUI(() =>
            {
                viewEditor.ChooseColumnsTab.ActivateColumn(2);
                Assert.IsTrue(viewEditor.ChooseColumnsTab.TrySelect(
                    PropertyPath.Root.Property(AnnotationDef.ANNOTATION_PREFIX + "Tailing")));
                viewEditor.ChooseColumnsTab.AddSelectedColumn();
            });
            PauseForScreenShot("Customize View form showing Tailing annotation checked", 30, typeof(ViewEditor));
            OkDialog(viewEditor, viewEditor.OkDialog);
            PauseForScreenShot("Main window with Tailing column added to Results Grid");   // p. 27
        }
    }
}
