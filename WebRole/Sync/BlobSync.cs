// Copyright © Microsoft Corporation. All Rights Reserved. 
// This code released under the terms of the
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.) 

using System;
using System.ComponentModel;
using Microsoft.WindowsAzure.StorageClient;

namespace smarx.BlobSync
{
    public class UpdatingFileEventArgs : CancelEventArgs
    {
        public CloudBlob Blob;
        public string LocalPath;
        public UpdatingFileEventArgs(CloudBlob blob, string localPath)
        {
            Blob = blob;
            LocalPath = localPath;
        }
    }

    public delegate void UpdatingFileHandler(object sender, UpdatingFileEventArgs args);

    public delegate void SyncStartedHandler(object sender);
    public delegate void SyncCompletedHandler(object sender);
}