﻿/*
 * Original author: Don Marsh <donmarsh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2014 University of Washington - Seattle, WA
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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using Ionic.Zip;
using SkylineNightly.Properties;

namespace SkylineNightly
{
    // ReSharper disable NonLocalizedString
    public class Nightly
    {
        private const string NIGHTLY_TASK_NAME = "Skyline nightly build";

        private const string TEAM_CITY_BUILD_URL = "https://teamcity.labkey.org/viewType.html?buildTypeId={0}";
        private const string TEAM_CITY_ZIP_URL = "https://teamcity.labkey.org/repository/download/{0}/{1}:id/SkylineTester.zip";
        private const string TEAM_CITY_BUILD_TYPE_64 = "bt209";
        private const string TEAM_CITY_BUILD_TYPE_32 = "bt19";
        private const string TEAM_CITY_BUILD_TYPE = TEAM_CITY_BUILD_TYPE_64;
        private const string TEAM_CITY_USER_NAME = "guest";
        private const string TEAM_CITY_USER_PASSWORD = "guest";
        private const string LABKEY_URL = "https://skyline.ms/labkey/testresults/home/development/Nightly%20x64/post.view?";
        private const string LABKEY_PERF_URL = "https://skyline.ms/labkey/testresults/home/development/Performance%20Tests/post.view?";
        private const string LABKEY_STRESS_URL = "https://skyline.ms/labkey/testresults/home/development/NightlyStress/post.view?";
        private const string LABKEY_RELEASE_URL = "https://skyline.ms/labkey/testresults/home/development/Release%20Branch/post.view?";
        private const string LABKEY_RELEASE_PERF_URL = "https://skyline.ms/labkey/testresults/home/development/Release%20Branch%20Performance%20Tests/post.view?";
        private const string LABKEY_INTEGRATION_URL = "https://skyline.ms/labkey/testresults/home/development/Integration/post.view";
        
        private const string GIT_MASTER_URL = "https://github.com/ProteoWizard/pwiz";
        private const string GIT_BRANCHES_URL = GIT_MASTER_URL + "/tree/";

        // Current integration branch - this gets appended to GIT_BRANCHES_URL for integration build
        private const string INTEGRATION_BRANCH = "Skyline/work/20180125_FiguresOfMerit";

        private DateTime _startTime;
        public string LogFileName { get; private set; }
        private readonly Xml _nightly;
        private readonly Xml _failures;
        private readonly Xml _leaks;
        private Xml _pass;
        private readonly string _logDir;
        private readonly RunMode _runMode;
        private string PwizDir
        {
            get
            {
                // Place source code in SkylineTester instead of next to it, so we can 
                // still proceed if the source tree is locked against delete for any reason
                return Path.Combine(_skylineTesterDir, "pwiz");
            }
        }
        private TimeSpan _duration;
        private string _skylineTesterDir;

        public const int DEFAULT_DURATION_HOURS = 9;
        public const int PERF_DURATION_HOURS = 12;

        public Nightly(RunMode runMode, string decorateSrcDirName = null)
        {
            _runMode = runMode;
            _nightly = new Xml("nightly");
            _failures = _nightly.Append("failures");
            _leaks = _nightly.Append("leaks");
            
            // Locate relevant directories.
            var nightlyDir = GetNightlyDir();
            _logDir = Path.Combine(nightlyDir, "Logs");
            // Clean up after any old screengrab directories
            var logDirScreengrabs = Path.Combine(_logDir, "NightlyScreengrabs");
            if (Directory.Exists(logDirScreengrabs))
                Directory.Delete(logDirScreengrabs, true);
            // First guess at working directory - distinguish between run types for machines that do double duty
            _skylineTesterDir = Path.Combine(nightlyDir, "SkylineTesterForNightly_"+runMode + (decorateSrcDirName ?? string.Empty));

            // Default duration.
            _duration = TimeSpan.FromHours(DEFAULT_DURATION_HOURS);
        }

        public static string NightlyTaskName { get { return NIGHTLY_TASK_NAME; } }
        public static string NightlyTaskNameWithUser { get { return string.Format("{0} ({1})", NIGHTLY_TASK_NAME, Environment.UserName);} }

        public void Finish(string message, string errMessage)
        {
            // Leave a note for the user, in a way that won't interfere with our next run
            Log("Done.  Exit message:"); // Not L10N
            Log(message);
            if (!string.IsNullOrEmpty(errMessage))
                Log(errMessage);
            if (string.IsNullOrEmpty(LogFileName))
            {
                MessageBox.Show(message, @"SkylineNightly Help");    // Not L10N
            }
            else
            {
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = "notepad.exe", // Not L10N
                        Arguments = LogFileName
                    }
                };
                process.Start();
            }
        }

        public enum RunMode { parse, post, trunk, perf, release, stress, integration, release_perf }

        public string RunAndPost()
        {
            var runResult = Run() ?? string.Empty;
            Parse();
            var postResult = Post(_runMode);
            if (!string.IsNullOrEmpty(postResult))
            {
                if (!string.IsNullOrEmpty(runResult))
                    runResult += "\n"; // Not L10N
                runResult += postResult;
            }
            return runResult;
        }

        /// <summary>
        /// Run nightly build/test and report results to server.
        /// </summary>
        public string Run()
        {
            string result = string.Empty;
            // Locate relevant directories.
            var nightlyDir = GetNightlyDir();
            var skylineNightlySkytr = Path.Combine(nightlyDir, "SkylineNightly.skytr");

            bool withPerfTests = _runMode != RunMode.trunk && _runMode != RunMode.integration && _runMode != RunMode.release;

            if (_runMode == RunMode.stress)
            {
                _duration = TimeSpan.FromHours(168);  // Let it go as long as a week
            }
            else if (withPerfTests)
            {
                _duration = TimeSpan.FromHours(PERF_DURATION_HOURS); // Let it go a bit longer than standard 9 hours
            }

            // Kill any other instance of SkylineNightly, unless this is
            // the StressTest mode, in which case assume that a previous invocation
            // is still running and just exit to stay out of its way.
            foreach (var process in Process.GetProcessesByName("skylinenightly"))
            {
                if (process.Id != Process.GetCurrentProcess().Id)
                {
                    if (_runMode == RunMode.stress)
                    {
                        Application.Exit();  // Just let the already (long!) running process do its thing
                    }
                    else
                    {
                        process.Kill();
                    }
                }
            }

            // Kill processes started within the proposed working directory - most likely SkylineTester and/or TestRunner.
            // This keeps stuck tests around for 24 hours, which should be sufficient, but allows us to replace directory
            // on a daily basis - otherwise we could fill the hard drive on smaller machines
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    if (process.Modules[0].FileName.StartsWith(_skylineTesterDir) &&
                        process.Id != Process.GetCurrentProcess().Id)
                    {
                        process.Kill();
                    }
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch (Exception)
                {
                }
            }

            // Create place to put run logs
            if (!Directory.Exists(_logDir))
                Directory.CreateDirectory(_logDir);
            // Start the nightly log file
            StartLog(_runMode);

            // Delete source tree and old SkylineTester.
            Delete(skylineNightlySkytr);
            Log("Delete SkylineTester");
            var skylineTesterDirBasis = _skylineTesterDir; // Default name
            const int maxRetry = 1000;  // Something would have to be very wrong to get here, but better not to risk a hang
            string nextDir = _skylineTesterDir;
            for (var retry = 1; retry < maxRetry; retry++)
            {
                try
                {
                    if (!Directory.Exists(nextDir))
                        break;

                    string deleteDir = nextDir;
                    // Keep going until a directory is found that does not exist
                    nextDir = skylineTesterDirBasis + "_" + retry;

                    Delete(deleteDir);
                }
                catch (Exception e)
                {
                    if (Directory.Exists(_skylineTesterDir))
                    {
                        // Work around undeletable file that sometimes appears under Windows 10
                        Log("Unable to delete " + _skylineTesterDir + "(" + e + "),  using " + nextDir + " instead.");
                        _skylineTesterDir = nextDir;
                    }
                }
            }
            Log("buildRoot is " + PwizDir);

            // We used to put source tree alongside SkylineTesterDir instead of under it, delete that too
            try
            {
                Delete(Path.Combine(nightlyDir, "pwiz"));
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }

            Directory.CreateDirectory(_skylineTesterDir);

            // Download most recent build of SkylineTester.
            var skylineTesterZip = Path.Combine(_skylineTesterDir, skylineTesterDirBasis + ".zip");
            const int attempts = 30;
            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    DownloadSkylineTester(skylineTesterZip, _runMode);
                    break;
                }
                catch (Exception ex)
                {
                    Log("Exception while downloading SkylineTester: " + ex.Message + " (Probably still being built, will retry every 60 seconds for 30 minutes.)");
                    if (i == attempts-1)
                    {
                        LogAndThrow("Unable to download SkylineTester");
                    }
                    Thread.Sleep(60*1000);  // one minute
                }
            }

            // Install SkylineTester.
            if (!InstallSkylineTester(skylineTesterZip, _skylineTesterDir))
                LogAndThrow("SkylineTester installation failed.");

            // Delete zip file.
            Log("Delete zip file " + skylineTesterZip);
            File.Delete(skylineTesterZip);

            // Now figure out which branch we're working in - there's a file in the downloaded SkylineTester zip that tells us.
            string branchUrl = null;
            var branchLine = File.ReadAllLines(Path.Combine(_skylineTesterDir, "SkylineTester Files", "Version.cpp")).FirstOrDefault(l => l.Contains("Version::Branch"));
            if (!string.IsNullOrEmpty(branchLine))
            {
                // Looks like std::string Version::Branch()   {return "skyline_9_7";}
                var branch = branchLine.Split(new[] { "\"" }, StringSplitOptions.None)[1];
                if (branch.Equals("master"))
                {
                    branchUrl = GIT_MASTER_URL;
                }
                else
                {
                    branchUrl = GIT_BRANCHES_URL + branch;
                }
            }
            // Or if we are running the integration build, then we use the trunk SkylineTester, but have it build the branch TestRunner.exe
            else if (_runMode == RunMode.integration)
            {
                branchUrl = GIT_BRANCHES_URL + INTEGRATION_BRANCH;
            }

            // Create ".skytr" file to execute nightly build in SkylineTester.
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "SkylineNightly.SkylineNightly.skytr";
            int durationSeconds;
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    LogAndThrow(result = "Embedded resource is broken");
                    return result; 
                }
                using (var reader = new StreamReader(stream))
                {
                    var skylineTester = Xml.FromString(reader.ReadToEnd());
                    skylineTester.GetChild("nightlyStartTime").Set(DateTime.Now.ToShortTimeString());
                    skylineTester.GetChild("nightlyRoot").Set(nightlyDir);
                    skylineTester.GetChild("buildRoot").Set(_skylineTesterDir);
                    skylineTester.GetChild("nightlyRunPerfTests").Set(withPerfTests?"true":"false");
                    skylineTester.GetChild("nightlyDuration").Set(((int)_duration.TotalHours).ToString());
                    skylineTester.GetChild("nightlyRepeat").Set(_runMode == RunMode.stress ? "100" : "1");
                    skylineTester.GetChild("nightlyRandomize").Set(_runMode == RunMode.stress ? "true" : "false");
                    if (!string.IsNullOrEmpty(branchUrl) && branchUrl.Contains("tree"))
                    {
                        skylineTester.GetChild("nightlyBuildTrunk").Set("false");
                        skylineTester.GetChild("nightlyBranch").Set("true");
                        skylineTester.GetChild("nightlyBranchUrl").Set(branchUrl);
                        Log("Testing branch at " + branchUrl);
                    }
                    skylineTester.Save(skylineNightlySkytr);
                    var durationHours = double.Parse(skylineTester.GetChild("nightlyDuration").Value);
                    durationSeconds = (int) (durationHours*60*60) + 30*60;  // 30 minutes grace before we kill SkylineTester
                }
            }


            // Start SkylineTester to do the build.
            var skylineTesterExe = Path.Combine(_skylineTesterDir, "SkylineTester Files", "SkylineTester.exe");
            Log(string.Format("Starting {0} with config file {1}, which contains:", skylineTesterExe, skylineNightlySkytr));
            foreach (var line in File.ReadAllLines(skylineNightlySkytr))
            {
                Log(line);
            }

            var processInfo = new ProcessStartInfo(skylineTesterExe, skylineNightlySkytr)
            {
                WorkingDirectory = Path.GetDirectoryName(skylineTesterExe) ?? ""
            };

            var startTime = DateTime.Now;

            bool retryTester;
            const int maxRetryMinutes = 60;
            do
            {
                var skylineTesterProcess = Process.Start(processInfo);
                if (skylineTesterProcess == null)
                {
                    LogAndThrow(result = "SkylineTester did not start");
                    return result;
                }
                Log("SkylineTester started");
                if (!skylineTesterProcess.WaitForExit(durationSeconds * 1000))
                {
                    SaveErrorScreenshot();
                    Log(result = "SkylineTester has exceeded its " + durationSeconds +
                                 " second WaitForExit timeout.  You should investigate.");
                }
                else
                {
                    Log("SkylineTester finished");
                }

                _duration = DateTime.Now - startTime;
                retryTester = _duration.TotalMinutes < maxRetryMinutes;
                if (retryTester)
                {
                    // Retry a very short test run if there is no log file or the log file does not contain any tests
                    string logFile = GetLatestLog();
                    if (logFile != null && File.Exists(logFile))
                        retryTester = ParseTests(File.ReadAllText(logFile), false) == 0;
                    if (retryTester)
                    {
                        Log("No tests run in " + Math.Round(_duration.TotalMinutes) + " minutes retrying.");
                    }
                }
            }
            while (retryTester);

            return result;
        }

        public void StartLog(RunMode runMode)
        {
            _startTime = DateTime.Now;

            // Create log file.
            LogFileName = Path.Combine(_logDir, string.Format("SkylineNightly-{0}-{1}.log", runMode, _startTime.ToString("yyyy-MM-dd-HH-mm", CultureInfo.InvariantCulture)));
            Log(_startTime.ToShortDateString());
        }

        private void DownloadSkylineTester(string skylineTesterZip, RunMode mode)
        {
            using (var client = new WebClient())
            {
                client.Credentials = new NetworkCredential(TEAM_CITY_USER_NAME, TEAM_CITY_USER_PASSWORD);
                var branchType = ((mode == RunMode.release) || (mode == RunMode.release_perf))
                    ? "Skyline/skyline"
                    : "master";
                var buildPageUrl = string.Format(TEAM_CITY_BUILD_URL, TEAM_CITY_BUILD_TYPE);
                Log("Download Team City build page");
                var buildStatusPage = client.DownloadString(buildPageUrl);
                // Multiple branches may be built under the same config, locate the one we want
                var builds = buildStatusPage.Split(new []{@"class=""branchName"">"}, StringSplitOptions.None);
                var buildStatus = builds.Where(b => b.StartsWith(branchType)).ToArray();
                switch (buildStatus.Length)
                {
                    case 1:
                        var match = Regex.Match(buildStatus[0], @"<span\sid=""build:([^""]*):text"">Tests\spassed:");
                        if (match.Success)
                        {
                            string id = match.Groups[1].Value;
                            string zipFileLink = string.Format(TEAM_CITY_ZIP_URL, TEAM_CITY_BUILD_TYPE, id);
                            Log("Download SkylineTester zip file");
                            client.DownloadFile(zipFileLink, skylineTesterZip);
                            return;
                        }
                        Log(string.Format(@"Found a TeamCity build starting with ""{0}"" but it is not a successful build", branchType));
                        break;
                    case 0:
                        Log(string.Format(@"Did not find a TeamCity build whose name starts with ""{0}""", branchType));
                        break;
                    default:
                        Log(string.Format(@"Found more than one TeamCity build whose name starts with ""{0}""", branchType));
                        break;
                }
            }
        }

        private bool InstallSkylineTester(string skylineTesterZip, string skylineTesterDir)
        {
            using (var zipFile = new ZipFile(skylineTesterZip))
            {
                try
                {
                    Log("Unzip SkylineTester");
                    zipFile.ExtractAll(skylineTesterDir, ExtractExistingFileAction.OverwriteSilently);
                }
                catch (Exception)
                {
                    Log("Error attempting to unzip SkylineTester");
                    return false;
                }
            }
            return true;
        }

        public RunMode Parse(string logFile = null, bool parseOnlyNoXmlOut = false)
        {
            if (logFile == null)
                logFile = GetLatestLog();
            if (logFile == null || !File.Exists(logFile))
                throw new Exception(string.Format("cannot locate {0}", logFile ?? "current log"));
            var log = File.ReadAllText(logFile);

            // Extract log start time from log contents
            var reStartTime = new Regex(@"\n\# Nightly started (.*)\r\n", RegexOptions.Compiled); // As in "# Nightly started Thursday, May 12, 2016 8:00 PM"
            var stMatch = reStartTime.Match(log);
            if (stMatch.Success)
            {
                var dateTimeStr = stMatch.Groups[1].Value;
                DateTime.TryParse(dateTimeStr, out _startTime);
            }

            // Extract all test lines.
            var testCount = ParseTests(log);

            // Extract failures.
            ParseFailures(log);

            // Extract leaks.
            ParseLeaks(log);

            var hasPerftests = log.Contains("# Perf tests");
            var isIntegration = log.Contains(GIT_BRANCHES_URL);
            var isTrunk = !isIntegration && !log.Contains("Testing branch at");

            var machineName = Environment.MachineName;
            // Get machine name from logfile name, in case it's not from this machine
            var reMachineName = new Regex(@"(.*)_\d+\-\d+\-\d+_\d+\-\d+\-\d+\.\w+", RegexOptions.Compiled); // As in "NATBR-LAB-PC_2016-05-12_20-00-19.log"
            var mnMatch = reMachineName.Match(Path.GetFileName(logFile));
            if (mnMatch.Success)
            {
                machineName = mnMatch.Groups[1].Value.ToUpperInvariant();
            }

            // See if we can parse revision info from the log
            string revisionInfo = null;
            string gitHash = null;
            // Checked out revision 9708.
            var reRevision = new Regex(@"\nChecked out revision (.*)\.\r\n", RegexOptions.Compiled); // As in "Checked out revision 9708."
            var revMatch = reRevision.Match(log);
            if (revMatch.Success)
            {
                revisionInfo = revMatch.Groups[1].Value;
                gitHash = "(svn)";
            }
            else // Look for log message where we emit our build ID
            {
                // look for build message like "ProteoWizard 3.0.18099.a0147f2 x64 AMD64"
                reRevision = new Regex(@"\nProteoWizard \d+\.\d+\.([^ ]*)\.([^ ]*).*\r\n", RegexOptions.Compiled); 
                revMatch = reRevision.Match(log);
                if (revMatch.Success)
                {
                    revisionInfo = revMatch.Groups[1].Value;
                    gitHash = revMatch.Groups[2].Value;
                }
            }

            _nightly["id"] = machineName;
            _nightly["os"] = Environment.OSVersion;
            var buildroot = ParseBuildRoot(log);
            _nightly["revision"] = revisionInfo ?? GetRevision(buildroot);
            _nightly["git_hash"] = gitHash ?? string.Empty;
            _nightly["start"] = _startTime;
            _nightly["duration"] = (int)_duration.TotalMinutes;
            _nightly["testsrun"] = testCount;
            _nightly["failures"] = _failures.Count;
            _nightly["leaks"] = _leaks.Count;

            // Save XML file.
            if (!parseOnlyNoXmlOut)
            {
                var xmlFile = Path.ChangeExtension(logFile, ".xml");
                File.WriteAllText(xmlFile, _nightly.ToString());
            }
            return isTrunk
                ? (hasPerftests ? RunMode.perf : RunMode.trunk)
                : (isIntegration ? RunMode.integration :  (hasPerftests ? RunMode.release_perf : RunMode.release));
        }

        private int ParseTests(string log, bool storeXml = true)
        {
            var startTest = new Regex(@"\r\n\[(\d\d:\d\d)\] +(\d+).(\d+) +(\S+) +\((\w\w)\) ", RegexOptions.Compiled);
            var endTest = new Regex(@" \d+ failures, ([\.\d]+)/([\.\d]+) MB, (\d+) sec\.\r\n", RegexOptions.Compiled);

            string lastPass = null;
            int testCount = 0;
            for (var startMatch = startTest.Match(log); startMatch.Success; startMatch = startMatch.NextMatch())
            {
                var timestamp = startMatch.Groups[1].Value;
                var passId = startMatch.Groups[2].Value;
                var testId = startMatch.Groups[3].Value;
                var name = startMatch.Groups[4].Value;
                var language = startMatch.Groups[5].Value;

                var endMatch = endTest.Match(log, startMatch.Index);
                var managed = endMatch.Groups[1].Value;
                var total = endMatch.Groups[2].Value;
                var duration = endMatch.Groups[3].Value;

                if (string.IsNullOrEmpty(managed) || string.IsNullOrEmpty(total) || string.IsNullOrEmpty(duration))
                    continue;

                if (lastPass != passId)
                {
                    lastPass = passId;
                    if (storeXml)
                    {
                        _pass = _nightly.Append("pass");
                        _pass["id"] = passId;
                    }
                }

                if (storeXml)
                {
                    var test = _pass.Append("test");
                    test["id"] = testId;
                    test["name"] = name;
                    test["language"] = language;
                    test["timestamp"] = timestamp;
                    test["duration"] = duration;
                    test["managed"] = managed;
                    test["total"] = total;
                }

                testCount++;
            }
            return testCount;
        }

        private void ParseFailures(string log)
        {
            var startFailure = new Regex(@"\r\n!!! (\S+) FAILED\r\n", RegexOptions.Compiled);
            var endFailure = new Regex(@"\r\n!!!\r\n", RegexOptions.Compiled);
            var failureTest = new Regex(@"\r\n\[(\d\d:\d\d)\] +(\d+).(\d+) +(\S+)\s+\(+(\S+)\)",
                RegexOptions.Compiled | RegexOptions.RightToLeft);

            for (var startMatch = startFailure.Match(log); startMatch.Success; startMatch = startMatch.NextMatch())
            {
                var name = startMatch.Groups[1].Value;
                var endMatch = endFailure.Match(log, startMatch.Index);
                var failureTestMatch = failureTest.Match(log, startMatch.Index);
                var timestamp = failureTestMatch.Groups[1].Value;
                var passId = failureTestMatch.Groups[2].Value;
                var testId = failureTestMatch.Groups[3].Value;
                var language = failureTestMatch.Groups[5].Value;
                if (string.IsNullOrEmpty(passId) || string.IsNullOrEmpty(testId))
                    continue;
                var failureDescription = log.Substring(startMatch.Index + startMatch.Length,
                    endMatch.Index - startMatch.Index - startMatch.Length);
                var failure = _failures.Append("failure");
                failure["name"] = name;
                failure["timestamp"] = timestamp;
                failure["pass"] = passId;
                failure["test"] = testId;
                failure["language"] = language;
                failure.Set(Environment.NewLine + failureDescription + Environment.NewLine);
            }
        }

        private void ParseLeaks(string log)
        {
            var leakPattern = new Regex(@"!!! (\S+) LEAKED (\d+) bytes", RegexOptions.Compiled);
            for (var match = leakPattern.Match(log); match.Success; match = match.NextMatch())
            {
                var leak = _leaks.Append("leak");
                leak["name"] = match.Groups[1].Value;
                leak["bytes"] = match.Groups[2].Value;
            }
        }

        private string ParseBuildRoot(string log)
        {
            // Look for:  > "C:\Program Files\Git\cmd\git.exe" clone "https://github.com/ProteoWizard/pwiz" "C:\Nightly\SkylineTesterForNightly_trunk\pwiz"
            var brPattern = new Regex(@".*"".*git\.exe"" clone "".*"" ""(.*)""", RegexOptions.Compiled);
            var match = brPattern.Match(log);
            if (!match.Success)
            {
                brPattern = new Regex(@"Deleting Build directory\.\.\.\r\n\> rmdir /s ""(\S+)""", RegexOptions.Compiled);
                match = brPattern.Match(log);
            }
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return PwizDir;
        }

        /// <summary>
        /// Post the latest results to the server.
        /// </summary>
        public string Post(RunMode mode, string xmlFile = null)
        {
            if (xmlFile == null)
            {
                xmlFile = GetLatestLog();    // Change extension to .xml below
                if (xmlFile == null)
                    return string.Empty;
                xmlFile = Path.ChangeExtension(xmlFile, ".xml"); // In case it's actually the log file name
            }
            
            var xml = File.ReadAllText(xmlFile);
            var logFile = GetLatestLog();
            if (logFile != null)
            {
                var log = File.ReadAllText(logFile);
                XDocument doc = XDocument.Parse(xml);
                if (doc.Root != null)
                {
                    doc.Root.Add(new XElement("Log", log));
                    xml = doc.ToString();
                }
            }
            string url;
            // Post to server.
            if (mode == RunMode.integration)
                url = LABKEY_INTEGRATION_URL;
            else if (mode == RunMode.release_perf)
                url = LABKEY_RELEASE_PERF_URL;
            else if (mode == RunMode.release)
                url = LABKEY_RELEASE_URL;
            else if (mode == RunMode.perf)
                url = LABKEY_PERF_URL;
            else if (mode == RunMode.stress)
                url = LABKEY_STRESS_URL;
            else
                url = LABKEY_URL;
            var result = PostToLink(url, xml);
            return result;
        }

        public string GetLatestLog()
        {
            var directory = new DirectoryInfo(_logDir);
            var logFile = directory.GetFiles()
                .Where(f => f.Name.StartsWith(Environment.MachineName) && f.Name.EndsWith(".log"))
                .OrderByDescending(f => f.LastWriteTime)
                .First();
            return logFile == null ? null : logFile.FullName;
        }

        /// <summary>
        /// Post data to the given link URL.
        /// </summary>
        private string PostToLink(string link, string postData)
        {
            var errmessage = string.Empty;
            Log("Posting results to " + link); // Not L10N
            for (var retry = 5; retry > 0; retry--)
            {
                string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x"); // Not L10N
                byte[] boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n"); // Not L10N

                var wr = (HttpWebRequest) WebRequest.Create(link);
                wr.ProtocolVersion = HttpVersion.Version10;
                wr.ContentType = "multipart/form-data; boundary=" + boundary; // Not L10N
                wr.Method = "POST"; // Not L10N
                wr.KeepAlive = true;
                wr.Credentials = CredentialCache.DefaultCredentials;
                
                var rs = wr.GetRequestStream();

                rs.Write(boundarybytes, 0, boundarybytes.Length);
                const string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n"; // Not L10N
                string header = string.Format(headerTemplate, "xml_file", "xml_file", "text/xml");
                byte[] headerbytes = Encoding.UTF8.GetBytes(header);
                rs.Write(headerbytes, 0, headerbytes.Length);
                var bytes = Encoding.UTF8.GetBytes(postData);
                rs.Write(bytes, 0, bytes.Length);

                byte[] trailer = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n"); // Not L10N
                rs.Write(trailer, 0, trailer.Length);
                rs.Close();

                WebResponse wresp = null;
                try
                {
                    wresp = wr.GetResponse();
                    var stream2 = wresp.GetResponseStream();
                    if (stream2 != null)
                    {
                        var reader2 = new StreamReader(stream2);
                        var result = reader2.ReadToEnd();
                        return result;
                    }
                }
                catch (Exception e)
                {
                    Log(errmessage = e.ToString());
                    if (wresp != null)
                    {
                        wresp.Close();
                    }
                }
                if (retry > 1)
                {
                    Thread.Sleep(30000);
                    Log("Retrying post"); // Not L10N
                    errmessage = String.Empty;
                }
            }
            Log(errmessage = "Failed to post results: " + errmessage); // Not L10N
            return errmessage;
        }

        private static string GetNightlyDir()
        {
            var nightlyDir = Settings.Default.NightlyFolder;
            return Path.IsPathRooted(nightlyDir)
                ? nightlyDir
                // Kept for backward compatibility, but we don't allow this anymore, because the Documents
                // folder is a terrible place to be running these high-use, nightly tests from.
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), nightlyDir);
        }

        private static string GitCommand(string workingdir, string cmd)
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var exe = Path.Combine(programFiles, @"Git\cmd\git.exe");
            Process git = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    FileName = exe,
                    WorkingDirectory = workingdir,
                    Arguments = cmd,
                    CreateNoWindow = true
                }
            };
            git.Start();
            var gitOutput = git.StandardOutput.ReadToEnd();
            git.WaitForExit();
            return gitOutput.Trim();
        }

        private string GetRevision(string pwizDir)
        {

            var revisionHash = GitCommand(pwizDir, @"rev-parse --short HEAD");
            var revision = "unknownDate." + revisionHash;
            return revision;
        }

        /// <summary>
        /// Delete a file or directory, with quite a lot of retry on the expectation that 
        /// it's probably the TortoiseGit windows explorer icon plugin getting in your way
        /// </summary>
        private void Delete(string fileOrDir)
        {
            for (var i = 5; i >0; i--)
            {
                try
                {
                    DeleteRecursive(fileOrDir);
                    break;
                }
                catch (Exception ex)
                {
                    if (i == 1)
                        throw;
                    Log("Retrying failed delete of " + fileOrDir + ": " + ex.Message);
                    var random = new Random();
                    Thread.Sleep(1000 + random.Next(0, 5000)); // A little stutter-step to avoid unlucky sync with TortoiseGit icon update
                }
            }
        }

        private void DeleteRecursive(string fileOrDir)
        {
            if (File.Exists(fileOrDir))
            {
                File.SetAttributes(fileOrDir, FileAttributes.Normal);   // Protect against failing on read-only files
                File.Delete(fileOrDir);
            }
            else if (Directory.Exists(fileOrDir))
            {
                foreach (var entry in Directory.EnumerateFileSystemEntries(fileOrDir))
                {
                    DeleteRecursive(entry);
                }
                Directory.Delete(fileOrDir, true);
            }
        }

        internal string Log(string message)
        {
            var time = DateTime.Now;
            var timestampedMessage = string.Format(
                "[{0}:{1}:{2}] {3}",
                time.Hour.ToString("D2"),
                time.Minute.ToString("D2"),
                time.Second.ToString("D2"),
                message);
            if (LogFileName != null)
                File.AppendAllText(LogFileName, timestampedMessage
                              + Environment.NewLine);
            return timestampedMessage;
        }

        private void LogAndThrow(string message)
        {
            var timestampedMessage = Log(message);
            SaveErrorScreenshot();
            throw new Exception(timestampedMessage);
        }

        private void SaveErrorScreenshot()
        {
            // Capture the screen in hopes of finding exception dialogs etc
            // From http://stackoverflow.com/questions/362986/capture-the-screen-into-a-bitmap

            try
            {
                foreach (var screen in Screen.AllScreens) // Handle multi-monitor
                {
                    // Create a new bitmap.
                    using (var bmpScreenshot = new Bitmap(screen.Bounds.Width, screen.Bounds.Height, PixelFormat.Format32bppArgb))
                    {
                        // Create a graphics object from the bitmap.
                        using (var gfxScreenshot = Graphics.FromImage(bmpScreenshot))
                        {
                            // Take the screenshot from the upper left corner to the right bottom corner.
                            gfxScreenshot.CopyFromScreen(screen.Bounds.X, screen.Bounds.Y,
                                0, 0, screen.Bounds.Size, CopyPixelOperation.SourceCopy);

                            // Save the screenshot
                            const string basename = "SkylineNightly_error_screenshot";
                            const string ext = ".png";
                            var fileScreenshot = Path.Combine(GetNightlyDir(), basename + ext);
                            for (var retry = 0; File.Exists(fileScreenshot); retry++)
                                fileScreenshot = Path.Combine(GetNightlyDir(), basename + "_" + retry + ext);
                            bmpScreenshot.Save(fileScreenshot, ImageFormat.Png);
                            Log("Diagnostic screenshot saved to \"" + fileScreenshot + "\"");
                        }
                    }
                }
            }
            catch (Exception x)
            {
                Log("Could not create diagnostic screenshot: got exception \"" + x.Message + "\"");
            }
        }
    }

    // ReSharper restore NonLocalizedString
}
