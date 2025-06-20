using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAI.Models.Entity
{
    public class DSourceDocument
    {
        public string ResponseResult { get; set; }
        public int SourceDocumentId { get; set; }
        public string SiteId { get; set; }
        public string InputBucketName { get; set; }
        public string OutputBucketName { get; set; }
        public string SourceFileName { get; set; }
        public string SourceFilePath { get; set; }
        public long SourceFileSize { get; set; }
        public string SourceFileType { get; set; }
        public string ProcessTypeId { get; set; }  // e.g. "SINGLE"
        public string ProcessedBy { get; set; }
        public DateTime ProcessedDttm { get; set; }
        public string PDFBase64 { get; set; }
    }
}
