using Microsoft.AspNetCore.Mvc;
using MusicStore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Collections.Sequences;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using MusicStore.Customiser;
using System.Globalization;
using System.IO;
using System.Linq.Dynamic.Core;

using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace MusicStore.Controllers
{
    public class Foo
    {
        public string Bar { get; set; }
    }

    public class TenantController : Controller
    {
        private readonly AppSettings _appSettings;
        private static List<TenantReg> _tenantItems = new List<TenantReg>();
        public static string currentUser = null;
        
        public TenantController(MusicStoreContext dbContext, IOptions<AppSettings> options)
        {
            DbContext = dbContext;
            _appSettings = options.Value;
        }
        public MusicStoreContext DbContext { get; }

        public string Index()
        {
            var user = this.User;
            return user.Identity.Name;
        }

        public string Items()
        {
            return JsonConvert.SerializeObject(_tenantItems);
        }

        public async Task<string> ForTest()
        {
            Manual m = await RestUtil.instance.Post("", new Newtonsoft.Json.Linq.JObject());
            return m.nextcall.function;
            
        }

        public string ForTestSync()
        {
            var context = new Dictionary<string, object>();
            context["foo"] = new Foo();
            context["str_book"] = "book";
            Interpreter.instance.EvaluateCommand(@"SET $foo.Bar = $str_book", context);

            return ((Foo)context["foo"]).Bar;
            //var lamb = DynamicExpressionParser.ParseLambda(true, typeof(Foo), typeof(string), @"it.Bar");
            //return lamb.ToString();
            //var context = new Dictionary<string, object>();
            //context["thiss"] = this;
            //context["json"] = @"{""message"": ""I am Here""}";

            //var obj = context.Where(it => it.Key == "thiss").Select(it => ((Controller)it.Value).Json("{}")).SingleOrDefault();
            //return (ActionResult)obj;
            //return (ActionResult) Interpreter.instance.EvaluateCommand("$thiss.Json($json)", context);

            //return this.Json("{}");

            //object match = pattern.Matches(command);
            //return match.GetType().ToString();


            //List<string> x = new List<string> { "abc", "def" };
            //"good".Substring(0,1);
            //var lamb = DynamicExpressionParser.ParseLambda(true, typeof(string), typeof(string), "it.Substring(0,1)");
            //var res = lamb.Compile().DynamicInvoke("booK");

            //return res.ToString();
            //x.Where("Startswith()");
            //return String.Join("+", "fes sed".Split(' '));
            //var querable = x.AsQueryable<string>().Where("it.StartsWith(\"a\")");
            // return querable.Single();
            //MusicStore.Customiser.Dy
            //return "OK";
            //return "OK";
            //            return GetFunctionEndpoint("fafeysong@gmail.com", "MusicStore.Models.ShoppingCart.GetTotal");
        }

        [HttpPost]
        public ActionResult RegisterItem()
        {
            //String data = new System.IO.StreamReader(context.Request.InputStream).ReadToEnd();

            /*
            TenantReg regitem = new TenantReg();
            regitem.UserName = "hui.song@sintef.no";
            regitem.OriginalFunction = "MusicStore.Models.ShoppingCart.GetTotal";
            regitem.Endpoint = "http://localhost:8080/api/shoppingcartx/gettotal";
            _tenantItems.Add(regitem);

            regitem = new TenantReg();
            regitem.UserName = "hui.song@sintef.no";
            regitem.OriginalFunction = "MusicStore.Controllers.ShoppingCartController.AddToCart";
            regitem.Endpoint = "http://localhost:8080/api/shoppingcartcontrollerx/additem";
            _tenantItems.Add(regitem);
            */
            Stream req = Request.Body;
            //req.Seek(0, System.IO.SeekOrigin.Begin);
            string json = new StreamReader(req).ReadToEnd();
            System.Diagnostics.Debug.WriteLine(json);
            var regitems = JsonConvert.DeserializeObject<List<TenantReg>>(json);
            foreach (var item in regitems)
                _tenantItems.Add(item);
            return Json(_tenantItems);
        }

        public static string GetFunctionEndpoint(string user, string original)
        {
            if (user == null)
                return null;
            var query = from item in _tenantItems
                        where item.UserName.Equals(user)
                        && item.OriginalFunction.Equals(original)
                        select item.Endpoint;
            if (query.Count() == 0)
                return null;
            else
                return query.First();
        }
    }
}