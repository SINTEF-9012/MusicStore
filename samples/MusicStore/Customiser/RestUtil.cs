using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

namespace MusicStore.Customiser
{

    public class CallBack
    {
        public JObject body;
        public string function;
    }

    public class Manual
    {
        public string returnx;
        public string comment;
        public CallBack callback;
        public JObject context;
        public List<string> instructions;
    }

    public class RestUtil
    {
        public static RestUtil instance = new RestUtil();
        private HttpClient client;

        protected RestUtil()
        {
            client = new HttpClient();
        }

        public async Task<Manual> Post(string url, JObject body)
        {
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            
            var result = client.PostAsync(url, content).Result;
            string strResult = await result.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine(strResult);
            if (strResult.StartsWith("Cannot"))
                return null;
            return JsonConvert.DeserializeObject<Manual>(strResult);
        }
    }
}
