using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.IO.IsolatedStorage;

/// <summary>
/// Sends requests to the Gemini API and manages model selection and request cancellation.
/// </summary>

namespace GeminiWP81
{
    public sealed class GeminiApiClient
    {
        public sealed class GeminiApiResult
        {
            public string Text { get; set; }
            public string FinishReason { get; set; }
        }

        private static readonly object RequestSync = new object();
        private static HttpWebRequest _activeRequest;
        private static int _requestGeneration;

        public static void CancelPendingRequest()
        {
            HttpWebRequest requestToCancel;
            lock (RequestSync)
            {
                _requestGeneration++;
                requestToCancel = _activeRequest;
                _activeRequest = null;
            }

            if (requestToCancel != null) requestToCancel.Abort();
        }

        public static int BeginRequest()
        {
            lock (RequestSync) return _requestGeneration;
        }

        public static void EnsureRequestCurrent(int generation)
        {
            lock (RequestSync)
            {
                if (generation != _requestGeneration) throw new OperationCanceledException();
            }
        }

        public static string GetSelectedModel()
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            string savedModel = settings.Contains("GeminiModel") ? settings["GeminiModel"].ToString() : null;
            if (string.IsNullOrEmpty(savedModel) || savedModel == "gemini-3.5-flash")
            {
                settings["GeminiModel"] = MainPage.DefaultGeminiModel;
                settings.Save();
                return MainPage.DefaultGeminiModel;
            }

            return savedModel;
        }

        public async Task<GeminiApiResult> GenerateContentAsync(List<ChatMessage> messages, string model, int generation)
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            string apiKey = settings.Contains("GeminiApiKey") ? settings["GeminiApiKey"].ToString() : string.Empty;
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException(Gemini.AppText.Get("Error") + Gemini.AppText.Get("MissingApiKey"));
            }

            string url = "https://generativelanguage.googleapis.com/v1beta/models/"
                + model + ":generateContent?key=" + apiKey;

            try
            {
                GeminiApiRequest payload = new GeminiApiRequest();
                foreach (ChatMessage message in messages)
                {
                    string role = message.Role == "model" ? "model" : "user";
                    if (payload.Contents.Count > 0 && payload.Contents[payload.Contents.Count - 1].Role == role)
                    {
                        payload.Contents[payload.Contents.Count - 1].Parts[0].Text += "\n\n" + (message.Text ?? string.Empty);
                    }
                    else
                    {
                        payload.Contents.Add(new GeminiContent
                        {
                            Role = role,
                            Parts = new List<GeminiPart> { new GeminiPart { Text = message.Text ?? string.Empty } }
                        });
                    }
                }

                DataContractJsonSerializer requestSerializer = new DataContractJsonSerializer(typeof(GeminiApiRequest));
                byte[] requestBytes;
                using (MemoryStream buffer = new MemoryStream())
                {
                    requestSerializer.WriteObject(buffer, payload);
                    requestBytes = buffer.ToArray();
                }

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json";
                RegisterActiveRequest(request, generation);

                try
                {
                    using (Stream stream = await Task.Factory.FromAsync<Stream>(
                        request.BeginGetRequestStream, request.EndGetRequestStream, null))
                    {
                        stream.Write(requestBytes, 0, requestBytes.Length);
                    }

                    using (WebResponse response = await Task.Factory.FromAsync<WebResponse>(
                        request.BeginGetResponse, request.EndGetResponse, null))
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string responseJson = reader.ReadToEnd();
                        EnsureRequestCurrent(generation);
                        return ParseResponse(responseJson);
                    }
                }
                finally
                {
                    ClearActiveRequest(request);
                }
            }
            catch (WebException ex)
            {
                EnsureRequestCurrent(generation);
                throw new InvalidOperationException(Gemini.AppText.Get("NetworkError") + ex.Message, ex);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                EnsureRequestCurrent(generation);
                throw new InvalidOperationException(Gemini.AppText.Get("InternalError") + ex.Message, ex);
            }
        }

        private static void RegisterActiveRequest(HttpWebRequest request, int generation)
        {
            lock (RequestSync)
            {
                if (generation != _requestGeneration) throw new OperationCanceledException();
                _activeRequest = request;
            }
        }

        private static void ClearActiveRequest(HttpWebRequest request)
        {
            lock (RequestSync)
            {
                if (ReferenceEquals(_activeRequest, request)) _activeRequest = null;
            }
        }

        private static GeminiApiResult ParseResponse(string json)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(GeminiApiResponse));
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                GeminiApiResponse response = serializer.ReadObject(stream) as GeminiApiResponse;
                if (response != null && response.Candidates != null && response.Candidates.Count > 0)
                {
                    GeminiCandidate candidate = response.Candidates[0];
                    if (candidate.Content != null && candidate.Content.Parts != null)
                    {
                        StringBuilder answer = new StringBuilder();
                        foreach (GeminiPart part in candidate.Content.Parts)
                        {
                            if (part != null && part.Text != null) answer.Append(part.Text);
                        }

                        if (answer.Length > 0)
                        {
                            return new GeminiApiResult
                            {
                                Text = answer.ToString(),
                                FinishReason = candidate.FinishReason
                            };
                        }
                    }
                }
            }

            throw new InvalidOperationException(Gemini.AppText.Get("Error") + Gemini.AppText.Get("NoResponse"));
        }
    }
}
