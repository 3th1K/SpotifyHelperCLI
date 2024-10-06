namespace SpotifyHelper.Models;

public class UserData
{
    public string display_name { get; set; }
    public ExternalUrls external_urls { get; set; }
    public string href { get; set; }
    public string id { get; set; }
    public List<Image> images { get; set; }
    public string type { get; set; }
    public string uri { get; set; }
    public Followers followers { get; set; }
    public string country { get; set; }
    public string product { get; set; }
    public ExplicitContent explicit_content { get; set; }
    public string email { get; set; }
}