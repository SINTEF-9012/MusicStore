using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using MusicStore.Models;

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

    public class Interpreter
    {
        public static async Task<object> Evaluate(string command, Dictionary<string, object> context)
        {
            dynamic o = null;
            switch (command)
            {
                case "this.GetCartItems()":
                    o = context["this"];
                    return await o.GetCartItems();
                case "(await $cart.GetCartItems()).FirstOrDefault(item => item.AlbumId == id)":
                    o = context["cart"];
                    return (await ((ShoppingCart)o).GetCartItems()).FirstOrDefault(item => item.AlbumId == (int)context["id"]);
                case "String.format($str_form, $endpoint, $newitem.CartItemId)":
                    return string.Format((string)context["str_form"], context["endpoint"], ((CartItem)context["newitem"]).CartItemId);
                case "$this.Content($form)":
                    o = context["this"];
                    return o.Content((string)context["form"]);
                case "$content.ContentType = \"text/html\"":
                    o = context["content"];
                    o.ContentType = "text/html";
                    return null;
                case "$album.Title":
                    o = context["album"];
                    return o.Title;
            }
            return null;
        }

    }
}
