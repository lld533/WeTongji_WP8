using System;
using System.IO;
using System.Linq;

namespace WeTongji.Api.Util
{
    public class FileItem
    {
        private String _filename;
        private Stream _content;
        private String _contentType;

        public FileItem(String fileName, String contentType, Stream content)
        {
            FileName = fileName;
            Content = content;
            ContentType = contentType;
        }

        public String FileName
        {
            get { return _filename; }
            private set { _filename = value; }
        }

        public Stream Content
        {
            get { return _content; }
            private set { _content = value; }
        }

        public String ContentType 
        {
            get;
            private set;
        }
    }
}
