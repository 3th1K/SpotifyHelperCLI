namespace SpotifyHelper.Models;

public class Item
{
    public string added_at { get; set; }
    public AddedBy added_by { get; set; }
    public bool is_local { get; set; }
    public object primary_color { get; set; }
    public Track track { get; set; }
    public VideoThumbnail video_thumbnail { get; set; }
}