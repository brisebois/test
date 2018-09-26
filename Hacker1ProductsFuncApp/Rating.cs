using System.Runtime.Serialization;

namespace Hacker1ProductsFuncApp
{
    [DataContract]
    public class Rating
    {
        [DataMember]
        public double? sentimentScore { get; set; }

        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string userId { get; set; }
        [DataMember]
        public string productId { get; set; }
        [DataMember]
        public string timestamp { get; set; }
        [DataMember]
        public string locationName { get; set; }
        [DataMember]
        public int rating { get; set; }
        [DataMember]
        public string userNotes { get; set; }
    }
}