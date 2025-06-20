using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAI.Models.Entity
{
    public class DTranscribedOutput
    {
        public int TranscribedOutputId { get; set; }
        public int SourceDocumentId { get; set; }
        public string TranscribedContent { get; set; }
        public string TranscribedFilePath { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public string ProcessedBy { get; set; }
        public DateTime ProcessedDttm { get; set; }

        public DSourceDocument SourceDocument { get; set; }
    }
}
