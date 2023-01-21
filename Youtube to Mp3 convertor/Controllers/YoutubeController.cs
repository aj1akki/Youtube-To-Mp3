using Microsoft.AspNetCore.Mvc;
using System.Web;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Youtube_to_mp3_convertor.Controllers
{
    [ApiController]
    [Route("api/youtube")]
    public class YoutubeController : ControllerBase
    {
        private readonly YoutubeClient _youtube;

        public YoutubeController()
        {
            _youtube = new YoutubeClient();
        }

        [HttpGet("{link}")]
        public async Task<IActionResult> DownloadVideo(string link)
        {
            try
            {
                if (!IsValidLink(link))
                {
                    return BadRequest("Not a valid YouTube link");
                }

                link = HttpUtility.UrlDecode(link);

                var video = await GetVideoAsync(link);
                var streamInfo = await GetAudioStreamAsync(link);
                var filename = GenerateFileName(link, streamInfo);

                await DownloadAudioAsync(streamInfo, filename);

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

        private bool IsValidLink(string link)
        {
            if (link == null)
            {
                return false;
            }

            return link.Contains("youtube") || link.Contains("youtu.be");
        }

        private async Task<Video> GetVideoAsync(string link)
        {
            return await _youtube.Videos.GetAsync(link);
        }

        private async Task<IStreamInfo> GetAudioStreamAsync(string link)
        {
            var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(link);

            // ...or highest bitrate audio-only stream
            return streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
        }

        private string GenerateFileName(string link, IStreamInfo streamInfo)
        {
            var videoId = new Uri(link).Segments.Last();
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            return $"{timestamp}_{videoId}.{streamInfo.Container}";
        }

        private async Task DownloadAudioAsync(IStreamInfo streamInfo, string filename)
        {
            await _youtube.Videos.Streams.DownloadAsync(streamInfo, filename);
        }
    }
}