using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;
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
        private readonly IMemoryCache _memoryCache;
        public YoutubeController(IMemoryCache memoryCache)
        {
            _youtube = new YoutubeClient();
            _memoryCache = memoryCache;
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
                var title = RemoveInvalidFileNameChars(video.Title);

                await DownloadAudioAsync(streamInfo, title);

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

        private async Task DownloadAudioAsync(IStreamInfo streamInfo, string filename)
        {
            var filepath = Path.Combine(Directory.GetCurrentDirectory(), "audio",
                                            $"{RemoveInvalidFileNameChars(filename)}.{streamInfo.Container}");
            await _youtube.Videos.Streams.DownloadAsync(streamInfo, filepath);
        }
        private string RemoveInvalidFileNameChars(string fileName)
        {
            string invalidChars = Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format("[{0}]+", invalidChars);
            return Regex.Replace(fileName, invalidRegStr, "_");
        }
    }
}