﻿/*
 * SonarQube Scanner for MSBuild
 * Copyright (C) 2016-2018 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarScanner.MSBuild.Common;
using TestUtilities;

namespace SonarScanner.MSBuild.Tasks.UnitTests
{
    [TestClass]
    public class TaskUtilitiesTests
    {
        public TestContext TestContext { get; set; }

        #region Tests

        [TestMethod]
        [TestCategory("Task")] // Regression test for bug http://jira.codehaus.org/browse/SONARMSBRU-11
        public void TaskUtils_LoadConfig_RetryIfConfigLocked_ValueReturned()
        {
            // Arrange
            var testFolder = TestUtils.CreateTestSpecificFolder(TestContext);
            var configFile = CreateAnalysisConfig(testFolder);

            var logger = new TestLogger();

            AnalysisConfig result = null;
            PerformOpOnLockedFile(configFile, () => result = TaskUtilities.TryGetConfig(testFolder, logger), shouldTimeoutReadingConfig: false);

            Assert.IsNotNull(result, "Expecting the config to have been loaded");

            AssertRetryAttempted(logger);
            logger.AssertWarningsLogged(0);
            logger.AssertErrorsLogged(0);
        }

        [TestMethod]
        [TestCategory("Task")] // Regression test for bug http://jira.codehaus.org/browse/SONARMSBRU-11
        public void TaskUtils_LoadConfig_TimeoutIfConfigLocked_NullReturned()
        {
            // Arrange
            // We'll lock the file and sleep for long enough for the task to timeout
            var testFolder = TestUtils.CreateTestSpecificFolder(TestContext);
            var configFile = CreateAnalysisConfig(testFolder);

            var logger = new TestLogger();

            AnalysisConfig result = null;

            PerformOpOnLockedFile(configFile, () => result = TaskUtilities.TryGetConfig(testFolder, logger), shouldTimeoutReadingConfig: true);

            Assert.IsNull(result, "Not expecting the config to be retrieved");

            AssertRetryAttempted(logger);
            logger.AssertWarningsLogged(0);
            logger.AssertErrorsLogged(1);
        }

        [TestMethod]
        public void TaskUtils_TryGetMissingConfig_NoError()
        {
            // Arrange
            ILogger logger = new TestLogger();

            // 1. Null -> no error
            var actual = TaskUtilities.TryGetConfig(null, logger);
            Assert.IsNull(actual);

            // 2. Empty -> no error
            actual = TaskUtilities.TryGetConfig(string.Empty, logger);
            Assert.IsNull(actual);

            // 3. Missing -> no error
            actual = TaskUtilities.TryGetConfig("c:\\missing\\dir", logger);
            Assert.IsNull(actual);
        }

        #endregion Tests

        #region Public test helpers

        /// <summary>
        /// Performs the specified operation against a locked analysis config file
        /// </summary>
        /// <param name="configFile">The config file to be read</param>
        /// <param name="op">The test operation to perform against the locked file</param>
        /// <param name="shouldTimeoutReadingConfig">When the operation should timeout or not</param>
        public static void PerformOpOnLockedFile(string configFile, System.Action op, bool shouldTimeoutReadingConfig)
        {
            Assert.IsTrue(File.Exists(configFile), "Test setup error: specified config file should exist: {0}", configFile);

            int lockPeriodInMilliseconds;
            if (shouldTimeoutReadingConfig)
            {
                lockPeriodInMilliseconds = TaskUtilities.MaxConfigRetryPeriodInMilliseconds + 600; // sleep for longer than the timeout period
            }
            else
            {
                // We'll lock the file and sleep for long enough for the retry period to occur, but
                // not so long that the task times out
                lockPeriodInMilliseconds = 1000;
                Assert.IsTrue(lockPeriodInMilliseconds < TaskUtilities.MaxConfigRetryPeriodInMilliseconds, "Test setup error: the test is sleeping for too long");
            }

            var testDuration = Stopwatch.StartNew();

            using (var lockingStream = File.OpenWrite(configFile))
            {
                System.Threading.Tasks.Task.Factory.StartNew(() =>
                {
                    System.Threading.Thread.Sleep(lockPeriodInMilliseconds);
                    lockingStream.Close();
                });

                // Perform the operation against the locked file
                op();
            }

            // Sanity check for our test code
            testDuration.Stop();
            var expectedMinimumLockPeriod = System.Math.Min(TaskUtilities.MaxConfigRetryPeriodInMilliseconds, lockPeriodInMilliseconds);
            Assert.IsTrue(testDuration.ElapsedMilliseconds >= expectedMinimumLockPeriod, "Test error: expecting the test to have taken at least {0} milliseconds to run. Actual: {1}",
                expectedMinimumLockPeriod, testDuration.ElapsedMilliseconds);
        }

        #endregion Public test helpers

        #region Private methods

        /// <summary>
        /// Ensures an analysis config file exists in the specified directory,
        /// replacing one if it already exists.
        /// If the supplied "regExExpression" is not null then the appropriate setting
        /// entry will be created in the file
        /// </summary>
        private static string CreateAnalysisConfig(string parentDir)
        {
            var fullPath = Path.Combine(parentDir, FileConstants.ConfigFileName);

            var config = new AnalysisConfig();
            config.Save(fullPath);
            return fullPath;
        }

        private static void AssertRetryAttempted(TestLogger logger)
        {
            // We'll assume retry has been attempted if there is a message containing
            // both of the timeout values
            logger.AssertSingleDebugMessageExists(TaskUtilities.MaxConfigRetryPeriodInMilliseconds.ToString(), TaskUtilities.DelayBetweenRetriesInMilliseconds.ToString());
        }

        #endregion Private methods
    }
}
