using Microsoft.AspNetCore.Mvc;
using Youtube_to_Mp3_convertor.Helper;

namespace Youtube_to_mp3_convertor.Controllers
{
    [ApiController]
    [Route("api/youtube")]
    public class YoutubeController : ControllerBase
    {
        private readonly YoutubeHelper _youtubeHelper;
        private readonly ILogger<YoutubeController> _logger;

        public YoutubeController(YoutubeHelper youtubeHelper, ILogger<YoutubeController> logger)
        {
            _youtubeHelper = youtubeHelper;
            _logger = logger;
        }

        [HttpPost("{link}")]
        public async Task<IActionResult> DownloadVideo(string link)
        {
            try
            {
                _logger.LogInformation("Log - DownloadVideo request: {link}", link);

                link = Uri.UnescapeDataString(link);

                if (!_youtubeHelper.IsValidLink(link))
                {
                    _logger.LogInformation("Log - DownloadVideo request - Invalid link:{0}", link);
                    return BadRequest("Not a valid YouTube link");
                }

                var video = await _youtubeHelper.GetVideoAsync(link);
                var streamInfo = await _youtubeHelper.GetAudioStreamAsync(link);
                _logger.LogInformation("Log - DownloadVideo request: streamInfo:{0} video:{1}", streamInfo,video);

                if (streamInfo == null)
                {
                    return BadRequest("No audio stream available in the video.");
                }
                var title = _youtubeHelper.RemoveInvalidFileNameChars(video.Title);

                await _youtubeHelper.DownloadAudioAsync(streamInfo, title);

                var fileBytes = System.IO.File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "audio", $"{title}.{streamInfo.Container}"));
                var result = File(fileBytes, $"audio/{streamInfo.Container}", title);

                try
                {
                    System.IO.File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "audio", $"{title}.{streamInfo.Container}"));
                    _logger.LogInformation("Log - DownloadVideo request - File delete successfull");
                }
                catch (IOException ex)
                {
                    _logger.LogInformation("Log - DownloadVideo request - File delete failed");
                    // Return a specific error message to the user
                    return StatusCode(500, "Error deleting the file, please try again later");
                }


                return result;
            }
            catch (YoutubeExplode.Exceptions.VideoUnavailableException)
            {
                _logger.LogInformation("Log - DownloadVideo request - VideoUnavailableException");

                return BadRequest("Video unavailable");
            }
            catch (YoutubeExplode.Exceptions.VideoRequiresPurchaseException)
            {
                _logger.LogInformation("Log - DownloadVideo request - VideoRequiresPurchaseException");

                return BadRequest("Video Requires Purchase");
            }
            catch (YoutubeExplode.Exceptions.VideoUnplayableException)
            {
                _logger.LogInformation("Log - DownloadVideo request - VideoUnplayableException");

                return BadRequest("Video Unplayable");
            }
            catch (YoutubeExplode.Exceptions.RequestLimitExceededException)
            {
                _logger.LogInformation("Log - DownloadVideo request - RequestLimitExceededException");

                return StatusCode(429, "Too many requests");
            }
            catch (YoutubeExplode.Exceptions.YoutubeExplodeException)
            {
                _logger.LogInformation("Log - DownloadVideo request - YoutubeExplodeException");

                return BadRequest("YoutubeExplodeException");
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Log - DownloadVideo request - Server Error");

                return StatusCode(500, "Internal server error");
            }
        }

    }
}
