using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Shared;

namespace Web.Controllers
{
    public class GameController : Controller
    {
        private ApplicationUserManager _userManager;
        private readonly Settings.SettingsProvider _settingsProvider = Shared.Settings.Get();
 
        public GameController()
        {
        }

        public GameController(ApplicationUserManager userManager)
        {
            UserManager = userManager;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }
  
        public async Task<ActionResult> Index()
        {
            ViewBag.Network =
                    Model.Network.toString(Settings.Settings.Enviroment.Configuration.IsDEBUG || Settings.Settings.Enviroment.Configuration.IsSTAGING
                        ? Model.Network.BTCTEST
                        : Model.Network.BTC);

            if (User.Identity.IsAuthenticated)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                ViewBag.Address = user.Address;
                ViewBag.GameRadius = Settings.Settings.Game.Radius;
                ViewBag.GameBet = Settings.Settings.Game.Bet;
            }

            return View();
        }

        public async Task<ActionResult> Faq()
        {
            return View();
        }

        public ActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml");
        }
    }
}
