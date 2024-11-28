using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SpotifyHelper.Models;
using static Program;

public class Program
{
    private readonly HttpClient _httpClient = new();
    private const string CLIENT_ID = "xxx";
    private const string CLIENT_SECRET = "xxx";
    private const string REDIRECT_URI = "http://localhost:8080/callback";

    public async Task<string> Login()
    {
        string state = "1234567890987654";
        string scope = "user-read-private " +
                       "user-read-email " +
                       "ugc-image-upload " +
                       "playlist-read-private " +
                       "playlist-read-collaborative " +
                       "playlist-modify-private " +
                       "playlist-modify-public " +
                       "user-library-read " +
                       "user-library-modify ";
        string response_type = "code";
        var client_id = CLIENT_ID;
        var redirect_uri = REDIRECT_URI;

        string authUrl = "https://accounts.spotify.com/authorize";
        string queryString = $"{authUrl}?" +
                             $"response_type={response_type}" +
                             $"&client_id={client_id}" +
                             $"&scope={WebUtility.UrlEncode(scope)}" +
                             $"&redirect_uri={WebUtility.UrlEncode(REDIRECT_URI)}" +
                             $"&state={state}" +
                             $"&show_dialog=true";

        var httpListener = new HttpListener();
        httpListener.Prefixes.Add(REDIRECT_URI + '/');
        httpListener.Start();

        Process.Start(new ProcessStartInfo(queryString) { UseShellExecute = true });

        Console.WriteLine("Waiting for the callback...");

        HttpListenerContext context = await httpListener.GetContextAsync();
        HttpListenerRequest request = context.Request;

        string authorizationCode = request.QueryString["code"];
        httpListener.Stop();

        if (authorizationCode == null)
        {
            Console.WriteLine("Access Refused");
            return null;
        }
        return authorizationCode;
    }

    public async Task<string> GetToken(string authCode)
    {
        //string url = "https://accounts.spotify.com/api/token";
        var authOptions = new
        {
            url = "https://accounts.spotify.com/api/token",
            form = new
            {
                code = authCode,
                redirect_uri = REDIRECT_URI,
                grant_type = "authorization_code"
            },
            headers = new
            {
                content_type = "application/x-www-form-urlencoded",
                Authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{CLIENT_ID}:{CLIENT_SECRET}"))
            },
            json = true
        };

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("code", authOptions.form.code),
            new KeyValuePair<string, string>("redirect_uri", authOptions.form.redirect_uri),
            new KeyValuePair<string, string>("grant_type", authOptions.form.grant_type)
        });
        _httpClient.DefaultRequestHeaders.Add("Authorization", authOptions.headers.Authorization);

        var response = await _httpClient.PostAsync(authOptions.url, content);
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> GetCurrentUserProfile(string token)
    {
        var url = "https://api.spotify.com/v1/me";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _httpClient.GetAsync(url);
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> AddSongToPlaylist(string playlistId, string songId, string token)
    {
        var url = $"https://api.spotify.com/v1/playlists/{playlistId}/tracks";
        var json = '{' + "\"uris\":[" + $"\"spotify:track:{songId}\"]" + '}';
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            var response = await _httpClient.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStringAsync();
            return null;
        }
        catch (Exception e)
        {
            return null;
        }
    }

    public async Task<List<Track>> GetPlaylistTracks(string playlistId)
    {
        var sourcePlaylistUrl = $"https://api.spotify.com/v1/playlists/{playlistId}/tracks";
        List<Track> tracks = new List<Track>();
        var playlistData = new PlaylistData();
        playlistData.next = sourcePlaylistUrl;

        while (playlistData.next is not null)
        {
            var response = await _httpClient.GetAsync(playlistData.next);
            var responseData = await response.Content.ReadAsStringAsync();
            playlistData.next = null;
            playlistData = JsonSerializer.Deserialize<PlaylistData>(responseData);
            var playlistItems = playlistData.items;
            foreach (var item in playlistItems)
            {
                var track = item.track;
                tracks.Add(track);
            }
        }

        return tracks;
    }

    public async Task CopyPlaylist(string sourcePlaylistId, string destinationPlaylistId, string token)
    {
        var sourcePlaylistUrl = $"https://api.spotify.com/v1/playlists/{sourcePlaylistId}/tracks";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var destinationTracks = await GetPlaylistTracks(destinationPlaylistId);
        List<Track> tracks = new List<Track>();
        var playlistData = new PlaylistData();
        playlistData.next = sourcePlaylistUrl;

        while (playlistData.next is not null)
        {
            var response = await _httpClient.GetAsync(playlistData.next);
            var responseData = await response.Content.ReadAsStringAsync();
            playlistData.next = null;
            playlistData = JsonSerializer.Deserialize<PlaylistData>(responseData);
            var playlistItems = playlistData.items;
            foreach (var item in playlistItems)
            {
                var track = item.track;
                tracks.Add(track);
            }
        }
        Console.WriteLine("Found " + tracks.Count + " Tracks in the Playlist");
        int success = 0;
        int failure = 0;
        int skipped = 0;
        foreach (var track in tracks)
        {
            if (destinationTracks.FirstOrDefault(t => t.id == track.id) is not null)
            {
                Console.WriteLine($"Skipping track [ {track.name} ] => Result : Skipped");
                skipped++;
            }
            else
            {
                Console.Write($"Copying track [ {track.name} ] => Result : ");
                try
                {
                    var result = await AddSongToPlaylist(destinationPlaylistId, track.id, token);
                    if (result is not null)
                    {
                        Console.WriteLine("Success");
                        success++;
                    }
                    else
                    {
                        Console.WriteLine("Failure");
                        failure++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failure");
                    failure++;
                }
                Console.WriteLine($"Copied {success} tracks, Failed : {failure}, Skipped : {skipped}");
            }
        }
    }

    public async Task RemoveDuplicatesFromPlaylist(string playlistId)
    {
        //var tracks =
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine(DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
        var app = new Program();
        var authCode = await app.Login();
        if (authCode == null)
        {
            Console.WriteLine("Exiting");
            return;
        }
        var token = await app.GetToken(authCode);
        if (token == null)
        {
            Console.WriteLine("Error while fetching token");
            return;
        }

        TokenData tokenData = JsonSerializer.Deserialize<TokenData>(token) ?? throw new InvalidOperationException();

        var response = await app.GetCurrentUserProfile(tokenData.access_token);
        var user = JsonSerializer.Deserialize<UserData>(response);

        Console.WriteLine($"Current user : {user.display_name}");
        //var addresponse = await app.AddSongToPlaylist("xxx", "xxx", tokenData.access_token);
        await app.CopyPlaylist("xxx", "xxx", tokenData.access_token);
    }
}
