using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleDriveFileDownload
{
    public class DriveFile
    {
        public Google.Apis.Drive.v3.Data.File? File { get; set; }
        public string? Path { get; set; }
    }
}
