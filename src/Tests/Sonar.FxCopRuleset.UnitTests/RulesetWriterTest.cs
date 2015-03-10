﻿//-----------------------------------------------------------------------
// <copyright file="RulesetWriterTest.cs" company="SonarSource SA and Microsoft Corporation">
//   (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------


using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace Sonar.FxCopRuleset.UnitTests
{
    [TestClass]
    public class RulesetWriterTest
    {
        [TestMethod]
        public void RulesetWriterToString()
        {
            List<string> ids = new List<string>();
            ids.Add("CA1000");
            ids.Add("MyCustomCheckId");

            string actual = RulesetWriter.ToString(ids);

            StringBuilder expected = new StringBuilder();
            expected.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            expected.AppendLine("<RuleSet Name=\"SonarQube\" Description=\"Rule set generated by SonarQube\" ToolsVersion=\"12.0\">");
            expected.AppendLine("  <Rules AnalyzerId=\"Microsoft.Analyzers.ManagedCodeAnalysis\" RuleNamespace=\"Microsoft.Rules.Managed\">");
            expected.AppendLine("    <Rule Id=\"CA1000\" Action=\"Warning\" />");
            expected.AppendLine("    <Rule Id=\"MyCustomCheckId\" Action=\"Warning\" />");
            expected.AppendLine("  </Rules>");
            expected.AppendLine("</RuleSet>");

            Assert.AreEqual(expected.ToString(), actual);
        }
    }
}
