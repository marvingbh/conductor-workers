using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workers.Dtos
{


    public class UploadContentDto
    {
        public List<Folder> files { get; set; }
    }


    public class InputContents
    {
        public Folder[] contents { get; set; }
    }
    public class Folder
    {
        public string id { get; set; }
        public string name { get; set; }
        public List<Folder> SubFolders { get; set; } = new List<Folder>() { };
        public List<Content> Contents { get; set; } = new List<Content>() { };
        public bool ShouldCreateFolder { get; set; }
        public string Status { get; set; }

        public void SetShouldCreateFolder(bool value)
        {
            ShouldCreateFolder = value;
        }
    }

    public class Content
    {
        public string md5;
        public long size;
        public string id { get; set; }
        public string title { get; set; }
        public string subType { get; set; }
        public string? Status { get; set; }
    }


    public class FileContent
    {
        public File file { get; set; }
        public object dateRawFileReplaced { get; set; }
        public DateTime dateRawFileUploaded { get; set; }
        public bool hasPii { get; set; }
        public bool isLatestVersion { get; set; }
        public int requestedSignaturesCount { get; set; }
        public object nextDueSignatureDate { get; set; }
        public int pendingTasksCount { get; set; }
        public int timelinesCount { get; set; }
        public string rawFilename { get; set; }
        public string binderId { get; set; }
        public int pageCount { get; set; }
        public string conversionStatus { get; set; }
        public string importedVia { get; set; }
        public string formStatus { get; set; }
        public string ext { get; set; }
        public string name { get; set; }
        public string id { get; set; }
        public bool isExpired { get; set; }
        public bool isAlmostExpired { get; set; }
        public bool hasPendingSignatureDue { get; set; }
        public bool hasPendingSignatureAlmostDue { get; set; }
        public bool isFullySigned { get; set; }
        public int version { get; set; }
        public bool isDownloadable { get; set; }
        public Timestamps timestamps { get; set; }
        public int shortcutsCount { get; set; }
        public string parentId { get; set; }
    }

    public class File
    {
        public string fileId { get; set; }
        public long size { get; set; }
        public string md5 { get; set; }
    }

    public class Timestamps
    {
        public DateTime createdAt { get; set; }
        public string createdBy { get; set; }
        public DateTime updatedAt { get; set; }
        public string updatedBy { get; set; }
    }

}
