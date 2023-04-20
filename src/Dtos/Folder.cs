using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workers.Dtos
{


    public class UploadContentDto
    {
        public Folder[] files { get; set; }
    }


    public class InputContents
    {
        public Folder[] contents { get; set; }
    }
    public class Folder
    {
        public string id { get; set; }
        public string name { get; set; }
        public List<Folder> SubFolders { get; set; }  = new List<Folder>();
        public List<Content> Contents { get; set; }
        public bool ShouldCreateFolder { get; set; }

        public void SetShouldCreateFolder(bool value)
        {
            ShouldCreateFolder = value;
        }
    }

    public class Content
    {
        public string id { get; set; }
        public string title { get; set; }
        public string subType { get; set; }
    }


}
