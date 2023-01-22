using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;
using Youtube_to_Mp3_convertor.Helper;

namespace Youtube_to_mp3_convertor.Controllers
{
    [ApiController]
    [Route("api/youtube")]
    public class YoutubeController : ControllerBase
    {
        private readonly YoutubeHelper _youtubeHelper;
        private readonly ILogger<YoutubeController> _logger;
        private readonly IDistributedCache _distributedCache;
        public YoutubeController(YoutubeHelper youtubeHelper, ILogger<YoutubeController> logger, IDistributedCache distributedCache)
        {
            _youtubeHelper = youtubeHelper;
            _logger = logger;
            _distributedCache = distributedCache;
        }

        [HttpPost("{link}")]
        public async Task<IActionResult> DownloadVideo(string link)
        {
            // Get the client's IP address
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            if (CheckRateLimit(clientIp))
            {
                return StatusCode(429); // Too Many Requests
            }
            try
            {
                link = Uri.UnescapeDataString(link);

                if (!_youtubeHelper.IsValidLink(link))
                {
                    _logger.LogInformation("Log - DownloadVideo request - Invalid link:{0}", link);
                    return BadRequest("Not a valid YouTube link");
                }

                var video = await _youtubeHelper.GetVideoAsync(link);
                var streamInfo = await _youtubeHelper.GetAudioStreamAsync(link);                

                if (streamInfo == null)
                {
                    _logger.LogInformation("Log - DownloadVideo request - No audio stream available in the video", link);
                    return BadRequest("No audio stream available in the video.");
                }
                var title = _youtubeHelper.RemoveInvalidFileNameChars(video.Title);

                await _youtubeHelper.DownloadAudioAsync(streamInfo, title);

                var fileBytes = System.IO.File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "audio", $"{title}.{streamInfo.Container}"));
                var result = File(fileBytes, $"audio/{streamInfo.Container}", title);

                try
                {
                    // Using Path.GetFullPath method to get the absolute path
                    var filepath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "audio", $"{title}.{streamInfo.Container}"));
                    System.IO.File.Delete(filepath);
                }
                catch (IOException)
                {
                    _logger.LogError("Log - DownloadVideo request - File delete failed:", Path.Combine(Directory.GetCurrentDirectory(), "audio", $"{title}.{streamInfo.Container}"));
                    // Return a specific error message to the user
                    return StatusCode(500, "Error deleting the file, please try again later");
                }

                return result;
            }
            catch (YoutubeExplode.Exceptions.VideoUnavailableException)
            {
                return BadRequest("Video unavailable");
            }
            catch (YoutubeExplode.Exceptions.VideoRequiresPurchaseException)
            {
                return BadRequest("Video Requires Purchase");
            }
            catch (YoutubeExplode.Exceptions.VideoUnplayableException)
            {
                return BadRequest("Video Unplayable");
            }
            catch (YoutubeExplode.Exceptions.RequestLimitExceededException)
            {
                return StatusCode(429, "Too many requests");
            }
            catch (YoutubeExplode.Exceptions.YoutubeExplodeException)
            {
                return BadRequest("YoutubeExplodeException");
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error");
            }
        }
        private bool CheckRateLimit(string clientIp)
        {
            var cacheKey = $"rate-limit:{clientIp}";
            var currentTime = DateTimeOffset.UtcNow;

            var timestampBytes = _distributedCache.Get(cacheKey);
            if (timestampBytes != null)
            {
                var timestamp = DateTimeOffset.Parse(Encoding.UTF8.GetString(timestampBytes));
                if (currentTime - timestamp < TimeSpan.FromSeconds(30))
                {
                    return true;
                }
                _distributedCache.Remove(cacheKey);
            }
            var currentTimeBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(currentTime));

            _distributedCache.Set(cacheKey, currentTimeBytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = currentTime.AddSeconds(30)
            });
            return false;
        }


    }
}
