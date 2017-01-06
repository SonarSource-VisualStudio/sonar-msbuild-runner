﻿/*
 * SonarQube Scanner for MSBuild
 * Copyright (C) 2015-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using SonarQube.Common;
using System;
using System.Collections.Generic;

namespace SonarQube.TeamBuild.Integration
{
    public interface ICoverageReportDownloader // was internal
    {
        /// <summary>
        /// Downloads the specified files and returns a dictionary mapping the url to the name of the downloaded file
        /// </summary>
        /// <param name="tfsUri">The project collection URI</param>
        /// <param name="reportUrl">The file to be downloaded</param>
        /// <param name="downloadDir">The directory into which the files should be downloaded</param>
        /// <param name="newFileName">The name of the new file</param>
        /// <returns>True if the file was downloaded successfully, otherwise false</returns>
        bool DownloadReport(string tfsUri, string reportUrl, string newFullFileName, ILogger logger);
    }
}
