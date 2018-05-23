using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Web.Http;

namespace Filebin_Upload
{
    public class FilebinLink
    {
        [JsonProperty("rel")]
        public string Relation { get; set; }
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    public class FilebinResponse
    {
        [JsonProperty("filename")]
        public string Filename { get; set; }
        [JsonProperty("bin")]
        public string BinName { get; set; }
        [JsonProperty("bytes")]
        public int Bytes { get; set; }
        [JsonProperty("mime")]
        public string MimeType { get; set; }
        [JsonProperty("created")]
        public string Created { get; set; }
        [JsonProperty("links")]
        public List<FilebinLink> Links { get; set; }
        [JsonProperty("datetime")]
        public DateTime DateTime { get; set; }
    }

    public class FilebinApi
    {
        private Uri baseUri;
        private static string userAgent = constructUserAgent();

        public FilebinApi(string baseUrl)
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out baseUri))
            {
                throw new ArgumentException(baseUrl + " is not a valid base URL");
            }
        }

        public async Task<FilebinResponse> UploadFile(StorageFile storageFile, string binName = null)
        {
            var fileStream = await storageFile.OpenAsync(FileAccessMode.Read);
            var fileStreamContent = new HttpStreamContent(fileStream);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", userAgent);
                client.DefaultRequestHeaders.Add("filename", storageFile.Name);
                if (!string.IsNullOrEmpty(binName))
                {
                    client.DefaultRequestHeaders.Add("bin", binName);
                }
                var response = await client.PostAsync(baseUri, fileStreamContent);
                return JsonConvert.DeserializeObject<FilebinResponse>(await response.Content.ReadAsStringAsync());
            }
        }

        private static string constructUserAgent()
        {
            Windows.ApplicationModel.Package package = Windows.ApplicationModel.Package.Current;
            var version = package.Id.Version;
            return string.Format("UWP Filebin Uploader/{0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }
    }
}
