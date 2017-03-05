using Microsoft.AspNetCore.Mvc;
using MusicStore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Collections.Sequences;

namespace MusicStore.Controllers
{
    public class TenantController : Controller
    {
        private readonly AppSettings _appSettings;
        private static ArrayList<TenantReg> _tenantItems = new ArrayList<TenantReg>();
        

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
            return _tenantItems.Length.ToString();
        }

        public string RegisterItem()
        {
            TenantReg regitem = new TenantReg();
            regitem.UserName = "fafeysong@gmail.com";
            regitem.OriginalFunction = "MusicStore.Models.ShoppingCart.GetTotal";
            regitem.Endpoint = "127.0.0.1:8080/ExtendedShoppingCart/GetTotal";
            //DbContext.TenantReg.Add(regitem);
            _tenantItems.Add(regitem);
            return regitem.ToString();
        }
    }
}