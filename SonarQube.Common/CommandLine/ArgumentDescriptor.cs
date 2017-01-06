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

using System;
using System.Diagnostics;

namespace SonarQube.Common
{
    /// <summary>
    /// Data class that describes a single valid command line argument - id, prefixes, multiplicity etc
    /// </summary>
    [DebuggerDisplay("{id}")]
    public class ArgumentDescriptor
    {
        // https://msdn.microsoft.com/en-us/library/ms973919.aspx
        // "[d]ata that is designed to be culture-agnostic and linguistically irrelevant should... 
        //  use either StringComparison.Ordinal or StringComparison.OrdinalIgnoreCase..."
        public static readonly StringComparer IdComparer = StringComparer.Ordinal;
        public static readonly StringComparison IdComparison = StringComparison.Ordinal;

        private readonly string id;
        private readonly string[] prefixes;
        private readonly bool required;
        private readonly string description;
        private readonly bool allowMultiple;
        private readonly bool isVerb;

        public ArgumentDescriptor(string id, string[] prefixes, bool required, string description, bool allowMultiple)
            : this(id, prefixes, required, description, allowMultiple, false /* not a verb */)
        {
        }

        public ArgumentDescriptor(string id, string[] prefixes, bool required, string description, bool allowMultiple, bool isVerb)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }
            if (prefixes == null || prefixes.Length == 0)
            {
                throw new ArgumentNullException("prefixes");
            }
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentNullException("description");
            }

            this.id = id;
            this.prefixes = prefixes;
            this.required = required;
            this.description = description;
            this.allowMultiple = allowMultiple;
            this.isVerb = isVerb;
        }

        #region Properties

        /// <summary>
        /// The unique (internal) identifier for the argument
        /// </summary>
        public string Id { get { return this.id; } }

        /// <summary>
        /// Any prefixes supported for the argument. This should include all of the characters that
        /// are not to be treated as part of the value e.g. /key=
        /// </summary>
        public string[] Prefixes { get { return this.prefixes; } }

        /// <summary>
        /// Whether the argument is mandatory or not
        /// </summary>
        public bool Required { get { return this.required; } }

        /// <summary>
        /// A short description of the argument that will be displayed to the user
        /// e.g. /key= [SonarQube project key]
        /// </summary>
        public string Description { get { return this.description; } }

        /// <summary>
        /// True if the argument can be specified multiple times,
        /// false if it can be specified at most once
        /// </summary>
        public bool AllowMultiple { get { return this.allowMultiple; } }

        /// <summary>
        /// False if the argument has a value that follows the prefix,
        /// true if the argument is just single word (e.g. "begin")
        /// </summary>
        public bool IsVerb { get { return this.isVerb; } }

        #endregion
    }
}