using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using AutoQC;
using AutoQC.Properties;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.Controls;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Util;
using pwiz.SkylineTestUtil;

namespace AutoQCTest
{
    /// <summary>
    /// All functional tests MUST derive from this base class.
    /// </summary>
    public abstract class AbstractAutoQcFunctionalTest
    {
        private const int SLEEP_INTERVAL = 100;
        public const int WAIT_TIME = 60 * 1000;    // 60 seconds
        
        private bool _testCompleted;

        public static MainForm MainWindow => Program.MainWindow;
        protected static bool LaunchDebuggerOnWaitForConditionTimeout { get; set; } // Use with caution - this will prevent scheduled tests from completing, so we can investigate a problem

        protected abstract void DoTest();

        /// <summary>
        /// Starts up AutoQCLoader, and runs the <see cref="AbstractFunctionalTest.DoTest"/> test method.
        /// </summary>
        protected void RunFunctionalTest()
        {
            // bool firstTry = true;
            // Be prepared to re-run test in the event that a previously downloaded data file is damaged or stale
            for (; ; )
            {
                try
                {
                    RunFunctionalTestOrThrow();
                }
                catch (Exception x)
                {
                    Program.AddTestException(x);
                }

                // Delete unzipped test files.
                if (TestFilesDirs != null)
                {
                    foreach (TestFilesDir dir in TestFilesDirs)
                    {
                        try
                        {
                            dir?.Dispose();
                        }
                        catch (Exception x)
                        {
                            Program.AddTestException(x);
                            FileStreamManager.Default.CloseAllStreams();
                        }
                    }
                }

                // if (firstTry && Program.TestExceptions.Count > 0 && RetryDataDownloads)
                // {
                //     try
                //     {
                //         if (FreshenTestDataDownloads())
                //         {
                //             firstTry = false;
                //             Program.TestExceptions.Clear();
                //             continue;
                //         }
                //     }
                //     catch (Exception xx)
                //     {
                //         Program.AddTestException(xx); // Some trouble with data download, make a note of it
                //     }
                // }


                if (Program.TestExceptions.Count > 0)
                {
                    //Log<AbstractFunctionalTest>.Exception(@"Functional test exception", Program.TestExceptions[0]);
                    const string errorSeparator = "------------------------------------------------------";
                    Assert.Fail("{0}{1}{2}{3}",
                        Environment.NewLine + Environment.NewLine,
                        errorSeparator + Environment.NewLine,
                        Program.TestExceptions[0],
                        Environment.NewLine + errorSeparator + Environment.NewLine);
                }
                break;
            }

            if (!_testCompleted)
            {
                //Log<AbstractFunctionalTest>.Fail(@"Functional test did not complete");
                Assert.Fail("Functional test did not complete");
            }
        }

        protected void RunFunctionalTestOrThrow()
        {
            Program.FunctionalTest = true;
            Program.TestExceptions = new List<Exception>();
            LocalizationHelper.InitThread();

            // Unzip test files.
            if (TestFilesZipPaths != null)
            {
                TestFilesDirs = new TestFilesDir[TestFilesZipPaths.Length];
                for (int i = 0; i < TestFilesZipPaths.Length; i++)
                {
                    TestFilesDirs[i] = new TestFilesDir(TestContext, TestFilesZipPaths[i], TestDirectoryName,
                        TestFilesPersistent, IsExtractHere(i));
                }
            }

            // Run test in new thread (Skyline on main thread).
            // Program.Init();
            InitializeAutoQcSettings();

            var threadTest = new Thread(WaitForMainWindow) { Name = @"Functional test thread" };
            LocalizationHelper.InitThread(threadTest);
            threadTest.Start();
            Program.Main();
            threadTest.Join();

            // Were all windows disposed?
            FormEx.CheckAllFormsDisposed();
            CommonFormEx.CheckAllFormsDisposed();
        }

        /// <summary>
        /// Reset the settings for the application before starting a test.
        /// Tests can override this method if they have have any settings that need to
        /// be set before the test's DoTest method gets called.
        /// </summary>
        protected void InitializeAutoQcSettings()
        {
            Settings.Default.Reset();
        }

        private static int GetWaitCycles(int millis = WAIT_TIME)
        {
            int waitCycles = millis / SLEEP_INTERVAL;

            if (System.Diagnostics.Debugger.IsAttached)
            {
                // When debugger is attached, some vendor readers are S-L-O-W!
                waitCycles *= 10;
            }

            // Wait a little longer for debug build. (This may also imply code coverage testing, slower yet)
            if (ExtensionTestContext.IsDebugMode)
            {
                waitCycles = waitCycles * 4;
            }

            return waitCycles;
        }

        protected static TDlg ShowDialog<TDlg>([InstantHandle] Action act, int millis = -1) where TDlg : Form
        {
            var existingDialog = FindOpenForm<TDlg>();
            if (existingDialog != null)
            {
                var alertDlg = existingDialog as AlertDlg;
                if (alertDlg == null)
                    AssertEx.IsNull(existingDialog, typeof(TDlg) + " is already open");
                else
                    Assert.Fail(typeof(TDlg) + " is already open with the message: " + alertDlg.Message);
            }

            AppBeginInvoke(act);
            TDlg dlg;
            if (millis == -1)
                dlg = WaitForOpenForm<TDlg>();
            else
                dlg = WaitForOpenForm<TDlg>(millis);
            Assert.IsNotNull(dlg);

            return dlg;
        }

        public static TDlg FindOpenForm<TDlg>() where TDlg : Form
        {
            foreach (var form in OpenForms)
            {
                var tForm = form as TDlg;
                if (tForm != null && tForm.Created)
                {
                    return tForm;
                }
            }
            return null;
        }

        public static TDlg WaitForOpenForm<TDlg>(int millis = WAIT_TIME) where TDlg : Form
        {
            var result = TryWaitForOpenForm<TDlg>(millis);
            if (result == null)
            {
                int waitCycles = GetWaitCycles(millis);
                Assert.Fail(@"Timeout {0} seconds exceeded in WaitForOpenForm({1}). Open forms: {2}", waitCycles * SLEEP_INTERVAL / 1000, typeof(TDlg).Name, GetOpenFormsString());
            }
            return result;
        }

        private static string GetOpenFormsString()
        {
            var result = string.Join(", ", OpenForms.Select(form => string.Format("{0} ({1})", form.GetType().Name, GetTextForForm(form))));
            // Without line numbers, this isn't terribly useful.  Disable for now.
            // result += GetAllThreadsStackTraces();
            return result;
        }

        private static string GetTextForForm(Control form)
        {
            var result = form.Text;
            var threadExceptionDialog = form as ThreadExceptionDialog;
            if (threadExceptionDialog != null)
            {
                // Locate the details text box, return the contents - much more informative than the dialog title
                result = threadExceptionDialog.Controls.Cast<Control>()
                    .Where(control => control is TextBox)
                    .Aggregate(result, (current, control) => current + ": " + GetExceptionText(control));
            }

            FormEx formEx = form as FormEx;
            if (formEx != null)
            {
                String detailedMessage = formEx.DetailedMessage;
                if (detailedMessage != null)
                {
                    result = detailedMessage;
                }
            }
            return result;
        }

        private static string GetExceptionText(Control control)
        {
            string text = control.Text;
            int assembliesIndex = text.IndexOf("************** Loaded Assemblies **************", StringComparison.Ordinal);
            if (assembliesIndex != -1)
                text = pwiz.Skyline.Util.Extensions.TextUtil.LineSeparate(text.Substring(0, assembliesIndex).Trim(), "------------- End ThreadExceptionDialog Stack -------------");
            return text;
        }

        public static TDlg TryWaitForOpenForm<TDlg>(int millis = WAIT_TIME, Func<bool> stopCondition = null) where TDlg : Form
        {
            int waitCycles = GetWaitCycles(millis);
            for (int i = 0; i < waitCycles; i++)
            {
                Assert.IsFalse(Program.TestExceptions.Any(), "Exception while running test");

                var tForm = FindOpenForm<TDlg>();
                if (tForm != null)
                {
                    return tForm;
                }

                if (stopCondition != null && stopCondition())
                    break;

                Thread.Sleep(SLEEP_INTERVAL);
            }
            return null;
        }


        public static void WaitForClosedForm(Form formClose)
        {
            int waitCycles = GetWaitCycles();
            for (int i = 0; i < waitCycles; i++)
            {
                Assert.IsFalse(Program.TestExceptions.Any(), "Exception while running test");

                bool isOpen = true;
                AppInvoke(() => isOpen = IsFormOpen(formClose));
                if (!isOpen)
                    return;
                Thread.Sleep(SLEEP_INTERVAL);
            }

            Assert.Fail(@"Timeout {0} seconds exceeded in WaitForClosedForm. Open forms: {1}", waitCycles * SLEEP_INTERVAL / 1000, GetOpenFormsString());
        }

        public static bool IsFormOpen(Form form)
        {
            foreach (var formOpen in OpenForms)
            {
                if (ReferenceEquals(form, formOpen))
                {
                    return true;
                }
            }
            return false;
        }

        protected static void RunDlg<TDlg>(Action show, [InstantHandle] Action<TDlg> act = null, bool pause = false, int millis = -1) where TDlg : Form
        {
            RunDlg(show, false, act, pause, millis);
        }

        protected static void RunDlg<TDlg>(Action show, bool waitForDocument, Action<TDlg> act = null, bool pause = false, int millis = -1) where TDlg : Form
        {
            TDlg dlg = ShowDialog<TDlg>(show, millis);
            // if (pause)
            //     PauseTest();
            RunUI(() =>
            {
                if (act != null)
                    act(dlg);
                else
                    dlg.CancelButton.PerformClick();
            });
            WaitForClosedForm(dlg);
        }

        private void WaitForMainWindow()
        {
            try
            {
                int waitCycles = GetWaitCycles();
                for (int i = 0; i < waitCycles; i++)
                {
                    if (Program.MainWindow != null && Program.MainWindow.IsHandleCreated)
                        break;

                    Thread.Sleep(SLEEP_INTERVAL);
                }
                
                Assert.IsTrue(Program.MainWindow != null && Program.MainWindow.IsHandleCreated,
                    @"Timeout {0} seconds exceeded in WaitForSkyline", waitCycles * SLEEP_INTERVAL / 1000);
                
                RunTest();
            }
            catch (Exception x)
            {
                // Save exception for reporting from main thread.
                Program.AddTestException(x);
            }

            EndTest();

            Settings.Default.Reset();
        }

        private void RunTest()
        {
            // Use internal clipboard for testing so that we don't collide with other processes
            // using the clipboard during a test run.
            ClipboardEx.UseInternalClipboard();
            ClipboardEx.Clear();

            var doClipboardCheck = TestContext.Properties.Contains(@"ClipboardCheck");
            string clipboardCheckText = doClipboardCheck ? (string)TestContext.Properties[@"ClipboardCheck"] : String.Empty;
            if (doClipboardCheck)
            {
                RunUI(() => Clipboard.SetText(clipboardCheckText));
            }

            DoTest();
            if (doClipboardCheck)
            {
                RunUI(() => Assert.AreEqual(clipboardCheckText, Clipboard.GetText()));
            }
        }

        private void EndTest()
        {
            var appWindow = Program.MainWindow;
            if (appWindow == null || appWindow.IsDisposed || !IsFormOpen(appWindow))
            {
                return;
            }

            try
            {
                // TODO: Release all resources 
                // WaitForCondition(1000, () => !FileStreamManager.Default.HasPooledStreams, string.Empty, false);
                // if (FileStreamManager.Default.HasPooledStreams)
                // {
                //     // Just write to console to provide more information. This should cause a failure
                //     // trying to remove the test directory, which will provide a path to the problem file
                //     Console.WriteLine(TextUtil.LineSeparate("Streams left open:", string.Empty,
                //         FileStreamManager.Default.ReportPooledStreams()));
                // }

                if (Program.TestExceptions.Count == 0)
                {
                    WaitForConditionUI(5000, () => OpenForms.Count() == 1);
                }
            }
            catch (Exception x)
            {
                // An exception occurred outside RunTest
                Program.AddTestException(x);
            }

            CloseOpenForms(typeof(MainForm));

            _testCompleted = true;

            try
            {
                // Clear the clipboard to avoid the appearance of a memory leak.
                ClipboardEx.Release();
                // Occasionally this causes an InvalidOperationException during stress testing.
                RunUI(MainWindow.Close);
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch (System.ComponentModel.InvalidAsynchronousStateException)
            {
                // This gets thrown a lot during nightly tests under Windows 10
            }
            catch (InvalidOperationException)
            // ReSharper restore EmptyGeneralCatchClause
            {
            }
        }

        public static bool WaitForConditionUI(int millis, Func<bool> func, Func<string> timeoutMessage = null, bool failOnTimeout = true, bool throwOnProgramException = true)
        {
            int waitCycles = GetWaitCycles(millis);
            for (int i = 0; i < waitCycles; i++)
            {
                if (throwOnProgramException)
                    Assert.IsFalse(Program.TestExceptions.Any(), "Exception while running test");

                bool isCondition = false;
                Program.MainWindow.Invoke(new Action(() => isCondition = func()));
                if (isCondition)
                    return true;
                Thread.Sleep(SLEEP_INTERVAL);

                // Assistance in chasing down intermittent timeout problems
                if (i == waitCycles - 1 && LaunchDebuggerOnWaitForConditionTimeout)
                {
                    System.Diagnostics.Debugger.Launch(); // Try again, under the debugger
                    System.Diagnostics.Debugger.Break();
                    i = 0; // For debugging ease - stay in loop
                }
            }
            if (failOnTimeout)
            {
                var msg = string.Empty;
                if (timeoutMessage != null)
                    RunUI(() => msg = " (" + timeoutMessage() + ")");

                AssertEx.Fail(@"Timeout {0} seconds exceeded in WaitForConditionUI{1}. Open forms: {2}", waitCycles * SLEEP_INTERVAL / 1000, msg, GetOpenFormsString());
            }
            return false;
        }

        private static IEnumerable<Form> OpenForms
        {
            get
            {
                return FormUtil.OpenForms;
            }
        }

        private void CloseOpenForms(Type exceptType)
        {
            // Actually throwing an exception can cause an infinite loop in MSTest
            var openForms = OpenForms.Where(form => form.GetType() != exceptType).ToList();
            Program.TestExceptions.AddRange(
                from form in openForms
                select new AssertFailedException(
                    String.Format(@"Form of type {0} left open at end of test", form.GetType())));
            while (openForms.Count > 0)
                CloseOpenForm(openForms.First(), openForms);
        }

        private void CloseOpenForm(Form formToClose, List<Form> openForms)
        {
            openForms.Remove(formToClose);
            // Close any owned forms, since they may be pushing message loops that would keep this form
            // from closing.
            foreach (var ownedForm in formToClose.OwnedForms)
            {
                CloseOpenForm(ownedForm, openForms);
            }

            var messageDlg = formToClose as AlertDlg;
            // ReSharper disable LocalizableElement
            if (messageDlg == null)
                Console.WriteLine("\n\nClosing open form of type {0}\n", formToClose.GetType()); // Not L10N
            else
                Console.WriteLine("\n\nClosing open MessageDlg: {0}\n", TextUtil.LineSeparate(messageDlg.Message, messageDlg.DetailMessage)); // Not L10N
            // ReSharper restore LocalizableElement

            RunUI(() =>
            {
                try
                {
                    formToClose.Close();
                }
                catch
                {
                    // Ignore exceptions
                }
            });
        }

        protected static void RunUI([InstantHandle] Action act)
        {
            AppInvoke(() =>
            {
                try
                {
                    act();
                }
                catch (Exception e)
                {
                    Assert.Fail(e.ToString());
                }
            });
        }

        private static void AppInvoke(Action act)
        {
            MainWindow?.Invoke(act);
        }

        private static void AppBeginInvoke(Action act)
        {
            MainWindow?.BeginInvoke(act);
        }


        // From AbstractUnitTest.cs
        /// <summary>
        /// Tracks which zip files were downloaded this run, and which might possibly be stale
        /// </summary>
        public Dictionary<string, bool> DictZipFileIsKnownCurrent { get; private set; }
        public string TestFilesZip
        {
            get
            {
                // ReSharper disable LocalizableElement
                Assert.AreEqual(1, _testFilesZips.Length, "Attempt to use TestFilesZip on test with multiple ZIP files.\nUse TestFilesZipPaths instead.");
                // ReSharper restore LocalizableElement
                return _testFilesZips[0];
            }
            set { TestFilesZipPaths = new[] { value }; }
        }

        private string[] _testFilesZips;
        public TestFilesDir[] TestFilesDirs { get; set; }
        public string TestDirectoryName { get; set; }

        /// <summary>
        /// Optional list of files to be retained from run to run. Useful for really
        /// large data files which are expensive to extract and keep as local copies.
        /// </summary>
        public string[] TestFilesPersistent { get; set; }

        // ReSharper disable UnusedAutoPropertyAccessor.Global
        // ReSharper disable MemberCanBeProtected.Global
        public TestContext TestContext { get; set; }
        // ReSharper restore MemberCanBeProtected.Global
        // ReSharper restore UnusedAutoPropertyAccessor.Global

        public bool IsExtractHere(int zipPathIndex)
        {
            return TestFilesZipExtractHere != null && TestFilesZipExtractHere[zipPathIndex];
        }

        /// <summary>
        /// One bool per TestFilesZipPaths indicating whether to unzip in the root directory (true) or a sub-directory (false or null)
        /// </summary>
        public bool[] TestFilesZipExtractHere { get; set; }

        public string[] TestFilesZipPaths
        {
            get { return _testFilesZips; }
            set
            {
                string[] zipPaths = value;
                _testFilesZips = new string[zipPaths.Length];
                DictZipFileIsKnownCurrent = new Dictionary<string, bool>();
                for (int i = 0; i < zipPaths.Length; i++)
                {
                    var zipPath = zipPaths[i];
                    // If the file is on the web, save it to the local disk in the developer's
                    // Downloads folder for future use
                    if (zipPath.Substring(0, 8).ToLower().Equals(@"https://") || zipPath.Substring(0, 7).ToLower().Equals(@"http://"))
                    {
                        var targetFolder = GetTargetZipFilePath(zipPath, out var zipFilePath);
                        if (!File.Exists(zipFilePath)) // If this is a perf test, skip download unless perf tests are enabled
                        {
                            zipPath = DownloadZipFile(targetFolder, zipPath, zipFilePath);
                            DictZipFileIsKnownCurrent.Add(zipPath, true);
                        }
                        else
                        {
                            DictZipFileIsKnownCurrent.Add(zipPath, false); // May wish to retry test with a fresh download if it fails
                        }
                        zipPath = zipFilePath;
                    }
                    _testFilesZips[i] = zipPath;
                }
            }
        }

        private static string GetTargetZipFilePath(string zipPath, out string zipFilePath)
        {
            var downloadsFolder = PathEx.GetDownloadsPath();
            var urlFolder = zipPath.Split('/')[zipPath.Split('/').Length - 2]; // usually "tutorial" or "PerfTest"
            var targetFolder =
                Path.Combine(downloadsFolder, char.ToUpper(urlFolder[0]) + urlFolder.Substring(1)); // "tutorial"->"Tutorial"
            var fileName = zipPath.Substring(zipPath.LastIndexOf('/') + 1);
            zipFilePath = Path.Combine(targetFolder, fileName);
            return targetFolder;
        }

        private static string DownloadZipFile(string targetFolder, string zipPath, string zipFilePath)
        {
            if (!Directory.Exists(targetFolder))
                Directory.CreateDirectory(targetFolder);

            bool downloadFromS3 = Environment.GetEnvironmentVariable("SKYLINE_DOWNLOAD_FROM_S3") == "1";
            string s3hostname = @"skyline-perftest.s3-us-west-2.amazonaws.com";
            if (downloadFromS3)
                zipPath = zipPath.Replace(@"skyline.gs.washington.edu", s3hostname).Replace(@"skyline.ms", s3hostname);

            WebClient webClient = new WebClient();
            using (var fs = new FileSaver(zipFilePath))
            {
                try
                {
                    webClient.DownloadFile(zipPath.Split('\\')[0],
                        fs.SafeName); // We encode a Chorus anonymous download string as two parts: url\localName
                }
                catch (Exception x)
                {
                    Assert.Fail("Could not download {0}: {1}", zipPath, x.Message);
                }

                fs.Commit();
            }

            return zipPath;
        }
    }
}