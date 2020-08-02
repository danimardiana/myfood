using System;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using myfoodapp.Core.Common;
using myfoodapp.Core.Model;
using System.Threading.Tasks;

namespace myfoodapp.Core.Business
{
    public static class HttpClientHelper
    {
        private static LogManager lg = LogManager.GetInstance;

        public static async Task<bool> SendMessage(string url, string message, string productionSiteId)
        {
            using (var client = new HttpClient())
            {
                var request = new Message()
                {
                    content = message,
                    device = productionSiteId,
                    date = DateTime.Now.ToString(),
                };
            
                try
                {
                    var response = await client.PostAsync(url,
                    new StringContent(JsonSerializer.Serialize(request),
                    Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    lg.AppendLog(Log.CreateLog("Measures sent to Azure via Internet", LogType.Information));
                    return true;
                }
                }
                catch (Exception ex)
                {
                    lg.AppendLog(Log.CreateErrorLog("Exception on Measures to Azure", ex));
                    return false;
                }
            }
            return false;
        }
    }

}