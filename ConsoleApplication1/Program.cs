using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace ConsoleApplication1
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("YouTube Data API: Search and add to playlist");
            Console.WriteLine("========================");

            try
            {
                new SearchAddToPlaylist().Run().Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
        internal class SearchAddToPlaylist
        {
            public async Task Run()
            {
                var youtubeSearchService = new YouTubeService(new BaseClientService.Initializer()
                {
                    ApiKey = "AIzaSyAqPhD-ei7VyFyHo7LZIpH6JKb3Sq7Dvp0",
                    ApplicationName = this.GetType().ToString()
                });

                var searchListRequest = youtubeSearchService.Search.List("snippet");
                Console.WriteLine("Da cuvantul de cautare: ");
                string keyword = Console.ReadLine();
                searchListRequest.Q = keyword;
                searchListRequest.MaxResults = 10;

                // Call the search.list method to retrieve results matching the specified query term.
                var searchListResponse = await searchListRequest.ExecuteAsync();

                List<string> videos = new List<string>();
                List<string> videoIds = new List<string>();

                // Add each result to the appropriate list, and then display the lists of
                // matching videos, channels, and playlists.
                foreach (var searchResult in searchListResponse.Items)
                
                    if (searchResult.Id.Kind == "youtube#video")
                    {                       
                            videos.Add(String.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.VideoId));
                            videoIds.Add(searchResult.Id.VideoId);                                                   
                    }

                UserCredential credential;
                using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
                {
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        // This OAuth 2.0 access scope allows for full read/write access to the
                        // authenticated user's account.
                        new[] { YouTubeService.Scope.Youtube },
                        "user",
                        CancellationToken.None,
                        new FileDataStore(this.GetType().ToString())
                    );
                }

                var youtubePlaylistService = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = this.GetType().ToString()
                });
                var newPlaylist = new Playlist();
                newPlaylist.Snippet = new PlaylistSnippet();
                Console.WriteLine("Da numele Playlist-ului: ");
                string playlistName = Console.ReadLine();
                newPlaylist.Snippet.Title = playlistName;
                newPlaylist.Snippet.Description = "A playlist created with the YouTube API v3";
                newPlaylist.Status = new PlaylistStatus();
                newPlaylist.Status.PrivacyStatus = "public";
                newPlaylist = await youtubePlaylistService.Playlists.Insert(newPlaylist, "snippet,status").ExecuteAsync();

                var newPlaylistItem = new PlaylistItem();
                newPlaylistItem.Snippet = new PlaylistItemSnippet();
                newPlaylistItem.Snippet.PlaylistId = newPlaylist.Id;
                newPlaylistItem.Snippet.ResourceId = new ResourceId();
                newPlaylistItem.Snippet.ResourceId.Kind = "youtube#video";
                foreach (String video in videoIds)
                {
                   // Console.WriteLine("Adding " + video + " to playlist");
                    newPlaylistItem.Snippet.ResourceId.VideoId = video;
                    //Console.WriteLine("Inserting " + video + " to playlist");
                    newPlaylistItem = await youtubePlaylistService.PlaylistItems.Insert(newPlaylistItem, "snippet").ExecuteAsync();

                    Console.WriteLine("Playlist item id {0} was added to playlist id {1}.", newPlaylistItem.Id, newPlaylist.Id);
                }
            }
        }
    }
}
