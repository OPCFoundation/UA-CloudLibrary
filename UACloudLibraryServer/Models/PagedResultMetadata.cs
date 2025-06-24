using System.Runtime.Serialization;

[DataContract]
public class PagedResultMetadata
{
    [DataMember(Name = "cursor")]
    public string Cursor { get; set; }
}
