public class LinkRequest
{
    public long Id { get; set; }
    public string Url {get;set;}=string.Empty;
}

public class DeleteLinkRequest
{
    public string Url {get;set;}=string.Empty;
}