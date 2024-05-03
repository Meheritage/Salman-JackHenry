using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    //[Route("api/[controller]")]
    //[ApiController]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult FetchDataAsync()
        {
            Console.WriteLine("fetch data called");
            return Ok();
        }

        [HttpPost]
        public ActionResult UpdateOrder()
        {
            // some code
            Console.WriteLine("fetch data called");
            return Json(new { success = true, message = "Order updated successfully" });
        }

        public class TimeModel
        {
            public string Time { get; set; }
        }
        [HttpPost]
        public async Task<IActionResult> FetchDataAsync1([FromBody] TimeModel Time)
        {
            Console.WriteLine("fetch data called", Time.Time);
            try
            {
                // Convert the start time to Unix timestamp
                DateTime parsedStartTime = DateTime.Parse(Time.Time);
                long unixStartTime = (long)(parsedStartTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))).TotalSeconds;

                //long unixStartTime = 1714731200;


                var subreddit = "movies";
                var apiUrl = $"https://oauth.reddit.com/r/{subreddit}/new?limit=100";
                var accessToken = "eyJhbGciOiJSUzI1NiIsImtpZCI6IlNIQTI1NjpzS3dsMnlsV0VtMjVmcXhwTU40cWY4MXE2OWFFdWFyMnpLMUdhVGxjdWNZIiwidHlwIjoiSldUIn0.eyJzdWIiOiJ1c2VyIiwiZXhwIjoxNzE0ODM1Mzc2Ljc4MDA2LCJpYXQiOjE3MTQ3NDg5NzYuNzgwMDYsImp0aSI6ImJRUXc5RGdwUmt6alNoRGg2ME1GWUpzVUJXS0FmZyIsImNpZCI6ImN1LW5qU2ZReTRkU1MxYUs0ZXFhRFEiLCJsaWQiOiJ0Ml9nd3ZsY3hqZDkiLCJhaWQiOiJ0Ml9nd3ZsY3hqZDkiLCJsY2EiOjE2OTExODI3MjkxMjcsInNjcCI6ImVKeUtWdEpTaWdVRUFBRF9fd056QVNjIiwiZmxvIjo5fQ.BpgMwkRbhsKnWZWSf7cy4zcnxiGozOmCogboGm8a98fASx0RIDic-NG0jfkMeRoeG18J50NnkMMaGzM3o3l6tx0n9RZvinppTppmrIUATV1ioPxUaxK1tvC7d6h-Br8jaJQuT0AvjhPTycKZ16IdQgpnbhXJodVFpGfI9YF4Yj3OUly25rF8xWGVUdT87qktkdQ6W-AC8OB_QO2o2HHnhQu2BUXQJw76oVuXA3jbZUuwoKX7QECpiTulJWyi7RnQa2P70m4zHI8KntTbiBHyS6ZjbcQpu9oKu3kTfALMYS5RrOgm_3Vj7DNYSugTpkn_U_pFzMlf9XIJAPfN15FQbw";
                using (var client = _httpClientFactory.CreateClient())
                {
                    // Add the necessary headers for authorization and user agent
                    client.DefaultRequestHeaders.Add("Authorization", $"bearer {accessToken}");
                    client.DefaultRequestHeaders.Add("User-Agent", "ChangeMeClient/0.1 by YourUsername");

                    // Make the GET request to the Reddit API
                    HttpResponseMessage response = await client.GetAsync(apiUrl);

                    // Check if the request was successful (status code 200)
                    if (response.IsSuccessStatusCode)
                    {
                        // Read the response content as a string
                        string redditData = await response.Content.ReadAsStringAsync();

                        // Deserialize the JSON response into a dynamic object
                        dynamic jsonData = JsonConvert.DeserializeObject(redditData);

                        List<dynamic> filteredPosts = new List<dynamic>();

                        // Iterate through the posts and filter based on creation time
                        foreach (var post in jsonData.data.children)
                        {
                            // Convert the creation time of the post to Unix timestamp
                            long postUnixTime = (long)post.data.created_utc;

                            // Check if the post was created after the specified start time
                            if (postUnixTime > unixStartTime)
                            {
                                // Add the post to the filtered list
                                filteredPosts.Add(post);
                            }
                        }

                        if (filteredPosts.Count == 0)
                        {
                            return Ok(new
                            {
                                MostUpvotedPostUrl = "No post after the start time",
                                MostUpvotedPostUpvotes = 0,
                                MostActiveUser = "No users posted",
                                MostActiveUserPostCount = 0
                            });
                        }


                        // Extract the most upvoted post
                        var domain = "https://www.reddit.com";
                        var mostUpvotedPost = filteredPosts
                            .OrderByDescending(p => p.data.ups)
                            .FirstOrDefault();

                        var permalink = mostUpvotedPost?.data.permalink.ToString();
                        var mostUpvotedPostUrl = domain + permalink;
                        var mostUpvotedPostUpvotes = (int)mostUpvotedPost?.data.ups;

                        //Extract the user with most post
                        var userPostsCounts = filteredPosts
    .Where(p => p.data.author != null)
    .GroupBy(p => p.data.author.ToString())
    .Select(g => new
    {
        Username = g.Key,
        PostCount = g.Count()
    })
    .OrderByDescending(g => g.PostCount)
    .FirstOrDefault();



                        var mostActiveUser = userPostsCounts?.Username;
                        var mostActiveUserPostCount = userPostsCounts?.PostCount;

                        //var mostActiveUser = "Apple";
                        //var mostActiveUserPostCount =2;

                        // Return the extracted data
                        return Ok(new
                        {
                            MostUpvotedPostUrl = mostUpvotedPostUrl,
                            MostUpvotedPostUpvotes = mostUpvotedPostUpvotes,
                            MostActiveUser = mostActiveUser,
                            MostActiveUserPostCount = mostActiveUserPostCount
                        });
                    }
                    else
                    {
                        // If the request fails, return an error message
                        return StatusCode((int)response.StatusCode, $"Failed to retrieve data. Status code: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, return an error message
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        public IActionResult End()
        {
            return Json(new { message = "End action called" });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
