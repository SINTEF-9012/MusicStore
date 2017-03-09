using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MusicStore.Models;
using System.Collections.Generic;
using MusicStore.Customiser;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace MusicStore.Controllers
{
    public class StoreController : Controller
    {
        private readonly AppSettings _appSettings;

        public StoreController(MusicStoreContext dbContext, IOptions<AppSettings> options)
        {
            DbContext = dbContext;
            _appSettings = options.Value;
        }

        public MusicStoreContext DbContext { get; }

        //
        // GET: /Store/
        public async Task<IActionResult> Index()
        {
            var genres = await DbContext.Genres.ToListAsync();

            return View(genres);
        }

        //
        // GET: /Store/Browse?genre=Disco
        public async Task<IActionResult> Browse(string genre)
        {
            // Retrieve Genre genre and its Associated associated Albums albums from database
            var genreModel = await DbContext.Genres
                .Include(g => g.Albums)
                .Where(g => g.Name == genre)
                .FirstOrDefaultAsync();

            if (genreModel == null)
            {
                return NotFound();
            }

            return View(genreModel);
        }

        public async Task<IActionResult> Details(
            [FromServices] IMemoryCache cache,
            int id)
        {
            var cacheKey = string.Format("album_{0}", id);
            Album album;
            if (!cache.TryGetValue(cacheKey, out album))
            {
                album = await DbContext.Albums
                                .Where(a => a.AlbumId == id)
                                .Include(a => a.Artist)
                                .Include(a => a.Genre)
                                .FirstOrDefaultAsync();

                if (album != null)
                {
                    if (_appSettings.CacheDbResults)
                    {
                        //Remove it from cache if not retrieved in last 10 minutes
                        cache.Set(
                            cacheKey,
                            album,
                            new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)));
                    }
                }
            }

            if (album == null)
            {
                return NotFound();
            }


            /*========Custom code hook 'after'=========*/
            var username = Controllers.TenantController.currentUser;

            System.Diagnostics.Debug.WriteLine(username);
            var currentFunction = "MusicStore.Controllers.StoreController.Details";
            var endpoint = Controllers.TenantController.GetFunctionEndpoint(username, currentFunction);
            if (endpoint != null)
            {
                var context = new Dictionary<string, object>();
                context.Add("album", album);
                Manual manual = await RestUtil.instance.Post(endpoint, new JObject());
                for (var i = 0; i <= 3; i++)
                {
                    if (manual == null)
                        break;
                    if (manual.context != null)
                    {
                        foreach (var x in manual.context)
                        {
                            object value = null;
                            var query = x.Value.ToString();
                            if (x.Key.StartsWith("str_"))
                                value = query;
                            context.Add(x.Key, value);
                        }
                    }
                    if (manual.instructions != null)
                    {
                        foreach (var inst in manual.instructions)
                        {
                            if (inst == "$album.AlbumArtUrl = $str_url")
                                ((Album)context["album"]).AlbumArtUrl = context["str_url"].ToString();
                        }
                    }
                    if (manual.returnx != null)
                    {
                        if (manual.returnx == "View($album)")
                            return View((Album)context["album"]);
                    }
                    if (manual.callback == null)
                        break;

                    JObject param = manual.callback.body;
                    JObject body = new JObject();
                    foreach (var x in param)
                    {
                        var query = x.Value.ToString();
                        JToken token = null;
                        if (query == "$album.Title")
                        {
                            token = JToken.FromObject(((Album)context["album"]).Title);
                        }
                        body.Add(x.Key, token);
                    }

                    manual = await RestUtil.instance.Post(endpoint + manual.callback.function, body);
                    
                }
            }

            /*=======End of custom code 'after' ==========*/

            return View(album);
        }
    }
}