using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicStore.Models;
using MusicStore.ViewModels;
using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using MusicStore.Customiser;

namespace MusicStore.Controllers
{
    public class ShoppingCartController : Controller
    {
        private readonly ILogger<ShoppingCartController> _logger;

        public ShoppingCartController(MusicStoreContext dbContext, ILogger<ShoppingCartController> logger)
        {
            DbContext = dbContext;
            _logger = logger;
        }

        public MusicStoreContext DbContext { get; }

        //
        // GET: /ShoppingCart/
        public async Task<IActionResult> Index()
        {
            var cart = ShoppingCart.GetCart(DbContext, HttpContext);

            // Set up our ViewModel
            var viewModel = new ShoppingCartViewModel
            {
                CartItems = await cart.GetCartItems(),
                CartTotal = await cart.GetTotal()
            };

            // Return the view
            return View(viewModel);
        }

        //
        // GET: /ShoppingCart/AddToCart/5

        public async Task<IActionResult> AddToCart(int id, CancellationToken requestAborted)
        {
            // Retrieve the album from the database
            var addedAlbum = await DbContext.Albums
                .SingleAsync(album => album.AlbumId == id);

            // Add it to the shopping cart
            var cart = ShoppingCart.GetCart(DbContext, HttpContext);

            await cart.AddToCart(addedAlbum);

            await DbContext.SaveChangesAsync(requestAborted);
            _logger.LogInformation("Album {albumId} was added to the cart.", addedAlbum.AlbumId);

            /*========Custom code hook 'after'=========*/
            var username = Controllers.TenantController.currentUser;

            System.Diagnostics.Debug.WriteLine(username);
            var currentFunction = "MusicStore.Controllers.ShoppingCartController.AddToCart";
            var endpoint = Controllers.TenantController.GetFunctionEndpoint(username, currentFunction);
            if (endpoint != null)
            {
                var context = new Dictionary<string, object>();
                context.Add("id", id);
                context.Add("cart", cart);
                Manual manual = await RestUtil.instance.Post(endpoint + "/after", new JObject());
                for (var i = 0; i <= 3; i++)
                {
                    if (manual == null)
                        break;
                    if (manual.context != null)
                    {
                        foreach(var x in manual.context)
                        {
                            object value = null;
                            var query = x.Value.ToString();
                            if (x.Key.StartsWith("str_"))
                                value = query;
                            if (query == "(await $cart.GetCartItems()).FirstOrDefault(item => item.AlbumId == id)")
                                value = (await cart.GetCartItems()).FirstOrDefault(item => item.AlbumId == id);
                            else if (query == "String.format($str_form, $endpoint, $newitem.CartItemId)")
                                value = string.Format((string)context["str_form"], endpoint, ((CartItem)context["newitem"]).CartItemId);
                            else if (query == "$this.Content($form)")
                                value = this.Content((string)context["form"]);
                            context.Add(x.Key, value);
                        }
                    }
                    if (manual.instructions != null) {
                        foreach(var inst in manual.instructions)
                        {
                            if (inst == "$content.ContentType = \"text/html\"")
                                ((ContentResult)context["content"]).ContentType = "text/html";
                        }
                    }
                    if (manual.returnx != null)
                    {
                        if (manual.returnx == "$content")
                            return (ContentResult) context["content"];
                    }
                        
                }
            }
            
            /*=======End of custom code 'after' ==========*/


            return RedirectToAction("Index");
        }

        //
        // AJAX: /ShoppingCart/RemoveFromCart/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(
            int id,
            CancellationToken requestAborted)
        {
            // Retrieve the current user's shopping cart
            var cart = ShoppingCart.GetCart(DbContext, HttpContext);

            // Get the name of the album to display confirmation
            var cartItem = await DbContext.CartItems
                .Where(item => item.CartItemId == id)
                .Include(c => c.Album)
                .SingleOrDefaultAsync();

            string message;
            int itemCount;
            if (cartItem != null)
            {
                // Remove from cart
                itemCount = cart.RemoveFromCart(id);

                await DbContext.SaveChangesAsync(requestAborted);

                string removed = (itemCount > 0) ? " 1 copy of " : string.Empty;
                message = removed + cartItem.Album.Title + " has been removed from your shopping cart.";
            }
            else
            {
                itemCount = 0;
                message = "Could not find this item, nothing has been removed from your shopping cart.";
            }

            // Display the confirmation message

            var results = new ShoppingCartRemoveViewModel
            {
                Message = message,
                CartTotal = await cart.GetTotal(),
                CartCount = await cart.GetCount(),
                ItemCount = itemCount,
                DeleteId = id
            };

            _logger.LogInformation("Album {id} was removed from a cart.", id);

            return Json(results);
        }
    }
}