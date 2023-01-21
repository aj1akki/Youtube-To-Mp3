using Microsoft.AspNetCore.Mvc;
using System.Web;
using Youtube_to_Mp3_convertor.Helper;

namespace Youtube_to_mp3_convertor.Controllers
{
    [ApiController]
    [Route("api/youtube")]
    public class YoutubeController : ControllerBase
    {
        private readonly YoutubeHelper _youtubeHelper;

        public YoutubeController()
        {
            _youtubeHelper = new YoutubeHelper();
        }

        [HttpGet("{link}")]
        public async Task<IActionResult> DownloadVideo(string link)
        {
            try
            {
                if (!_youtubeHelper.IsValidLink(link))
                {
                    return BadRequest("Not a valid YouTube link");
                }

                link = HttpUtility.UrlDecode(link);

                var video = await _youtubeHelper.GetVideoAsync(link);
                var streamInfo = await _youtubeHelper.GetAudioStreamAsync(link);
                var title = _youtubeHelper.RemoveInvalidFileNameChars(video.Title);

                await _youtubeHelper.DownloadAudioAsync(streamInfo, title);

                var fileBytes = System.IO.File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "audio", $"{title}.{streamInfo.Container}"));
                var result = File(fileBytes, $"audio/{streamInfo.Container}", title);

                // Delete the file
                System.IO.File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "audio", $"{title}.{streamInfo.Container}"));

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
