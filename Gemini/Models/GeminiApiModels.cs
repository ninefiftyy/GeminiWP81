using System.Collections.Generic;
using System.Runtime.Serialization;

/// <summary>
/// Defines the data-transfer types used to serialize Gemini API requests and responses.
/// </summary>

namespace GeminiWP81
{
    [DataContract]
    public class GeminiApiRequest
    {
        public GeminiApiRequest()
        {
            Contents = new List<GeminiContent>();
        }

        [DataMember(Name = "contents")]
        public List<GeminiContent> Contents { get; set; }
    }

    [DataContract]
    public class GeminiApiResponse
    {
        [DataMember(Name = "candidates")]
        public List<GeminiCandidate> Candidates { get; set; }
    }

    [DataContract]
    public class GeminiCandidate
    {
        [DataMember(Name = "content")]
        public GeminiContent Content { get; set; }

        [DataMember(Name = "finishReason")]
        public string FinishReason { get; set; }
    }

    [DataContract]
    public class GeminiContent
    {
        [DataMember(Name = "role", EmitDefaultValue = false)]
        public string Role { get; set; }

        [DataMember(Name = "parts")]
        public List<GeminiPart> Parts { get; set; }
    }

    [DataContract]
    public class GeminiPart
    {
        [DataMember(Name = "text")]
        public string Text { get; set; }
    }
}
