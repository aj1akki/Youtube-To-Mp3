using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Web;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace Youtube_to_mp3_convertor.Controllers
{
    [ApiController]
    [Route("api/youtube")]
    public class YoutubeController : ControllerBase
    {
        [HttpGet("{link}")]
        public async Task<IActionResult> DownloadVideo(string link)
        {
            try
            {
                if (link == null)
                {
                    return BadRequest("Link undefined");
                }

                if (!link.Contains("youtube") && !link.Contains("youtu.be"))
                {
                    return BadRequest("Not a YouTube link");
                }

                link = HttpUtility.UrlDecode(link);

                var youtube = new YoutubeClient();

                // You can specify either video ID or URL
                var video = await youtube.Videos.GetAsync(link);

                var title = video.Title;

                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(link);

                // ...or highest bitrate audio-only stream
                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                // Generate a unique file name
                var videoId = new Uri(link).Segments.Last();
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string filename = $"{timestamp}_{videoId}.{streamInfo.Container}";

                // Download the stream to a file
                await youtube.Videos.Streams.DownloadAsync(streamInfo, filename);

                // Return the audio file as a response to the client
                var fileBytes = System.IO.File.ReadAllBytes(filename);
                var result = File(fileBytes, $"audio/{streamInfo.Container}", filename);
                // Delete the file
                System.IO.File.Delete(filename);

                return result;
            }
            catch (YoutubeExplode.Exceptions.VideoUnavailableException)
            {
                return BadRequest("Video unavailable");
            }
            catch (YoutubeExplode.Exceptions.VideoRequiresPurchaseException)
            {
                return NotFound("Video requires purchase");
            }
            catch (YoutubeExplode.Exceptions.RequestLimitExceededException)
            {
                return StatusCode(429, "Too many requests");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }
    }

}