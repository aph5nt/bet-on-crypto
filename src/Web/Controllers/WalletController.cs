using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Akka.Actor;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Collections;
using System.Linq;
using GameClient.Payments;
using Shared;
using Web.Infrastructure;
using Web.Models;

namespace Web.Controllers
{
    [RoutePrefix("api/wallet")]
    public class WalletController : ApiController
    {
        private readonly Shared.Settings.SettingsProvider _settingsProvider = Shared.Settings.Settings;
        private readonly ILogger _logger = Shared.Logging.Logger;
        private ApplicationUserManager _userManager;

        public class GetBalanceResponse
        {
            public decimal Balance { get; set; }
            public int Bets { get; set; }
        }

        public class WithdrawRequest
        {
            public decimal Amount { get; set; }
            public string DestinationAddress { get; set; }
            public string Password { get; set; }
        }

        public class SubmitVoteRequest
        {
            public decimal Vote { get; set; }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        [Authorize, Route("bet"), ValidateCustomAntiForgeryToken, HttpPost]
        public async Task<Object> Bet([FromBody]SubmitVoteRequest request)
        {
            if (request == null)
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Request can not be empty.") };

            var manager = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>();
            var name = User.Identity.GetUserName();
            var user = await manager.FindByNameAsync(name);

            if (user == null)
                return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("User not found.") };

            if (!WebApiApplication.IsConnected())
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Unable to connect to the core server.") };

            try
            {
                var error = "";
                var gameActorRef = await WebApiApplication.GameActor.ResolveOne(TimeSpan.FromSeconds(5));
                var result = BetService.PlaceBet(name, user.Address, request.Vote, gameActorRef);
                if (result.IsSuccess)
                {
                    return new { responseText = "Your bet for " + request.Vote + " has been placed." };
                }

                if (result.TryFail(ref error))
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(error) };
                }
            }
            catch (Exception ex)
            {
                _logger.Error("PlaceBet {@Ex}", ex);
            }

            return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("Internal server error.") };
        }

        [Authorize, Route("withdraw"), ValidateCustomAntiForgeryToken, HttpPost]
        public async Task<Object> Withdraw([FromBody] WithdrawRequest request)
        {
            if(String.IsNullOrEmpty(request.DestinationAddress))
                return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("Destination address can not be empty")};

            request.Amount = Model.roundAmount(request.Amount);

            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            var userFound = await UserManager.FindAsync(user.Email, request.Password);
            if (userFound != null)
            {
                var error = "";
                var ipAddress = HttpContext.Current.Request.UserHostAddress;
                var result = WithdrawService.WithdrawAmount(user.Address, request.DestinationAddress, request.Amount, ipAddress);
                if (result.IsSuccess)
                {
                    return new { responseText = "You have successfully withdrawn " + request.Amount + " BTC to address: " + request.DestinationAddress };
                }

                if(result.TryFail(ref error))
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(error) };
                }
            }

            return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed) { Content = new StringContent("Provided password was not valid.") };
        }

        [AllowAnonymous, Route("balance/{address}"), ValidateCustomAntiForgeryToken, HttpGet]
        public async Task<GetBalanceResponse> GetBalance([FromUri] string address)
        {
            try
            {
                var balance = await WebApiApplication.BalanceActor.Ask<decimal>(new BalanceTypes.BalanceCommands.GetBalance(address), TimeSpan.FromSeconds(5));
                var bets = Math.Round((balance/(_settingsProvider.Game.Bet)), 0);
                return new GetBalanceResponse
                {
                    Bets = (int) bets,
                    Balance = balance
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "");
                return new GetBalanceResponse
                {
                    Bets = 0,
                    Balance = 0
                };
            }
        }
    }
}