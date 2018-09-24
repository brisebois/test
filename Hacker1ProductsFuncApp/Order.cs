using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Hacker1ProductsFuncApp
{
    [DataContract]
    public class Order
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "datetime")]
        public DateTime Datetime { get; set; }
        [DataMember(Name = "location")]
        public Location Location { get; set; } = new Location();
        [DataMember(Name = "totalTax")]
        public double TotalTax { get; set; }
        [DataMember(Name = "totalCost")]
        public double TotalCost { get; set; }
        [DataMember(Name = "lineItems")]
        public List<LineItem> LineItems { get; set; } = new List<LineItem>();
    }
}