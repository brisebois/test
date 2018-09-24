using System.Runtime.Serialization;

namespace Hacker1ProductsFuncApp
{
    [DataContract]
    public class ProductDetail
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
    }
}