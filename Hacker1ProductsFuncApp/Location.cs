using System.Runtime.Serialization;

namespace Hacker1ProductsFuncApp
{
    [DataContract]
    public class Location
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "address")]
        public string Address { get; set; }
        [DataMember(Name = "postcode")]
        public string PostCode { get; set; }
    }
}