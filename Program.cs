using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace imageScrape {
    class Program {
        const string API_KEY = "<Insert your BING API KEY HERE>";
        const string URL = "https://api.cognitive.microsoft.com/bing/v7.0/images/search";
        static async Task Main (string[] args) {
            string query = args[0]; //Search query which will be used to scrape bing.
            string directory = args[1]; //Directory images will be saved in. i.e /SomeLocation
            string numberOfItems = args[2];

            using (HttpClient client = new HttpClient ()) {
                try {
                    string currentDirectory = Directory.GetCurrentDirectory ();

                    string target = currentDirectory + directory;
                    if (!Directory.Exists (target)) {
                        Console.WriteLine ("Setting up directory {0}", target);
                        Directory.CreateDirectory (target);
                    }

                    Console.WriteLine ("Setting up http client");

                    var builder = new UriBuilder (URL);
                    builder.Port = -1;

                    var uriQuery = HttpUtility.ParseQueryString (builder.Query);
                    uriQuery["q"] = query;
                    uriQuery["offset"] = "0";
                    uriQuery["count"] = numberOfItems;
                    uriQuery["size"] = "medium";

                    builder.Query = uriQuery.ToString ();
                    string url = builder.ToString ();

                    client.DefaultRequestHeaders.Accept.Clear ();
                    client.DefaultRequestHeaders.Accept.Add (new MediaTypeWithQualityHeaderValue ("application/json"));
                    client.DefaultRequestHeaders.Add ("Ocp-Apim-Subscription-Key", API_KEY);

                    Console.WriteLine ("Start the scrap for {0}!", query);

                    var response = await client.GetAsync (url);
                    response.EnsureSuccessStatusCode ();

                    var stringResult = await response.Content.ReadAsStringAsync ();
                    BingImageResult results = JsonConvert.DeserializeObject<BingImageResult> (stringResult);
                    List<Value> values = results.value;

                    int total = 0;
                    foreach (var item in values) {
                        if (item.contentUrl != String.Empty) {
                            Console.WriteLine ("fetching {0}", item.contentUrl);
                            var content = await client.GetAsync (item.contentUrl);
                            if (content.IsSuccessStatusCode) {
                                string targetFilePath = target + string.Format ("/{0}-", total) +
                                    item.contentUrl.Substring (item.contentUrl.LastIndexOf ('/') + 1);
                                var arrBytes = await content.Content.ReadAsByteArrayAsync ();
                                File.WriteAllBytes (targetFilePath, arrBytes);

                            }
                        };
                        total++;
                    }

                } catch (HttpRequestException httpRequestException) {
                    Console.WriteLine ($"Error getting images from Bing : {httpRequestException.Message}");
                } catch (System.Exception) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine ("There has been an error in console app");
                    throw;
                }
            }

        }

    }
}