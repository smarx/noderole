using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using smarx.BlobSync;
using System.Threading;
using Microsoft.Web.Administration;
using System.IO;
using System.Diagnostics;

namespace WebRole
{
    public class WebRole : RoleEntryPoint
    {
        OneWayGitSync gitSync = null;
        OneWayBlobSync blobSync = null;
        object lockObject = new Object();

        public override void Run()
        {
            while (true)
            {
                int interval = int.Parse(RoleEnvironment.GetConfigurationSettingValue("PollingIntervalInSeconds"));
                if (interval > 0)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(interval));
                    Sync();
                }
                else
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }
        }

        private void Sync()
        {
            lock (lockObject)
            {
                if (gitSync != null) gitSync.SyncAll();
                if (blobSync != null) blobSync.SyncAll();
            }
        }

        private void Configure()
        {
            lock (lockObject)
            {
                var appPath = RoleEnvironment.GetLocalResource("App").RootPath;

                foreach (var entry in new DirectoryInfo(appPath).GetFileSystemInfos())
                {
                    DeleteRecursive(entry);
                }

                var gitUrl = RoleEnvironment.GetConfigurationSettingValue("GitUrl");
                if (!string.IsNullOrEmpty(gitUrl))
                {
                    gitSync = new OneWayGitSync(gitUrl, appPath);
                    gitSync.SyncCompleted += new SyncCompletedHandler(syncCompleted);
                    gitSync.SyncAll();
                }
                else
                {
                    gitSync = null;
                }

                var containerName = RoleEnvironment.GetConfigurationSettingValue("ContainerName");
                if (!string.IsNullOrEmpty(containerName))
                {
                    var container = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("DataConnectionString")).CreateCloudBlobClient().GetContainerReference(containerName);
                    container.CreateIfNotExist();
                    blobSync = new OneWayBlobSync(container, appPath);
                    blobSync.SyncCompleted += new SyncCompletedHandler(syncCompleted);
                }
                else
                {
                    blobSync = null;
                }
            }
        }

        void syncCompleted(object sender)
        {
            var appPath = RoleEnvironment.GetLocalResource("app").RootPath;
            var proc = new Process()
            {
                StartInfo = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"nodejs\npm.cmd"), "install")
                {
                    WorkingDirectory = appPath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            proc.WaitForExit((int)TimeSpan.FromMinutes(2).TotalMilliseconds);
 
            try
            {
                File.Copy(Environment.ExpandEnvironmentVariables(@"%RoleRoot%\approot\bin\node.Web.config"), Path.Combine(appPath, @"Web.config"));
            }
            catch (IOException) { } // ignore if file already exists
        }

        void DeleteRecursive(FileSystemInfo fsi)
        {
            fsi.Attributes = FileAttributes.Normal;
            var di = fsi as DirectoryInfo;

            if (di != null)
            {
                foreach (var dirInfo in di.GetFileSystemInfos())
                {
                    DeleteRecursive(dirInfo);
                }
            }
            fsi.Delete();
        }

        public override bool OnStart()
        {
            var appPath = RoleEnvironment.GetLocalResource("App").RootPath;

            var sm = new ServerManager();
            sm.Sites[RoleEnvironment.CurrentRoleInstance.Id + "_Web"].Applications.First().VirtualDirectories.First().PhysicalPath = appPath;
            // Note that this can sometimes throw exceptions under the compute emulator when using multiple instances,
            // because they're trying to edit applicationHost.config simultaneously. Best to stick to a single instance.
            sm.CommitChanges();

            Configure();
            Sync();

            RoleEnvironment.Changing += (_, e) =>
            {
                // any config setting changes that aren't just the polling interval (which requires nothing, since the loop will just pick up the new value next time through)
                if (e.Changes.OfType<RoleEnvironmentConfigurationSettingChange>().Any(c => c.ConfigurationSettingName != "PollingIntervalInSeconds"))
                {
                    Configure();
                }
            };

            return base.OnStart();
        }
    }
}
