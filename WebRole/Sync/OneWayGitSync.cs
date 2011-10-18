// Copyright © Microsoft Corporation. All Rights Reserved. 
// This code released under the terms of the
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.) 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.IO;
using System.Diagnostics;

namespace smarx.BlobSync
{
    public class OneWayGitSync : IOneWaySync
    {
        private string gitUrl;
        private string localPath;
        public string LocalPath { get { return localPath; } }

        public event SyncCompletedHandler SyncCompleted;

        private bool firstTime = true;
        public void SyncAll()
        {
            string output = null;
            if (firstTime)
            {
                output = Git(localPath, "clone {0} .", gitUrl);
                if (Directory.Exists(Path.Combine(localPath, ".git")))
                {
                    firstTime = false;
                }
            }
            else
            {
                output = Git(localPath, "pull");
            }
            if (!output.Contains("Already up-to-date.") && SyncCompleted != null)
            {
                SyncCompleted(this);
            }
        }

        public static string Git(string workingDirectory, string formatString, params object[] p)
        {
            var gitExecutable = Path.Combine(RoleEnvironment.GetLocalResource("Git").RootPath, @"bin\git.exe");
            var startInfo = new ProcessStartInfo(gitExecutable, string.Format(formatString, p))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            startInfo.EnvironmentVariables["LOGONSERVER"] = @"\\" + Environment.MachineName;
            startInfo.EnvironmentVariables["PATH"] += Path.GetDirectoryName(gitExecutable);
            var proc = new Process { StartInfo = startInfo };

            var sb = new StringBuilder();
            DataReceivedEventHandler recv = (_, e) =>
            {
                lock (sb)
                {
                    sb.AppendLine(e.Data);
                }
            };
            proc.OutputDataReceived += recv;
            proc.ErrorDataReceived += recv;
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            if (!proc.WaitForExit(120000))
            {
                try { proc.Kill(); } // try to clean up a hung process
                catch { }
            }

            return sb.ToString();
        }

        public OneWayGitSync(string gitUrl, string localPath)
        {
            this.gitUrl = gitUrl;
            this.localPath = localPath;
        }
    }
}