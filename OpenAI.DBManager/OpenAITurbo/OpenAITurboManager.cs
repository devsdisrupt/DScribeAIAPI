using APIUtility.Constants;
using APIUtility.Models;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Google.Cloud.Vision.V1;
using Google.Protobuf;
using Grpc.Auth;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OpenAI.Models;
using OpenAI.Models.Entity;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAi_Assistant.Models;
using OpenAi_Assistant.Services;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Security.Policy;
using System.Text;
using Utility;
using static OpenAI_API.Audio.TextToSpeechRequest;

namespace OpenAI.DBManager
{
    public class OpenAITurboManager
    {
        public object UploadPDFtoGCS(RequestModel request)
        {
            SuccessResponse successResponseModel = new SuccessResponse();

            try
            {
                DSourceDocument sourceDocument = JsonConvert.DeserializeObject<DSourceDocument>(Convert.ToString(request.RequestData));

                //GoogleCredential credential;

                //string _googleCredentialsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GoogleServiceAccount.json");

                //using (var stream = new FileStream(_googleCredentialsPath, FileMode.Open, FileAccess.Read))
                //{
                //    credential = GoogleCredential.FromStream(stream);
                //}

                GoogleCredential credential = GoogleCredential.GetApplicationDefaultAsync().Result;

                StorageClient _storage = StorageClient.Create(credential);

                string uniqueId = Guid.NewGuid().ToString();

                var objectName = $"{sourceDocument.SiteId}/{uniqueId}.pdf";

                byte[] pdfBytes = Convert.FromBase64String(sourceDocument.PDFBase64);

                using (var ms = new MemoryStream(pdfBytes))
                {
                    var uploadResult = _storage
                        .UploadObjectAsync(sourceDocument.InputBucketName, objectName, "application/pdf", ms)
                        .Result;

                    if (uploadResult != null && !string.IsNullOrEmpty(uploadResult.Name))
                    {
                        sourceDocument.ProcessedDttm = DateTime.Now;
                        sourceDocument.SourceFileSize = pdfBytes.Length;
                        sourceDocument.ProcessTypeId = "Upload";
                        sourceDocument.SourceFilePath = $"gs://{sourceDocument.InputBucketName}/{objectName}";
                        sourceDocument.ResponseResult = $"gs://{sourceDocument.InputBucketName}/{objectName}";
                        successResponseModel = new SuccessResponse(sourceDocument, true, "Upload successful. GCS Object: {uploadResult.Name}");
                    }
                    else
                    {
                        //return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
                    }
                }


            }
            catch (Exception ex)
            {
                Logger.WriteInfoLog("GetAIAssistance() - Error: " + ex);
                Logger.WriteErrorLog(ex);
                return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
            }
            return successResponseModel;
        }

        public object TestingAIAPI(RequestModel request)
        {
            SuccessResponse successResponseModel = new SuccessResponse();

            try
            {
                DSourceDocument sourceDocument = JsonConvert.DeserializeObject<DSourceDocument>(Convert.ToString(request.RequestData));

                if(sourceDocument.InputBucketName == "SuccessTest")
                {
                    successResponseModel = new SuccessResponse("Success Response Test", true,"Success");
                }
                else
                {
                    successResponseModel = new SuccessResponse("Failure Response Test",false,"Failure");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteInfoLog("GetAIAssistance() - Error: " + ex);
                Logger.WriteErrorLog(ex);
                return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
            }
            return successResponseModel;
        }

        public object PerformOCR(RequestModel request)
        {
            //Input:
            //"SiteId": "LNH",
            //"OutputBucketName": "my-output-bucket-ocr",
            //"SourceFilePath": "gs://my-input-bucket-ocr/LNH/848e0610-26e8-4bc9-8a93-191dc976c622.pdf"

            SuccessResponse successResponseModel = new SuccessResponse();

            try
            {
                // Deserialize the input request
                DSourceDocument sourceDocument = JsonConvert.DeserializeObject<DSourceDocument>(Convert.ToString(request.RequestData));

                // Extract Source File Info
                string sourceFilePath = sourceDocument.SourceFilePath;  // Example: gs://my-input-bucket-ocr/LNH/xxx.pdf

                // Parse bucket name and object name from SourceFilePath
                Uri sourceUri = new Uri(sourceFilePath);
                string inputBucket = sourceUri.Host;
                string objectName = sourceUri.AbsolutePath.TrimStart('/');

                // Prepare target info
                string outputBucket = sourceDocument.OutputBucketName;
                string siteId = sourceDocument.SiteId;
                string todayDate = DateTime.UtcNow.ToString("ddMMyyyy");  // Example: 18062025

                // Generate output folder prefix: SiteId/OCRFiles/Today'sDate/
                string outputPrefix = $"{siteId}/OCRFiles/{todayDate}/";

                // File Name for .txt
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(objectName);
                string outputFileName = $"{outputPrefix}{fileNameWithoutExtension}.txt";

                //GoogleCredential credential;

                //string _googleCredentialsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dscribe-462811-3feadef0aeed.json");

                //using (var stream = new FileStream(_googleCredentialsPath, FileMode.Open, FileAccess.Read))
                //{
                //    credential = GoogleCredential.FromStream(stream).CreateScoped(new[]
                //    {
                //    "https://www.googleapis.com/auth/cloud-platform"
                //    });
                //}

                GoogleCredential credential = GoogleCredential
                        .GetApplicationDefaultAsync()
                        .Result;

                credential = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");

                // Create clients
                // Create Storage client (you already had this)
                StorageClient storage = StorageClient.Create(credential);

                //string credsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dscribe-462811-3feadef0aeed.json");

                var builder = new ImageAnnotatorClientBuilder
                {
                    // supply the JSON directly:
                    //JsonCredentials = File.ReadAllText(credsPath)
                    Credential = credential
                };

                ImageAnnotatorClient visionClient = builder.Build();

                // Create async batch OCR request
                var asyncRequest = new AsyncBatchAnnotateFilesRequest
                {
                    Requests =
                    {
                        new AsyncAnnotateFileRequest
                        {
                            InputConfig = new InputConfig
                            {
                                GcsSource = new GcsSource { Uri = $"gs://{inputBucket}/{objectName}" },
                                MimeType = "application/pdf"
                            },
                            Features = { new Feature { Type = Feature.Types.Type.DocumentTextDetection } },
                            OutputConfig = new OutputConfig
                            {
                                GcsDestination = new GcsDestination { Uri = $"gs://{outputBucket}/{outputPrefix}" },
                                BatchSize = 1
                            }
                        }
                    }
                };

                // Run the OCR operation
                var op = visionClient.AsyncBatchAnnotateFiles(asyncRequest);
                var completed = op.PollUntilCompleted();

                if (!completed.IsCompleted)
                    throw new Exception("OCR operation failed.");

                // Read back the output
                var results = new List<AnnotateFileResponse>();

                // List objects in output folder
                var objects = storage.ListObjects(outputBucket, outputPrefix).OrderBy(o => o.Name);

                StringBuilder finalTextContent = new StringBuilder();

                // Prepare a parser that ignores unknown fields:
                // before the loop, configure parser once:
                var parser = new JsonParser(JsonParser.Settings.Default
                    .WithIgnoreUnknownFields(true)
                );

                foreach (var obj in storage.ListObjects(outputBucket, outputPrefix)
                                          .OrderBy(o => o.Name))
                {
                    // skip everything except .json
                    if (!obj.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // download
                    using var ms = new MemoryStream();
                    storage.DownloadObject(obj, ms);
                    string json = Encoding.UTF8.GetString(ms.ToArray());

                    // trim any leading BOM or whitespace
                    json = json.TrimStart('\uFEFF', '\r', '\n', ' ', '\t');

                    // now safe to parse
                    AnnotateFileResponse resp = parser.Parse<AnnotateFileResponse>(json);

                    // extract text...
                    foreach (var page in resp.Responses)
                        if (page.FullTextAnnotation != null)
                            finalTextContent.AppendLine(page.FullTextAnnotation.Text);
                }

                // Now save the final text as a .txt file in output bucket
                using var textStream = new MemoryStream(Encoding.UTF8.GetBytes(finalTextContent.ToString()));
                storage.UploadObject(outputBucket, outputFileName, "text/plain", textStream);

                sourceDocument.ResponseResult = $"gs://{outputBucket}/{outputFileName}";

                //sourceDocument.OutputFilePath = $"gs://{outputBucket}/{outputFileName}";
                //successResponseModel.IsSuccess = true;

                successResponseModel = new SuccessResponse(sourceDocument, true, "OCR completed successfully.");

                return successResponseModel;
            }
            catch (Exception ex)
            {
                Logger.WriteInfoLog("GetAIAssistance() - Error: " + ex);
                Logger.WriteErrorLog(ex);
                return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
            }
            return successResponseModel;
        }

        public object PerformLLMProcessing(RequestModel request)
        {
            // Input JSON in request.RequestData, e.g.:
            // {
            //   "SiteId": "LNH",
            //   "SourceFilePath": "gs://my-output-bucket-ocr/LNH/OCRFiles/18062025/848e0610-26e8-4bc9-8a93-191dc976c622.txt",
            //   "OpenAIAPIKey": "sk-…",
            //   "GPTModel": "gpt-4-1106-preview"
            //   "PromptType": "Beautify"
            // }

            SuccessResponse successResponseModel = new SuccessResponse();

            try
            {
                // 1) Deserialize inputs
                dynamic doc = JsonConvert.DeserializeObject(Convert.ToString(request.RequestData));
                string siteId = doc.SiteId;
                string sourceFilePath = doc.SourceFilePath;
                string openAiKey = doc.OpenAIAPIKey;
                string gptModel = doc.GPTModel;
                string promptType = doc.PromptType;

                // 2) Parse GCS bucket + object
                var uri = new Uri(sourceFilePath);
                string bucket = uri.Host;
                string objectName = uri.AbsolutePath.TrimStart('/');

                // 3) Download the raw‑OCR text
                //GoogleCredential cred;
                //using (var stream = new FileStream(
                //           Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                //                        "dscribe-462811-3feadef0aeed.json"),
                //           FileMode.Open))
                //{
                //    cred = GoogleCredential.FromStream(stream)
                //            .CreateScoped("https://www.googleapis.com/auth/cloud-platform");
                //}

                GoogleCredential cred = GoogleCredential
                                        .GetApplicationDefaultAsync()
                                        .Result;

                cred = cred.CreateScoped("https://www.googleapis.com/auth/cloud-platform");

                var storage = StorageClient.Create(cred);

                string rawText;
                using (var ms = new MemoryStream())
                {
                    storage.DownloadObject(bucket, objectName, ms);
                    rawText = Encoding.UTF8.GetString(ms.ToArray());
                }
                string prompt = "";
                string folder = "";

                // 4) Call OpenAI
                if (!string.IsNullOrEmpty(promptType))
                {
                    if (promptType == "Beautify")
                    {
                        folder = "BeautifiedOutput";
                        #region Step-2(A) prompt
                            prompt = $@"
                                You are a medical AI assistant. 
                                The following text was extracted by OCR from scanned medical documents.
                                Refine and clean it to be more readable and meaningful, without altering any medical content.
                                Preserve page numbers and any medication lists, diagnoses, or instructions.
                                Do not add or assume information—just clarify the existing content.

                                Raw OCR text:
                                {rawText}
                                ";
                            #endregion
                    }
                    else if (promptType == "DateWiseClassification")
                    { 
                        folder = "DateWiseClassification";
                        #region Step-2(B) prompt
                            prompt = $@"
                                You are a medical AI assistant specialized in clinical summarization. 
                                You have already been given a cleaned OCR transcription of a patient’s multi‑page medical record.
                                Your task is to:

                                1. Identify each discrete clinical entry by its date (use any date formats present).
                                2. Group all findings, diagnoses, medications, and notes under those dates.
                                3. Produce a chronological summary list, where each date is a header followed by its associated clinical data.
                                4. Omit any content that cannot be associated with a specific date.

                                Return the result as:

                                Date: YYYY‑MM‑DD  
                                • Item 1  
                                • Item 2  
                                …

                                Cleaned Text:  
                                { rawText}
                                ";
                            #endregion
                    }
                    else if (promptType == "ClinicalDocumentGeneration")
                    {
                        folder = "ClinicalDocument";
                        #region Step-2(C) prompt

                        prompt = $@"
                            You are a medical AI assistant tasked with composing the final, polished clinical document.
                            You have already been given a cleaned OCR transcription of a patient’s multi‑page medical record. Your job is to:

                            1. Create a coherent narrative document that reads like a professional medical note.
                            2. Include standard sections: Patient Identifiers (if any), Clinical Summary, Medications, Diagnoses, Plan/Instructions.
                            3. Integrate dates naturally (e.g., “On 2025‑06‑18, the patient presented with…”).
                            4. Keep formatting clean and hierarchical (use headings and bullet points where appropriate).
                            5. Do not invent new information—only use what’s in the summary.

                            Cleaned Text:  
                            { rawText}
                            ";
                        #endregion
                    }

                    using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
                    {
                        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAiKey}");

                        var payload = new
                        {
                            model = gptModel,
                            messages = new[] { new { role = "user", content = prompt } },
                            temperature = 0.4
                        };
                        var body = new StringContent(
                            JsonConvert.SerializeObject(payload),
                            Encoding.UTF8, "application/json"
                        );

                        var resp = http.PostAsync(
                            "https://api.openai.com/v1/chat/completions", body
                        ).Result;

                        resp.EnsureSuccessStatusCode();

                        dynamic json = JsonConvert.DeserializeObject(
                            resp.Content.ReadAsStringAsync().Result
                        );

                        string beautified = (string)json.choices[0].message.content;

                        // 5) (Optional) write back to GCS
                        string today = DateTime.UtcNow.ToString("ddMMyyyy");
                        string prefix = $"{siteId}/{folder}/{today}/";
                        string outName = Path.GetFileNameWithoutExtension(objectName)
                                             + ".txt";

                        using (var outMs = new MemoryStream(
                                   Encoding.UTF8.GetBytes(beautified)))
                        {
                            storage.UploadObject(
                                bucket, prefix + outName,
                                "text/plain", outMs
                            );
                        }

                        // 6) return success with GCS path
                        doc.OutputFilePath = $"gs://{bucket}/{prefix}{outName}";
                        doc.ResponseResult = $"gs://{bucket}/{prefix}{outName}";
                        successResponseModel = new SuccessResponse(doc, true, "LLM Processing has been completed.");
                        return successResponseModel;
                    }
                }
                else
                {
                    return new SuccessResponse("Prompt Type was not provided",false, "Prompt Type was not provided");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
            }
        }

        //public object GetAIAssistance(RequestModel request)
        //{
        //    SuccessResponse successResponseModel = new SuccessResponse();
        //    try
        //    {
        //        ChatModel chatModel = JsonConvert.DeserializeObject<ChatModel>(Convert.ToString(request.RequestData));

        //        string outputResult = "";
        //        var openai = new OpenAIAPI(ApplicationSettings.Instance.AppLocalSetting.OpenAIKey);
        //        Logger.WriteInfoLog("API Key:"  + ApplicationSettings.Instance.AppLocalSetting.OpenAIKey);

        //        ChatRequest chatRequest = new ChatRequest();

        //        if (chatModel.QueryHistory == null)
        //            chatModel.QueryHistory = new List<string>();

        //        chatModel.QueryHistory.Insert(chatModel.QueryHistory.Count, chatModel.Query);

        //        var allMessages = chatModel.QueryHistory.Select(g => new ChatMessage
        //        {
        //            Content = g,
        //            Role = ChatMessageRole.User
        //        }).ToList();

        //        chatRequest.Messages = allMessages;
        //        chatRequest.Model = OpenAI_API.Models.Model.GPT4_Turbo;
        //        chatRequest.MaxTokens = 1000;
        //        chatRequest.Temperature = 0.5; //0.7

        //        var completions = openai.Chat.CreateChatCompletionAsync(chatRequest).Result;

        //        foreach (var completion in completions.Choices)
        //        {
        //            outputResult += completion.Message.Content;
        //        }
        //        chatModel.ResultContent = outputResult; ;

        //        Logger.WriteInfoLog("Query:" + chatModel.Query);
        //        Logger.WriteInfoLog("QueryHistory:" + chatModel.QueryHistory);
        //        Logger.WriteInfoLog("ResultContent:" + chatModel.ResultContent);

        //        ChatResponseModel chatResponseModel = new ChatResponseModel()
        //        {
        //            Query = chatModel.Query,
        //            QueryHistory = chatModel.QueryHistory,
        //            ResultContent = chatModel.ResultContent
        //        };

        //        successResponseModel = new SuccessResponse(chatResponseModel, true);
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.WriteInfoLog("GetAIAssistance() - Error: " + ex);
        //        Logger.WriteErrorLog(ex);
        //        return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
        //    }
        //    return successResponseModel;
        //}

        //public object GetAIVoiceResponse(RequestModel request)
        //{
        //    SuccessResponse successResponseModel = new SuccessResponse();
        //    try
        //    {
        //        ChatModel chatModel = JsonConvert.DeserializeObject<ChatModel>(Convert.ToString(request.RequestData));

        //        string outputResult = "";
        //        var openai = new OpenAIAPI(ApplicationSettings.Instance.AppLocalSetting.OpenAIKey);

        //        // Call getVoice method to get the audio stream
        //        Task<Stream> audioStream = getVoice(openai, chatModel.Query);
        //        Stream audioStreamNew = audioStream.Result;

        //        // Convert the audio stream to a byte array
        //        byte[] audioBytes;
        //        using (MemoryStream ms = new MemoryStream())
        //        {
        //            audioStreamNew.CopyTo(ms);
        //            audioBytes = ms.ToArray();
        //        }

        //        // Convert the byte array to base64 string to include it in the response
        //        string base64Audio = Convert.ToBase64String(audioBytes);

        //        ChatResponseModel chatResponseModel = new ChatResponseModel()
        //        {
        //            Query = chatModel.Query,
        //            ResultContent = base64Audio // Pass the base64 encoded audio to your Web App
        //        };

        //        successResponseModel = new SuccessResponse(chatResponseModel, true);
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.WriteErrorLog(ex);
        //        return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
        //    }
        //    return successResponseModel;
        //}

        //public async Task<Stream> getVoice(OpenAIAPI openai, string message)
        //{
        //    message = "This is a message in JSON format, I want you to simplify this in human readable form and just tell the physician as an assistant" +
        //        "what are the recent things ordered on this patient: " + message;
        //    // Return the audio stream
        //    return await openai.TextToSpeech.GetSpeechAsStreamAsync(message, Voices.Fable);
        //}

        ////Added by AUH for VRS on 26-March-2024
        //public object GetAIRefinedText(RequestModel request)
        //{
        //    SuccessResponse successResponseModel = new SuccessResponse();
        //    try
        //    {
        //    ChatModel chatModel = JsonConvert.DeserializeObject<ChatModel>(Convert.ToString(request.RequestData));


        //    ChatModel chat = new ChatModel();
        //    //if (ApplicationSettings.Instance.AppLocalSetting.OpenAIAssistants.ContainsKey("VRS_Message"))
        //    //{
        //    //    chat.Query = ApplicationSettings.Instance.AppLocalSetting.OpenAIAssistants["VRS_Message"] + chatModel.Query;
        //    //}


        //    chat.Query = "You are AKU-Assistant.Designed for Aga Khan University Hospital.Your job is to response within scope of AKUH only.You offer services like doctor days, times, open slots inquiry, doctor appointment booking." + "Doctor Detail: Dr. Azaz Ul Haq, Speciality: Dermatologist" + "specialize in diagnosing and treating skin conditions, including acne, eczema, psoriasis, skin cancer, rashes, and other skin issues" + "" + "Qualifications: MBBS, Fellowship in pediatric dermatology. Does clinic on every Monday Tuesday Wedensday 03 to 04 PM. NExt available slot is on 28 March, 2025" + "This is your query:"  + "" + chatModel.Query;

        //     RequestModel req = new RequestModel("", JsonConvert.SerializeObject(chat), "", "");

        //    object abc = GetAIAssistance(req);

        //    SuccessResponse successResponse = (SuccessResponse)abc;

        //    ChatResponseModel chatResponse = (ChatResponseModel)successResponse.Data;

        //            if (!string.IsNullOrEmpty(chatResponse.ResultContent.ToString()))
        //            {
        //                ChatResponseModel chatResponseModel = new ChatResponseModel()
        //                {
        //                    Query = chatModel.Query,
        //                    QueryHistory = chatModel.QueryHistory,
        //                    ResultContent = chatResponse.ResultContent.ToString(),
        //                };
        //                successResponseModel = new SuccessResponse(chatResponseModel, true);
        //            }
        //            else
        //            {
        //                ChatResponseModel chatResponseModel = new ChatResponseModel()
        //                {
        //                    Query = chatModel.Query
        //                };
        //                successResponseModel = new SuccessResponse(chatResponseModel, true);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.WriteErrorLog(ex);
        //            return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
        //        }

        //        return successResponseModel;
        //}
    }

 
}
