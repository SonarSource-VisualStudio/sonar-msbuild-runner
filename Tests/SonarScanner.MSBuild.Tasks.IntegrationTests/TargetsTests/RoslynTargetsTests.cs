﻿/*
 * SonarScanner for MSBuild
 * Copyright (C) 2016-2021 SonarSource SA
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

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarScanner.Integration.Tasks.IntegrationTests.TargetsTests;
using SonarScanner.MSBuild.Common;
using TestUtilities;

namespace SonarScanner.MSBuild.Tasks.IntegrationTests.TargetsTests
{
    [TestClass]
    public class RoslynTargetsTests
    {
        private const string RoslynAnalysisResultsSettingName = "sonar.cs.roslyn.reportFilePaths";
        private const string AnalyzerWorkDirectoryResultsSettingName = "sonar.cs.analyzer.projectOutPaths";
        private const string ErrorLogFilePattern = "{0}.RoslynCA.json";
        private const string TestSpecificImport = "<Import Project='$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), Capture.targets))Capture.targets' />";
        private const string TestSpecificProperties = @"<SonarQubeConfigPath>PROJECT_DIRECTORY_PATH</SonarQubeConfigPath>
                                                        <SonarQubeTempPath>PROJECT_DIRECTORY_PATH</SonarQubeTempPath>";

        public TestContext TestContext { get; set; }

        #region SetRoslynSettingsTarget tests

        [DataTestMethod]
        [DataRow("C#", false, "false")]
        [DataRow("C#", false, "true")]
        [DataRow("C#", true, "false")]
        [DataRow("VB", false, "false")]
        [DataRow("VB", false, "true")]
        [DataRow("VB", true, "false")]
        public void Settings_ValidSetup_ForAnalyzedProject(string msBuildLanguage, bool isTestProject, string excludeTestProjects)
        {
            // Arrange and Act
            var result = Execute_Settings_ValidSetup(msBuildLanguage, isTestProject, excludeTestProjects);

            // Assert
            AssertExpectedResolvedRuleset(result, $@"d:\{msBuildLanguage}-normal.ruleset");

            // Expecting all analyzers from the config file, but none from the project file
            AssertExpectedAnalyzers(result,
                $@"c:\1\SonarAnalyzer.{msBuildLanguage}.dll",
                @"c:\1\SonarAnalyzer.dll",
                @"c:\1\Google.Protobuf.dll",
                $@"c:\external.analyzer.{msBuildLanguage}.dll");

            // Expecting additional files from both config and project file
            AssertExpectedAdditionalFiles(result, "project.additional.file.1.txt", @"x:\aaa\project.additional.file.2.txt");
        }

        [DataTestMethod]
        [DataRow("C#")]
        [DataRow("VB")]
        public void Settings_ValidSetup_ForExcludedTestProject(string msBuildLanguage)
        {
            // Arrange and Act
            var result = Execute_Settings_ValidSetup(msBuildLanguage, true, "true");

            // Assert
            AssertExpectedResolvedRuleset(result, $@"d:\{msBuildLanguage}-deactivated.ruleset");

            // Expecting only the SonarC# analyzer
            AssertExpectedAnalyzers(result,
                $@"c:\1\SonarAnalyzer.{msBuildLanguage}.dll",
                @"c:\1\SonarAnalyzer.dll",
                @"c:\1\Google.Protobuf.dll");

            // Expecting only the additional files from the config file
            AssertExpectedAdditionalFiles(result);
        }

        [TestMethod]
        [Description("Checks any existing analyzers are overridden for projects using SonarQube pre-7.5")]
        public void Settings_ValidSetup_LegacyServer_Override_Analyzers()
        {
            // Arrange

            // Create a valid config containing analyzer settings
            var config = new AnalysisConfig
            {
                SonarQubeHostUrl = "http://sonarqube.com",
                SonarQubeVersion = "6.7", // legacy version
                AnalyzersSettings = new List<AnalyzerSettings>
                {
                    new AnalyzerSettings
                    {
                        Language = "cs",
                        RulesetPath = @"d:\my.ruleset",
                        AnalyzerPlugins = new List<AnalyzerPlugin>
                        {
                            CreateAnalyzerPlugin(@"c:\data\new.analyzer1.dll"),
                            CreateAnalyzerPlugin(@"c:\new.analyzer2.dll")
                        },
                        AdditionalFilePaths = new List<string> { @"c:\config.1.txt", @"c:\config.2.txt" }
                    }
                }
            };

            var testSpecificProjectXml = @"
  <PropertyGroup>
    <ResolvedCodeAnalysisRuleSet>c:\should.be.overridden.ruleset</ResolvedCodeAnalysisRuleSet>
    <Language>C#</Language>
  </PropertyGroup>

  <ItemGroup>
    <!-- all analyzers specified in the project file should be removed -->
    <Analyzer Include='c:\should.be.removed.analyzer2.dll' />
    <Analyzer Include='should.be.removed.analyzer1.dll' />
  </ItemGroup>
  <ItemGroup>
    <!-- These additional files don't match ones in the config and should be preserved -->
    <AdditionalFiles Include='should.not.be.removed.additional1.txt' />
    <AdditionalFiles Include='should.not.be.removed.additional2.txt' />

    <!-- This additional file matches one in the config and should be replaced -->
    <AdditionalFiles Include='should.be.removed/CONFIG.1.TXT' />
    <AdditionalFiles Include='should.be.removed\CONFIG.2.TXT' />

  </ItemGroup>
";

            var filePath = CreateProjectFile(config, testSpecificProjectXml);

            // Act
            var result = BuildRunner.BuildTargets(TestContext, filePath, TargetConstants.OverrideRoslynAnalysisTarget);

            // Assert
            result.AssertTargetExecuted(TargetConstants.CreateProjectSpecificDirs);
            result.AssertTargetExecuted(TargetConstants.OverrideRoslynAnalysisTarget);
            result.AssertTargetExecuted(TargetConstants.SetRoslynAnalysisPropertiesTarget);
            result.BuildSucceeded.Should().BeTrue();

            // Check the error log and ruleset properties are set
            AssertErrorLogIsSetBySonarQubeTargets(result);
            AssertExpectedResolvedRuleset(result, @"d:\my.ruleset");
            AssertExpectedAdditionalFiles(result, "should.not.be.removed.additional1.txt", "should.not.be.removed.additional2.txt");
            AssertExpectedAnalyzers(result, @"c:\data\new.analyzer1.dll", @"c:\new.analyzer2.dll");
            AssertWarningsAreNotTreatedAsErrorsNorIgnored(result);
            AssertRunAnalyzersIsEnabled(result);
        }

        [TestMethod]
        [Description("Checks existing analysis settings are merged for projects using SonarQube 7.5+")]
        public void Settings_ValidSetup_NonLegacyServer_MergeSettings()
        {
            // Arrange

            var dir = TestUtils.CreateTestSpecificFolderWithSubPaths(TestContext);
            var dummyQpRulesetPath = TestUtils.CreateValidEmptyRuleset(dir, "dummyQp");

            // Create a valid config containing analyzer settings
            var config = new AnalysisConfig
            {
                SonarQubeVersion = "7.5", // non-legacy version
                ServerSettings = new AnalysisProperties
                {
                    new Property { Id = "sonar.cs.roslyn.ignoreIssues", Value = "false" }
                },
                AnalyzersSettings = new List<AnalyzerSettings>
                {
                    new AnalyzerSettings
                    {
                        Language = "cs",
                        RulesetPath = dummyQpRulesetPath,
                        AnalyzerPlugins = new List<AnalyzerPlugin>
                        {
                            CreateAnalyzerPlugin(@"c:\data\new\analyzer1.dll", @"c:\new.analyzer2.dll")
                        },
                        AdditionalFilePaths = new List<string> { @"c:\config.1.txt", @"c:\config.2.txt" }
                    }
                }
            };

            var testSpecificProjectXml = @"
  <PropertyGroup>
    <ResolvedCodeAnalysisRuleSet>c:\original.ruleset</ResolvedCodeAnalysisRuleSet>
    <Language>C#</Language>
  </PropertyGroup>

  <ItemGroup>
    <!-- all analyzers specified in the project file should be preserved -->
    <Analyzer Include='c:\original\should.be.removed\analyzer1.dll' />
    <Analyzer Include='original\should.be.preserved\analyzer3.dll' />
  </ItemGroup>
  <ItemGroup>
    <!-- These additional files don't match ones in the config and should be preserved -->
    <AdditionalFiles Include='should.not.be.removed.additional1.txt' />
    <AdditionalFiles Include='should.not.be.removed.additional2.txt' />

    <!-- This additional file matches one in the config and should be replaced -->
    <AdditionalFiles Include='d:/duplicate.should.be.removed/CONFIG.1.TXT' />
    <AdditionalFiles Include='d:\duplicate.should.be.removed\config.2.TXT' />

  </ItemGroup>
";

            var filePath = CreateProjectFile(config, testSpecificProjectXml);

            // Act
            var result = BuildRunner.BuildTargets(TestContext, filePath, TargetConstants.OverrideRoslynAnalysisTarget);

            // Assert
            result.AssertTargetExecuted(TargetConstants.CreateProjectSpecificDirs);
            result.AssertTargetExecuted(TargetConstants.OverrideRoslynAnalysisTarget);
            result.AssertTargetExecuted(TargetConstants.SetRoslynAnalysisPropertiesTarget);
            result.BuildSucceeded.Should().BeTrue();

            // Check the error log and ruleset properties are set
            AssertErrorLogIsSetBySonarQubeTargets(result);

            var actualProjectSpecificConfFolder = result.GetCapturedPropertyValue(TargetProperties.ProjectSpecificConfDir);
            Directory.Exists(actualProjectSpecificConfFolder).Should().BeTrue();

            var expectedMergedRuleSetFilePath = Path.Combine(actualProjectSpecificConfFolder, "merged.ruleset");
            AssertExpectedResolvedRuleset(result, expectedMergedRuleSetFilePath);
            RuleSetAssertions.CheckMergedRulesetFile(actualProjectSpecificConfFolder, @"c:\original.ruleset");

            AssertExpectedAdditionalFiles(result,
                "should.not.be.removed.additional1.txt",
                "should.not.be.removed.additional2.txt");

            AssertExpectedAnalyzers(result,
                @"c:\data\new\analyzer1.dll",
                @"c:\new.analyzer2.dll",
                @"original\should.be.preserved\analyzer3.dll");

            AssertWarningsAreNotTreatedAsErrorsNorIgnored(result);
            AssertRunAnalyzersIsEnabled(result);
        }

        [TestMethod]
        public void Settings_LanguageMissing_NoError()
        {
            // Arrange

            // Set the config directory so the targets know where to look for the analysis config file
            var confDir = TestUtils.CreateTestSpecificFolderWithSubPaths(TestContext, "config");

            // Create a valid config file that does not contain analyzer settings
            var config = new AnalysisConfig();
            var configFilePath = Path.Combine(confDir, FileConstants.ConfigFileName);
            config.Save(configFilePath);

            // Create the project
            var projectSnippet = $@"
<PropertyGroup>
  <SonarQubeConfigPath>{confDir}</SonarQubeConfigPath>
  <ResolvedCodeAnalysisRuleset>c:\should.be.overridden.ruleset</ResolvedCodeAnalysisRuleset>
  <Language />
</PropertyGroup>

<ItemGroup>
  <Analyzer Include='should.be.removed.analyzer1.dll' />
  <AdditionalFiles Include='should.not.be.removed.additional1.txt' />
</ItemGroup>
";


            var filePath = CreateProjectFile(config, projectSnippet);

            // Act
            var result = BuildRunner.BuildTargets(TestContext, filePath, TargetConstants.OverrideRoslynAnalysisTarget);

            // Assert
            result.AssertTargetExecuted(TargetConstants.OverrideRoslynAnalysisTarget);
            result.AssertTargetExecuted(TargetConstants.SetRoslynAnalysisPropertiesTarget);
            result.BuildSucceeded.Should().BeTrue();

            result.MessageLog.Should().Contain("Analysis language is not specified");

            AssertErrorLogIsSetBySonarQubeTargets(result);
            AssertExpectedResolvedRuleset(result, string.Empty);
            result.AssertExpectedItemGroupCount(TargetProperties.AnalyzerItemType, 0);
            AssertExpectedItemValuesExists(result, TargetProperties.AdditionalFilesItemType, new[] {
                result.GetCapturedPropertyValue(TargetProperties.SonarProjectOutFolderFilePath),
                result.GetCapturedPropertyValue(TargetProperties.SonarProjectConfigFilePath),
                "should.not.be.removed.additional1.txt" /* additional files are not removed */
            });
        }

        [TestMethod]
        [Description("Checks that a config file with no analyzer settings does not cause an issue")]
        public void Settings_SettingsMissing_NoError()
        {
            // Arrange

            // Set the config directory so the targets know where to look for the analysis config file
            var confDir = TestUtils.CreateTestSpecificFolderWithSubPaths(TestContext, "config");

            // Create a valid config file that does not contain analyzer settings
            var config = new AnalysisConfig();
            var configFilePath = Path.Combine(confDir, FileConstants.ConfigFileName);
            config.Save(configFilePath);

            // Create the project
            var projectSnippet = $@"
<PropertyGroup>
  <SonarQubeConfigPath>{confDir}</SonarQubeConfigPath>
  <ResolvedCodeAnalysisRuleset>c:\should.be.overridden.ruleset</ResolvedCodeAnalysisRuleset>
</PropertyGroup>

<ItemGroup>
  <Analyzer Include='should.be.removed.analyzer1.dll' />
  <AdditionalFiles Include='should.not.be.removed.additional1.txt' />
</ItemGroup>
";
            var filePath = CreateProjectFile(config, projectSnippet);

            // Act
            var result = BuildRunner.BuildTargets(TestContext, filePath, TargetConstants.OverrideRoslynAnalysisTarget);

            // Assert
            result.AssertTargetExecuted(TargetConstants.OverrideRoslynAnalysisTarget);
            result.AssertTargetExecuted(TargetConstants.SetRoslynAnalysisPropertiesTarget);
            result.BuildSucceeded.Should().BeTrue();

            // Check the error log and ruleset properties are set
            AssertErrorLogIsSetBySonarQubeTargets(result);
            AssertExpectedResolvedRuleset(result, string.Empty);
            result.AssertExpectedItemGroupCount(TargetProperties.AnalyzerItemType, 0);
            AssertExpectedItemValuesExists(result, TargetProperties.AdditionalFilesItemType, new[] {
                result.GetCapturedPropertyValue(TargetProperties.SonarProjectOutFolderFilePath),
                result.GetCapturedPropertyValue(TargetProperties.SonarProjectConfigFilePath),
                "should.not.be.removed.additional1.txt" /* additional files are not removed any longer */
            });
        }

        [TestMethod]
        [Description("Checks the target is not executed if the temp folder is not set")]
        public void Settings_TempFolderIsNotSet()
        {
            // Arrange
            var projectSnippet = @"
<PropertyGroup>
  <ErrorLog>pre-existing.log</ErrorLog>
  <ResolvedCodeAnalysisRuleset>pre-existing.ruleset</ResolvedCodeAnalysisRuleset>
  <WarningsAsErrors>CS101</WarningsAsErrors>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <RunAnalyzers>false</RunAnalyzers>
  <RunAnalyzersDuringBuild>false</RunAnalyzersDuringBuild>

  <!-- This will override the value that was set earlier in the project file -->
  <SonarQubeTempPath />
</PropertyGroup>
";

            var filePath = CreateProjectFile(null, projectSnippet);

            // Act
            var result = BuildRunner.BuildTargets(TestContext, filePath, TargetConstants.OverrideRoslynAnalysisTarget);

            // Assert
            result.AssertTargetNotExecuted(TargetConstants.OverrideRoslynAnalysisTarget);
            result.AssertTargetNotExecuted(TargetConstants.SetRoslynAnalysisPropertiesTarget);

            // Existing properties should not be changed
            AssertExpectedErrorLog(result, "pre-existing.log");
            AssertExpectedResolvedRuleset(result, "pre-existing.ruleset");
            result.AssertExpectedItemGroupCount(TargetProperties.AnalyzerItemType, 0);
            result.AssertExpectedItemGroupCount(TargetProperties.AdditionalFilesItemType, 0);

            // Properties are not overriden
            result.AssertExpectedCapturedPropertyValue(TargetProperties.TreatWarningsAsErrors, "true");
            result.AssertExpectedCapturedPropertyValue(TargetProperties.WarningsAsErrors, "CS101");
            result.AssertExpectedCapturedPropertyValue(TargetProperties.RunAnalyzers, "false");
            result.AssertExpectedCapturedPropertyValue(TargetProperties.RunAnalyzersDuringBuild, "false");
        }

        [TestMethod]
        [Description("Checks an existing errorLog value is used if set")]
        public void Settings_ErrorLogAlreadySet()
        {
            // Arrange
            var projectSnippet = @"
<PropertyGroup>
  <ErrorLog>already.set.txt</ErrorLog>
</PropertyGroup>
";

            var filePath = CreateProjectFile(null, projectSnippet);

            // Act
            var result = BuildRunner.BuildTargets(TestContext, filePath, TargetConstants.OverrideRoslynAnalysisTarget);

            // Assert
            result.AssertTargetExecuted(TargetConstants.OverrideRoslynAnalysisTarget);
            result.AssertTargetExecuted(TargetConstants.SetRoslynAnalysisPropertiesTarget);
            result.BuildSucceeded.Should().BeTrue();

            result.AssertExpectedCapturedPropertyValue(TargetProperties.ErrorLog, "already.set.txt");
        }

        [TestMethod]
        [Description("Checks the code analysis properties are cleared for excludedprojects")]
        public void Settings_NotRunForExcludedProject()
        {
            // Arrange
            var projectSnippet = @"
<PropertyGroup>
  <SonarQubeExclude>TRUE</SonarQubeExclude>
  <ResolvedCodeAnalysisRuleset>Dummy value</ResolvedCodeAnalysisRuleset>
</PropertyGroup>
";
            var filePath = CreateProjectFile(null, projectSnippet);

            // Act
            var result = BuildRunner.BuildTargets(TestContext, filePath, TargetConstants.OverrideRoslynAnalysisTarget);

            // Assert
            result.AssertTargetExecuted(TargetConstants.OverrideRoslynAnalysisTarget);
            result.AssertTargetNotExecuted(TargetConstants.SetRoslynAnalysisPropertiesTarget);
            result.BuildSucceeded.Should().BeTrue();

            result.AssertExpectedCapturedPropertyValue("ResolvedCodeAnalysisRuleset", "Dummy value");
        }

        #endregion SetRoslynSettingsTarget tests

        #region AddAnalysisResults tests

        [TestMethod]
        [Description("Checks the target is not executed if the temp folder is not set")]
        public void SetResults_TempFolderIsNotSet()
        {
            // Arrange
            var projectSnippet = @"
<PropertyGroup>
  <SonarQubeTempPath />
</PropertyGroup>
";

            var filePath = CreateProjectFile(null, projectSnippet);

            // Act
            var result = BuildRunner.BuildTargets(TestContext, filePath, TargetConstants.OverrideRoslynAnalysisTarget);

            // Assert
            result.AssertTargetNotExecuted(TargetConstants.SetRoslynResultsTarget);
        }

        [TestMethod]
        [Description("Checks the analysis setting is not set if the results file does not exist")]
        public void SetResults_ResultsFileDoesNotExist()
        {
            // Arrange
            var rootInputFolder = TestUtils.CreateTestSpecificFolderWithSubPaths(TestContext, "Inputs");

            var projectSnippet = $@"
<PropertyGroup>
  <SonarQubeTempPath>{rootInputFolder}</SonarQubeTempPath>
</PropertyGroup>
";

            var filePath = CreateProjectFile(null, projectSnippet);

            // Act
            var result = BuildRunner.BuildTargets(TestContext, filePath, TargetConstants.SetRoslynResultsTarget);

            // Assert
            result.AssertTargetExecuted(TargetConstants.SetRoslynResultsTarget);
            AssertAnalysisSettingDoesNotExist(result, RoslynAnalysisResultsSettingName);
        }

        [TestMethod]
        [Description("Checks the analysis setting is set if the result file exists")]
        public void SetResults_ResultsFileExists()
        {
            // Arrange
            var rootInputFolder = TestUtils.CreateTestSpecificFolderWithSubPaths(TestContext, "Inputs");
            var resultsFile = TestUtils.CreateTextFile(rootInputFolder, "error.report.txt", "dummy report content");

            var projectSnippet = $@"
<PropertyGroup>
  <SonarQubeTempPath>{rootInputFolder}</SonarQubeTempPath>
  <SonarCompileErrorLog>{resultsFile}</SonarCompileErrorLog>
</PropertyGroup>
";
            var filePath = CreateProjectFile(null, projectSnippet);

            // Act
            var result = BuildRunner.BuildTargets(TestContext, filePath, TargetConstants.CreateProjectSpecificDirs, TargetConstants.SetRoslynResultsTarget);

            var projectSpecificOutDir = result.GetCapturedPropertyValue(TargetProperties.ProjectSpecificOutDir);

            // Assert
            result.AssertTargetExecuted(TargetConstants.CreateProjectSpecificDirs);
            result.AssertTargetExecuted(TargetConstants.SetRoslynResultsTarget);
            AssertExpectedAnalysisSetting(result, RoslynAnalysisResultsSettingName, resultsFile);
            AssertExpectedAnalysisSetting(result, AnalyzerWorkDirectoryResultsSettingName, projectSpecificOutDir);
        }

        [TestMethod]
        [Description("Checks the analysis settings are set if the normal Roslyn and the Razor result files exist")]
        public void SetResults_BothResultsFilesCreated()
        {
            // Arrange
            var rootInputFolder = TestUtils.CreateTestSpecificFolderWithSubPaths(TestContext, "Inputs");

            var resultsFile = TestUtils.CreateTextFile(rootInputFolder, "error.report.txt", "dummy report content");
            var razorResultsFile = TestUtils.CreateTextFile(rootInputFolder, "razor.error.report.txt", "dummy report content");

            var projectSnippet = $@"
<PropertyGroup>
  <SonarQubeTempPath>{rootInputFolder}</SonarQubeTempPath>
  <SonarCompileErrorLog>{resultsFile}</SonarCompileErrorLog>
  <RazorSonarCompileErrorLog>{razorResultsFile}</RazorSonarCompileErrorLog>
</PropertyGroup>
";

            var filePath = CreateProjectFile(null, projectSnippet);

            // Act
            var result = BuildRunner.BuildTargets(TestContext, filePath, TargetConstants.CreateProjectSpecificDirs, TargetConstants.SetRoslynResultsTarget);

            var projectSpecificOutDir = result.GetCapturedPropertyValue(TargetProperties.ProjectSpecificOutDir);

            // Assert
            result.AssertTargetExecuted(TargetConstants.CreateProjectSpecificDirs);
            result.AssertTargetExecuted(TargetConstants.SetRoslynResultsTarget);
            AssertExpectedAnalysisSetting(result, RoslynAnalysisResultsSettingName, resultsFile + "|" + razorResultsFile);
            AssertExpectedAnalysisSetting(result, AnalyzerWorkDirectoryResultsSettingName, projectSpecificOutDir);
        }

        #endregion AddAnalysisResults tests

        #region Combined tests

        [TestMethod]
        [Description("Checks the targets are executed in the expected order")]
        public void TargetExecutionOrder()
        {
            // Arrange
            var rootInputFolder = TestUtils.CreateTestSpecificFolderWithSubPaths(TestContext, "Inputs");
            var rootOutputFolder = TestUtils.CreateTestSpecificFolderWithSubPaths(TestContext, "Outputs");

            // We need to set the CodeAnalyisRuleSet property if we want ResolveCodeAnalysisRuleSet
            // to be executed. See test bug https://github.com/SonarSource/sonar-scanner-msbuild/issues/776
            var dummyQpRulesetPath = TestUtils.CreateValidEmptyRuleset(rootInputFolder, "dummyQp");

            var projectSnippet = $@"
<PropertyGroup>
  <SonarQubeTempPath>{rootInputFolder}</SonarQubeTempPath>
  <SonarQubeOutputPath>{rootInputFolder}</SonarQubeOutputPath>
  <SonarQubeConfigPath>{rootOutputFolder}</SonarQubeConfigPath>
  <CodeAnalysisRuleSet>{dummyQpRulesetPath}</CodeAnalysisRuleSet>
</PropertyGroup>

<ItemGroup>
  <SonarQubeSetting Include='sonar.other.setting'>
    <Value>other value</Value>
  </SonarQubeSetting>
</ItemGroup>
";

            var filePath = CreateProjectFile(null, projectSnippet);

            // Act
            var result = BuildRunner.BuildTargets(TestContext, filePath, TargetConstants.DefaultBuildTarget);

            // Assert
            // Checks that should succeed irrespective of the MSBuild version
            result.AssertTargetSucceeded(TargetConstants.DefaultBuildTarget);
            result.AssertTargetExecuted(TargetConstants.OverrideRoslynAnalysisTarget);

            result.AssertExpectedTargetOrdering(
                TargetConstants.ResolveReferencesTarget,
                TargetConstants.ResolveCodeAnalysisRuleSet,
                TargetConstants.CategoriseProjectTarget,
                TargetConstants.OverrideRoslynAnalysisTarget,
                TargetConstants.SetRoslynAnalysisPropertiesTarget,
                TargetConstants.CoreCompile,
                TargetConstants.DefaultBuildTarget,
                TargetConstants.SetRoslynResultsTarget,
                TargetConstants.WriteProjectDataTarget);
        }

        #endregion Combined tests

        #region Checks

        /// <summary>
        /// Checks the error log property has been set to the value supplied in the targets file
        /// </summary>
        private static void AssertErrorLogIsSetBySonarQubeTargets(BuildLog result)
        {
            var targetDir = result.GetCapturedPropertyValue(TargetProperties.TargetDir);
            var targetFileName = result.GetCapturedPropertyValue(TargetProperties.TargetFileName);
            var expectedErrorLog = Path.Combine(targetDir, string.Format(CultureInfo.InvariantCulture, ErrorLogFilePattern, targetFileName));
            AssertExpectedErrorLog(result, expectedErrorLog);
        }

        private static void AssertExpectedErrorLog(BuildLog result, string expectedErrorLog) =>
            result.AssertExpectedCapturedPropertyValue(TargetProperties.ErrorLog, expectedErrorLog);

        private static void AssertExpectedResolvedRuleset(BuildLog result, string expectedResolvedRuleset) =>
            result.AssertExpectedCapturedPropertyValue(TargetProperties.ResolvedCodeAnalysisRuleset, expectedResolvedRuleset);

        private void AssertExpectedItemValuesExists(BuildLog result, string itemType, params string[] expectedValues)
        {
            DumpLists(result, itemType, expectedValues);
            foreach (var expectedValue in expectedValues)
            {
                result.AssertSingleItemExists(itemType, expectedValue);
            }
            result.AssertExpectedItemGroupCount(itemType, expectedValues.Length);
        }

        private void AssertExpectedAnalyzers(BuildLog result, params string[] expected) =>
            AssertExpectedItemValuesExists(result, TargetProperties.AnalyzerItemType, expected);

        private void AssertExpectedAdditionalFiles(BuildLog result, params string[] testSpecificAdditionalFiles)
        {
            var projectSetupAdditionalFiles = new[] { @"c:\config.1.txt", @"c:\config.2.txt" };
            var projectSpecificOutFolderFilePath = result.GetCapturedPropertyValue(TargetProperties.SonarProjectOutFolderFilePath);
            var projectSpecificConfigFilePath = result.GetCapturedPropertyValue(TargetProperties.SonarProjectConfigFilePath);
            var allExpectedAdditionalFiles = projectSetupAdditionalFiles.Concat(testSpecificAdditionalFiles).Concat(new[] { projectSpecificOutFolderFilePath, projectSpecificConfigFilePath });
            AssertExpectedItemValuesExists(result, TargetProperties.AdditionalFilesItemType, allExpectedAdditionalFiles.ToArray());
        }

        private void DumpLists(BuildLog actualResult, string itemType, string[] expected)
        {
            TestContext.WriteLine("");
            TestContext.WriteLine("Dumping <" + itemType + "> list: expected");
            foreach (var item in expected)
            {
                TestContext.WriteLine("\t{0}", item);
            }
            TestContext.WriteLine("");

            TestContext.WriteLine("");
            TestContext.WriteLine("Dumping <" + itemType + "> list: actual");
            foreach (var item in actualResult.GetCapturedItemValues(itemType))
            {
                TestContext.WriteLine("\t{0}", item.Value);
            }
            TestContext.WriteLine("");
        }

        /// <summary>
        /// Checks that no analysis warnings will be treated as errors nor will they be ignored
        /// </summary>
        private static void AssertWarningsAreNotTreatedAsErrorsNorIgnored(BuildLog actualResult)
        {
            actualResult.AssertExpectedCapturedPropertyValue(TargetProperties.TreatWarningsAsErrors, "false");
            actualResult.AssertExpectedCapturedPropertyValue(TargetProperties.WarningsAsErrors, "");
            actualResult.AssertExpectedCapturedPropertyValue(TargetProperties.WarningLevel, "4");
        }

        /// <summary>
        /// Checks that VS2019 properties are set to run the analysis.
        /// </summary>
        private static void AssertRunAnalyzersIsEnabled(BuildLog actualResult)
        {
            actualResult.AssertExpectedCapturedPropertyValue(TargetProperties.RunAnalyzers, "true");
            actualResult.AssertExpectedCapturedPropertyValue(TargetProperties.RunAnalyzersDuringBuild, "true");
        }

        /// <summary>
        /// Checks that a SonarQubeSetting does not exist
        /// </summary>
        private static void AssertAnalysisSettingDoesNotExist(BuildLog actualResult, string settingName)
        {
            var matches = actualResult.GetCapturedItemValues(BuildTaskConstants.SettingItemName);

            matches.Should().BeEmpty("Not expected SonarQubeSetting with include value of '{0}' to exist. Actual occurrences: {1}", settingName, matches.Count());
        }

        /// <summary>
        /// Checks whether there is a single "SonarQubeSetting" item with the expected name and setting value
        /// </summary>
        private static void AssertExpectedAnalysisSetting(BuildLog actualResult, string settingName, string expectedValue)
        {
            /* The equivalent XML would look like this:
            <ItemGroup>
              <SonarQubeSetting Include="settingName">
                <Value>expectedValue</Value
              </SonarQubeSetting>
            </ItemGroup>
            */

            var settings = actualResult.GetCapturedItemValues(BuildTaskConstants.SettingItemName);
            settings.Should().NotBeEmpty();

            var matches = settings.Where(v => v.Value.Equals(settingName, System.StringComparison.Ordinal)).ToList();
            matches.Should().ContainSingle($"Only one and only expecting one SonarQubeSetting with include value of '{0}' to exist. Count: {matches.Count}", settingName);

            var item = matches[0];
            var value = item.Metadata.SingleOrDefault(v => v.Name.Equals(BuildTaskConstants.SettingValueMetadataName));

            value.Should().NotBeNull();
            value.Value.Should().Be(expectedValue, "SonarQubeSetting with include value '{0}' does not have the expected value", settingName);
        }

        #endregion Checks

        #region Setup

        private BuildLog Execute_Settings_ValidSetup(string msBuildLanguage, bool isTestProject, string excludeTestProject)
        {
            // Create a valid config file containing analyzer settings for both VB and C#
            var config = new AnalysisConfig
            {
                SonarQubeHostUrl = "http://sonarqube.com",
                SonarQubeVersion = "8.9", // Latest behavior, test code is analyzed by default.
                ServerSettings = new AnalysisProperties
                {
                    new Property { Id = "sonar.cs.roslyn.ignoreIssues", Value="true" },
                    new Property { Id = "sonar.vbnet.roslyn.ignoreIssues", Value="true" }
                },
                LocalSettings = new AnalysisProperties
                {
                    new Property { Id = "sonar.dotnet.excludeTestProjects", Value = excludeTestProject }
                },
                AnalyzersSettings = new List<AnalyzerSettings>
                    {
                        // C#
                        new AnalyzerSettings
                        {
                            Language = "cs",
                            RulesetPath = @"d:\C#-normal.ruleset",
                            DeactivatedRulesetPath = @"d:\C#-deactivated.ruleset",
                            AnalyzerPlugins = new List<AnalyzerPlugin>
                            {
                                new AnalyzerPlugin("csharp", "v1", "resName", new [] { @"c:\1\SonarAnalyzer.C#.dll", @"c:\1\SonarAnalyzer.dll", @"c:\1\Google.Protobuf.dll" }),
                                new AnalyzerPlugin("external-cs", "v1", "resName", new [] { @"c:\external.analyzer.C#.dll" })
                            },
                            AdditionalFilePaths = new List<string> { @"c:\config.1.txt", @"c:\config.2.txt" }
                        },

                        // VB
                        new AnalyzerSettings
                        {
                            Language = "vbnet",
                            RulesetPath = @"d:\VB-normal.ruleset",
                            DeactivatedRulesetPath = @"d:\VB-deactivated.ruleset",
                            AnalyzerPlugins = new List<AnalyzerPlugin>
                            {
                                new AnalyzerPlugin("vbnet", "v1", "resName", new [] { @"c:\1\SonarAnalyzer.VB.dll", @"c:\1\SonarAnalyzer.dll", @"c:\1\Google.Protobuf.dll" }),
                                new AnalyzerPlugin("external-vb", "v1", "resName", new [] { @"c:\external.analyzer.VB.dll" })
                            },
                            AdditionalFilePaths = new List<string> { @"c:\config.1.txt", @"c:\config.2.txt" }
                        }
                    }
            };

            // Create the project
            var projectSnippet = $@"
<PropertyGroup>
    <Language>{msBuildLanguage}</Language>
    <SonarQubeTestProject>{isTestProject}</SonarQubeTestProject>
    <ResolvedCodeAnalysisRuleset>c:\should.be.overridden.ruleset</ResolvedCodeAnalysisRuleset>
    <!-- These should be overriden by the targets file -->
    <RunAnalyzers>false</RunAnalyzers>
    <RunAnalyzersDuringBuild>false</RunAnalyzersDuringBuild>
</PropertyGroup>

<ItemGroup>
    <Analyzer Include='project.additional.analyzer1.dll' />
    <Analyzer Include='c:\project.additional.analyzer2.dll' />
    <AdditionalFiles Include='project.additional.file.1.txt' />
    <AdditionalFiles Include='x:\aaa\project.additional.file.2.txt' />
</ItemGroup>
";

            var filePath = CreateProjectFile(config, projectSnippet);

            // Act
            var result = BuildRunner.BuildTargets(TestContext, filePath, TargetConstants.OverrideRoslynAnalysisTarget);

            // Assert - check invariants
            result.AssertTargetExecuted(TargetConstants.OverrideRoslynAnalysisTarget);
            result.AssertTargetExecuted(TargetConstants.SetRoslynAnalysisPropertiesTarget);
            result.AssertTaskExecuted("GetAnalyzerSettings");
            result.BuildSucceeded.Should().BeTrue();

            AssertErrorLogIsSetBySonarQubeTargets(result);
            AssertWarningsAreNotTreatedAsErrorsNorIgnored(result);
            AssertRunAnalyzersIsEnabled(result);

            var capturedProjectSpecificConfDir = result.GetCapturedPropertyValue(TargetProperties.ProjectSpecificConfDir);
            result.MessageLog.Should().Contain($@"Sonar: ({Path.GetFileName(filePath)}) Analysis configured successfully with {capturedProjectSpecificConfDir}\SonarProjectConfig.xml.");

            return result;
        }

        private string CreateProjectFile(AnalysisConfig config, string projectSnippet)
        {
            var afterTargets = string.Join(";",
                TargetConstants.SetRoslynResultsTarget,
                TargetConstants.OverrideRoslynAnalysisTarget,
                TargetConstants.SetRoslynAnalysisPropertiesTarget
                );

            var projectDirectory = TestUtils.CreateTestSpecificFolderWithSubPaths(TestContext);
            var targetTestUtils = new TargetsTestsUtils(TestContext);
            var projectTemplate = targetTestUtils.GetProjectTemplate(config, projectDirectory, TestSpecificProperties, projectSnippet, TestSpecificImport);

            targetTestUtils.CreateCaptureDataTargetsFile(projectDirectory, afterTargets);

            return targetTestUtils.CreateProjectFile(projectDirectory, projectTemplate);
        }

        private static AnalyzerPlugin CreateAnalyzerPlugin(params string[] fileList) =>
            new AnalyzerPlugin
            {
                AssemblyPaths = new List<string>(fileList)
            };

        #endregion Setup
    }
}
