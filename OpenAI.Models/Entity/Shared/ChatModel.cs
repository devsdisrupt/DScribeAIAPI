using Utility;

namespace OpenAI.Models
{
    public class ChatModel : CredentialModel
    {        
        public string Query { get; set; }
        public List<string> QueryHistory { get; set; }
        public string ResultContent { get; set; }
        //Added by AUH for VRS on 26-March-2024
        public string AssistantID { get; set; }
        public string AssistantName { get; set; }
        public string ThreadID { get; set; }
    }

    public class ChatResponseModel
    {
        public string Query { get; set; }
        public List<string> QueryHistory { get; set; }
        public string ResultContent { get; set; }

        //Added by AUH for VRS on 26-March-2024
        public string AssistantID { get; set; }
        public string AssistantName { get; set; }
        public string ThreadID { get; set; }
    }
    
}
