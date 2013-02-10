using System;
using System.Collections.Generic;

namespace ValueWebHelper.ValueUpload.Infrastructure
{
    public class UploadInfo
    {
        public UploadInfo()
        {
            Form = new Dictionary<string, string>();
            Files = new List<FileInfo>();
        }

        public Dictionary<String, String> Form { get; set; }

        public IList<FileInfo> Files { get; set; }

        public Boolean Success { get; set; }

        public Exception Exception { get; set; }
    }
}
