using System.ComponentModel.DataAnnotations.Schema;

namespace UACloudLibrary
{
    public class AddressSpaceModel : AddressSpace 
    {
        [ForeignKey("Category")]
        [Column("categoryid")]
        public int CategoryId { get; set; }
        [ForeignKey("Contributor")]
        [Column("contributorid")]
        public int ContributorId { get; set; }
        [Column("nodesetid")]
        public long NodesetId { get; set; }
    }
}
