﻿namespace SpotifyHelper.Models;

public class Artist
{
    public ExternalUrls external_urls { get; set; }
    public string href { get; set; }
    public string id { get; set; }
    public string name { get; set; }
    public string type { get; set; }
    public string uri { get; set; }
}