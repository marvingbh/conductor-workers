using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workers.Dtos
{


    public class UploadContentDto
    {
        public Folders[] files { get; set; }
    }


    public class InputContents
    {
        public Folders[] contents { get; set; }
    }
    public class Folders
    {
        public string id { get; set; }
        public string name { get; set; }
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
