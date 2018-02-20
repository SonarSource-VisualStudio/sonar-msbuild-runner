﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SonarQube.TeamBuild.Integration {
    using System;


    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {

        private static global::System.Resources.ResourceManager resourceMan;

        private static global::System.Globalization.CultureInfo resourceCulture;

        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }

        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SonarQube.TeamBuild.Integration.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }

        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to The Coverage Report Processor was not initialised before use..
        /// </summary>
        internal static string EX_CoverageReportProcessorNotInitialised {
            get {
                return ResourceManager.GetString("EX_CoverageReportProcessorNotInitialised", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Fetching code coverage report information from TFS....
        /// </summary>
        internal static string PROC_DIAG_FetchingCoverageReportInfoFromServer {
            get {
                return ResourceManager.GetString("PROC_DIAG_FetchingCoverageReportInfoFromServer", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Updating project info files with code coverage information....
        /// </summary>
        internal static string PROC_DIAG_UpdatingProjectInfoFiles {
            get {
                return ResourceManager.GetString("PROC_DIAG_UpdatingProjectInfoFiles", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Absolute path to coverage file: {0}.
        /// </summary>
        internal static string TRX_DIAG_AbsoluteTrxPath {
            get {
                return ResourceManager.GetString("TRX_DIAG_AbsoluteTrxPath", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Looking for TRX files in: {0}.
        /// </summary>
        internal static string TRX_DIAG_FolderPaths {
            get {
                return ResourceManager.GetString("TRX_DIAG_FolderPaths", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Attempting to locate a test results (.trx) file....
        /// </summary>
        internal static string TRX_DIAG_LocatingTrx {
            get {
                return ResourceManager.GetString("TRX_DIAG_LocatingTrx", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to No code coverage attachments were found in the trx file.
        /// </summary>
        internal static string TRX_DIAG_NoCodeCoverageInfo {
            get {
                return ResourceManager.GetString("TRX_DIAG_NoCodeCoverageInfo", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to No test results files found.
        /// </summary>
        internal static string TRX_DIAG_NoTestResultsFound {
            get {
                return ResourceManager.GetString("TRX_DIAG_NoTestResultsFound", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to One code coverage attachment was found in the trx file: {0}.
        /// </summary>
        internal static string TRX_DIAG_SingleCodeCoverageAttachmentFound {
            get {
                return ResourceManager.GetString("TRX_DIAG_SingleCodeCoverageAttachmentFound", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Located a test results file: {0}.
        /// </summary>
        internal static string TRX_DIAG_SingleTrxFileFound {
            get {
                return ResourceManager.GetString("TRX_DIAG_SingleTrxFileFound", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Test results directory does not exist in {0}.
        /// </summary>
        internal static string TRX_DIAG_TestResultsDirectoryNotFound {
            get {
                return ResourceManager.GetString("TRX_DIAG_TestResultsDirectoryNotFound", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Coverage attachment could not be found. Trx file: {0}. Attachment name: {1}.
        /// </summary>
        internal static string TRX_WARN_InvalidConstructedCoveragePath {
            get {
                return ResourceManager.GetString("TRX_WARN_InvalidConstructedCoveragePath", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Located trx file is not a valid xml file. File: {0}. File load error: {1}.
        /// </summary>
        internal static string TRX_WARN_InvalidTrx {
            get {
                return ResourceManager.GetString("TRX_WARN_InvalidTrx", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Expecting to find at most one code coverage attachment in the trx file, but multiple attachments were found. Code coverage information will not be uploaded to SonarQube. Attachments: {0}.
        /// </summary>
        internal static string TRX_WARN_MultipleCodeCoverageAttachmentsFound {
            get {
                return ResourceManager.GetString("TRX_WARN_MultipleCodeCoverageAttachmentsFound", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to More than one test result file was found: expecting to find only one. Results files: {0}.
        /// </summary>
        internal static string TRX_WARN_MultipleTrxFilesFound {
            get {
                return ResourceManager.GetString("TRX_WARN_MultipleTrxFilesFound", resourceCulture);
            }
        }
    }
}
