using Microsoft.AspNetCore.Mvc;
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

        [HttpPost]
        public async Task<IActionResult> FetchDataAsync1([FromBody] string startTime)
        {
            Console.WriteLine("fetch data called");
            try
            {
                // Convert the start time to Unix timestamp
                DateTime parsedStartTime = DateTime.Parse(startTime);
                //DateTime parsedStartTime = new DateTime(2024, 5, 2, 15, 44, 22, 216);
                long unixStartTime = (long)(parsedStartTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;


                var subreddit = "movies";
                var apiUrl = $"https://oauth.reddit.com/r/{subreddit}/new?after={unixStartTime}";

                using (var client = _httpClientFactory.CreateClient())
                {
                    // Add the necessary headers for authorization and user agent
                    client.DefaultRequestHeaders.Add("Authorization", $"bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IlNIQTI1NjpzS3dsMnlsV0VtMjVmcXhwTU40cWY4MXE2OWFFdWFyMnpLMUdhVGxjdWNZIiwidHlwIjoiSldUIn0.eyJzdWIiOiJ1c2VyIiwiZXhwIjoxNzE0NzU1MzY0LjEzODQ2MywiaWF0IjoxNzE0NjY4OTY0LjEzODQ2MywianRpIjoiVWxadFBaT05OczNGU2ZDVnpGbU5kTHM5Tm9nWE9nIiwiY2lkIjoiY3UtbmpTZlF5NGRTUzFhSzRlcWFEUSIsImxpZCI6InQyX2d3dmxjeGpkOSIsImFpZCI6InQyX2d3dmxjeGpkOSIsImxjYSI6MTY5MTE4MjcyOTEyNywic2NwIjoiZUp5S1Z0SlNpZ1VFQUFEX193TnpBU2MiLCJmbG8iOjl9.JIDNLomUTH5NFrkLZ8AWjLfDk78MSiTbxB6irxZ5SJDwx9ht_yWSZvP6w7Wr6qT_Vk0OyfAtprUuUeZdhoKW2Wv_4VtlF4GEk1LXx7BR9TLpNJ-oP2GQvfvwbOZdrDoqE7rC4elCohjeikSKj0sPBZW624MQ57a3Ib5u4LF6S0_uIDTv5jRHV2lRG9lvRu9oYRQCoOGOolRDB94711e7COk5DBL2q2UnsGSAnbRSP2-mTUTOFe2qKxVA0UDScbiXZc-rDOmncEr89KCPw7bdcRvGda3kLBJ7eJ1VxSxYYSk5GRo7yt1_khGBqaUtaD28klLY6C_m7nk5vX2eDMd5tw");
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

                        // Extract the required data
                        var mostUpvotedPost = ((IEnumerable<dynamic>)jsonData.data.children)
                            .OrderByDescending(p => p.data.ups)
                            .FirstOrDefault();

                        var mostUpvotedPostUrl = mostUpvotedPost?.data.url.ToString();
                        var mostUpvotedPostUpvotes = mostUpvotedPost?.data.ups ?? 0;

                        // Explicitly cast jsonData.data.children to IEnumerable<dynamic>
                        var userPostsCounts = ((IEnumerable<dynamic>)jsonData.data.children)
                            .GroupBy(p => p.data.author.ToString())
                            .Select(g => new
                            {
                                Username = g.Key,
                                PostCount = g.Count()
                            })
                            .OrderByDescending(g => g.PostCount)
                            .FirstOrDefault();

                        var mostActiveUser = userPostsCounts?.Username;
                        var mostActiveUserPostCount = userPostsCounts?.PostCount ?? 0;

                        // Return the extracted data
                        return Ok(new
                        {
                            MostUpvotedPostUrl = mostUpvotedPostUrl ?? "No post after the start time",
                            MostUpvotedPostUpvotes = mostUpvotedPostUpvotes,
                            MostActiveUser = mostActiveUser ?? "No users",
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
