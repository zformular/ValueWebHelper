using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ValueWebHelper.ValueUpload.Infrastructure
{
    public class SaveInfo
    {
        public String Type { get; set; }

        public String FilePath { get; set; }

        public String[] FileNames { get; set; }
    }
}
