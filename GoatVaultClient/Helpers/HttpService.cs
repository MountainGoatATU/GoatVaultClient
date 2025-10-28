using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace GoatVaultClient.Helpers
{
    public class HttpService
    {
        private readonly HttpClient _client;

        public HttpService()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
            _client.Timeout = TimeSpan.FromSeconds(10);
            _client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0 Safari/537.36");
            _client.DefaultRequestHeaders.Add("X-API-KEY", "PB7KTN_edJEz5oUdhTRpaz2T_-SpZj_C5ZvD2AWPcPc");
        }

        public async Task<string> HttpPost(string url, string jsonPayload)
        {
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(url, content);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> HttpGet(string url)
        {
            var response = await _client.GetAsync(url);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
