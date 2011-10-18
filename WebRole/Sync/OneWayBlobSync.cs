// Copyright © Microsoft Corporation. All Rights Reserved. 
// This code released under the terms of the
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.) 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.WindowsAzure.StorageClient;

namespace smarx.BlobSync
{
    public class OneWayBlobSync : IOneWaySync
    {
        private CloudBlobContainer container;
        private string localPath;
        public string LocalPath { get { return localPath; } }
        private TimeSpan interval;
        private Thread syncingThread;
        private Dictionary<string, string> localBlobs = new Dictionary<string, string>();
        
        public event UpdatingFileHandler UpdatingFile;
        public event SyncStartedHandler SyncStarted;
        public event SyncCompletedHandler SyncCompleted;

        public OneWayBlobSync(CloudBlobContainer container, string localPath, TimeSpan interval)
        {
            this.container = container;
            this.localPath = localPath;
            this.interval = interval;
        }
        public OneWayBlobSync(CloudBlobContainer container, string localPath) : this(container, localPath, TimeSpan.FromSeconds(5)) { }

        private string GetLocalPath(string uri)
        {
            return Path.Combine(localPath, uri.Substring(container.Uri.AbsoluteUri.Length + 1).Replace('/', '\\'));
        }

        public void SyncAll()
        {
            bool started = false;
            var cloudBlobs = container.ListBlobs(new BlobRequestOptions() { UseFlatBlobListing = true, BlobListingDetails = BlobListingDetails.Metadata }).OfType<CloudBlob>();
            var cloudBlobNames = new HashSet<string>(cloudBlobs.Select(b => b.Uri.ToString()));
            var localBlobNames = new HashSet<string>(localBlobs.Keys);
            localBlobNames.ExceptWith(cloudBlobNames);
            foreach (var name in localBlobNames)
            {
                started = true;
                if (!started && SyncStarted != null)
                {
                    SyncStarted(this);
                }
                File.Delete(GetLocalPath(name));
                localBlobs.Remove(name);
            }
            foreach (var blob in cloudBlobs)
            {
                if (!localBlobs.ContainsKey(blob.Uri.ToString()) ||
                    blob.Attributes.Properties.ETag != localBlobs[blob.Uri.ToString()])
                {
                    if (!started)
                    {
                        started = true;
                        if (SyncStarted != null)
                        {
                            SyncStarted(this);
                        }
                    }
                    var path = GetLocalPath(blob.Uri.ToString());
                    var args = new UpdatingFileEventArgs(blob, path);
                    if (UpdatingFile != null)
                    {
                        UpdatingFile(this, args);
                    }
                    if (!args.Cancel)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        using (var stream = File.Create(GetLocalPath(blob.Uri.ToString())))
                        {
                            blob.DownloadToStream(stream);
                        }
                    }
                    localBlobs[blob.Uri.ToString()] = blob.Properties.ETag;
                }
            }
            if (started && SyncCompleted != null)
            {
                SyncCompleted(this);
            }
        }

        public void SyncForever()
        {
            while (true)
            {
                SyncAll();
                Thread.Sleep(interval);
            }
        }

        public void Start()
        {
            syncingThread = new Thread(new ThreadStart(SyncForever));
            syncingThread.Start();
        }

        public void Stop()
        {
            syncingThread.Abort();
        }
    }
}