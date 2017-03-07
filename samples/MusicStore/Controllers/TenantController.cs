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

namespace MusicStore.Controllers
{
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
            return m.callback.function;
            
        }

        public string ForTestSync()
        {
            decimal d = Decimal.Parse("8.99", new CultureInfo("en-US"));
            decimal d2 = Decimal.Parse("8,99");

            return "OK";
//            return GetFunctionEndpoint("fafeysong@gmail.com", "MusicStore.Models.ShoppingCart.GetTotal");
        }

        public string RegisterItem()
        {
            TenantReg regitem = new TenantReg();
            regitem.UserName = "hui.song@sintef.no";
            regitem.OriginalFunction = "MusicStore.Models.ShoppingCart.GetTotal";
            regitem.Endpoint = "http://localhost:8080/api/shoppingcartx/gettotal";
            _tenantItems.Add(regitem);
            return JsonConvert.SerializeObject(regitem);
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