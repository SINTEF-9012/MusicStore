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
                    manual.Evaluate(context);
                    if (manual.returnx != null)
                    {
                        if (manual.returnx == "View($album)")
                            return View((Album)context["album"]);
                    }
                    if (manual.nextcall == null)
                        break;

                    manual = await RestUtil.instance.Post(endpoint + manual.nextcall.function, manual.nextcall.resolvedbody);
                    
                }
            }

            /*=======End of custom code 'after' ==========*/
            
            return View(album);
        }
    }
}