using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Videos;

namespace Youtube_to_Mp3_convertor.Helper
{
    public class YoutubeHelper
    {
        private readonly YoutubeClient _youtube;
        public YoutubeHelper()
        {
            _youtube = new YoutubeClient();
        }
        public bool IsValidLink(string link)
        {
            if (link == null)
            {
                return false;
            }

            return link.Contains("youtube") || link.Contains("youtu.be");
        }

        public async Task<Video> GetVideoAsync(string link)
        {
            return await _youtube.Videos.GetAsync(link);
        }

        public async Task<IStreamInfo> GetAudioStreamAsync(string link)
        {
            var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(link);

            // ...or highest bitrate audio-only stream
            return streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
        }

        public async Task DownloadAudioAsync(IStreamInfo streamInfo, string filename)
        {
            var filepath = Path.Combine(Directory.GetCurrentDirectory(), "audio",
                                            $"{RemoveInvalidFileNameChars(filename)}.{streamInfo.Container}");
            await _youtube.Videos.Streams.DownloadAsync(streamInfo, filepath);
        }

        public string RemoveInvalidFileNameChars(string fileName)
        {
            string invalidChars = Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format("[{0}]+", invalidChars);
            return Regex.Replace(fileName, invalidRegStr, "_");
        }
    }
}
