
namespace OpenAI.DBManager
{
    internal sealed class APIManagerDirectory
    {
        private const string OPENAI_TURBO_MANAGER_CLASS = "OpenAITurboManager";

        public static string GetAIAssistance { get { return OPENAI_TURBO_MANAGER_CLASS; } }
        public static string GetAIRefinedText { get { return OPENAI_TURBO_MANAGER_CLASS; } }
        public static string UploadPDFtoGCS { get { return OPENAI_TURBO_MANAGER_CLASS; } }
        public static string TestingAIAPI { get { return OPENAI_TURBO_MANAGER_CLASS; } }
        public static string PerformOCR { get { return OPENAI_TURBO_MANAGER_CLASS; } }
        public static string PerformLLMProcessing { get { return OPENAI_TURBO_MANAGER_CLASS; } }
        


        private const string ENCRYPTED_PASSWORD_MANAGER_CLASS = "EncryptedPasswordManager";

        public static string GetEncryptedPassword { get { return ENCRYPTED_PASSWORD_MANAGER_CLASS; } }

        private const string TOKEN_MANAGER_CLASS = "TokenManager";

        public static string GetAIToken { get { return TOKEN_MANAGER_CLASS; } }
    }
}
