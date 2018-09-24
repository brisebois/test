using System.Runtime.Serialization;

namespace Hacker1ProductsFuncApp
{
    [DataContract]
    public class LineItem
    {
        [DataMember(Name = "product")]
        public ProductDetail Product { get; set; }
        [DataMember(Name = "quantity")]
        public int Quantity { get; set; }
        [DataMember(Name = "unitCost")]
        public double UnitCost { get; set; }
        [DataMember(Name = "totalCost")]
        public double TotalCost { get; set; }
        [DataMember(Name = "totalTax")]
        public double TotalTax { get; set; }
    }
}