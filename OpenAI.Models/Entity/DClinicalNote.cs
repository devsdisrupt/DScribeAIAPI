using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAI.Models.Entity
{
    public class DClinicalNote
    {
        public int ClinicalNoteId { get; set; }
        public int TranscribedOutputId { get; set; }
        public int SourceDocumentId { get; set; }
        public string ClinicalNoteContent { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public string ProcessedBy { get; set; }
        public DateTime ProcessedDttm { get; set; }

        public DTranscribedOutput TranscribedOutput { get; set; }
    }
}
