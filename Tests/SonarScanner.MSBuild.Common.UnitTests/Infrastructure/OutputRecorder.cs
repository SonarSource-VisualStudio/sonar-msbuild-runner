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

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarScanner.MSBuild.Common.UnitTests
{
    /// <summary>
    /// Test implementation of <see cref="IOutputWriter"/> that records the output messages
    /// </summary>
    internal class OutputRecorder : IOutputWriter
    {
        private class OutputMessage
        {
            public OutputMessage(string message, ConsoleColor textColor, bool isError)
            {
                Message = message;
                TextColor = textColor;
                IsError = isError;
            }

            public string Message { get; }
            public ConsoleColor TextColor { get; }
            public bool IsError { get; }
        }

        private readonly List<OutputMessage> outputMessages = new List<OutputMessage>();

        #region Checks

        public void AssertNoOutput()
        {
            Assert.AreEqual(0, outputMessages.Count, "Not expecting any output to have been written to the console");
        }

        public void AssertExpectedLastOutput(string message, ConsoleColor textColor, bool isError)
        {
            Assert.IsTrue(outputMessages.Any(), "Expecting some output to have been written to the console");

            var lastMessage = outputMessages.Last();

            Assert.AreEqual(message, lastMessage.Message, "Unexpected message content");
            Assert.AreEqual(textColor, lastMessage.TextColor, "Unexpected text color");
            Assert.AreEqual(isError, lastMessage.IsError, "Unexpected output stream");
        }

        public void AssertExpectedOutputText(params string[] messages)
        {
            CollectionAssert.AreEqual(messages, outputMessages.Select(om => om.Message).ToArray(), "Unexpected output messages");
        }

        #endregion Checks

        #region IOutputWriter methods

        public void WriteLine(string message, ConsoleColor textColor, bool isError)
        {
            outputMessages.Add(new OutputMessage(message, textColor, isError));

            // Dump to the console to assist debugging
            Console.WriteLine("IsError: {0}, TextColor: {1}, Message: {2}", isError, textColor.ToString(), message);
        }

        #endregion IOutputWriter methods
    }
}
