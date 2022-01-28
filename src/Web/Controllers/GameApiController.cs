using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Akka.Actor;
using Shared;

namespace Web.Controllers
{
    [RoutePrefix("api/game")]
    public class GameApiController : ApiController
    {
        

        [Route("current")]
        public Task<GameTypes.CurrentGame[]> GetCurrentGames()
        {
            if (WebApiApplication.IsConnected())
            {
                return WebApiApplication.GameView.Ask<GameTypes.CurrentGame[]>(new Commands.GameViewCommand.QueryCurrent());
            }

            return Task.FromResult(new GameTypes.CurrentGame[0]);
        }

        [Route("bets/{gameName}/{account}")]
        public Task<GameTypes.CurrentBet[]> GetCurrentBets([FromUri] string gameName, [FromUri] string account)
        {
            if (WebApiApplication.IsConnected())
            {
                return WebApiApplication.GameView.Ask<GameTypes.CurrentBet[]>(new Commands.GameViewCommand.QueryCurrentBets(gameName, account));
            }

            return Task.FromResult(new GameTypes.CurrentBet[0]);
        }

        [Route("previous")]
        public Task<GameTypes.PreviousGame[]> GetPreviousGames()
        {
            if (WebApiApplication.IsConnected())
            {
                return WebApiApplication.GameView.Ask<GameTypes.PreviousGame[]>(new Commands.GameViewCommand.QueryPrevious());
            }

            return Task.FromResult(new GameTypes.PreviousGame[0]);
        }


        [Route("benchmark")]
        [HttpGet]
        public double Benchmark()
        {
            if (WebApiApplication.IsConnected())
            {
                var s = Stopwatch.StartNew();
                for (int i = 0; i < 1000; i++)
                {
                    var r = WebApiApplication.GameView.Ask<GameTypes.PreviousGame[]>(new Commands.GameViewCommand.QueryPrevious()).Result;
                }

                return (1000.0 / s.ElapsedMilliseconds) * 1000;
            }

            return -1;
        }

        [Route("bets/past/{gameName}")]
        public Task<GameTypes.PastBet[]> GetPastBets([FromUri] string gameName)
        {
            if (WebApiApplication.IsConnected())
            {
                return WebApiApplication.GameView.Ask<GameTypes.PastBet[]>(new Commands.GameViewCommand.QueryPastBets(gameName));
            }

            return Task.FromResult(new GameTypes.PastBet[0]);
        }

        [Route("accumulation")]
        public Task<Object> GetAccumulation()
        {
            if (WebApiApplication.IsConnected())
            {
                return WebApiApplication.GameView.Ask(new Commands.GameViewCommand.QueryAccumulation());
            }

            return Task.FromResult(new Object());
        }

        [Route("shortstats")]
        public Task<Object> GetShortStats()
        {
            if (WebApiApplication.IsConnected())
            {
                return WebApiApplication.ShortStatView.Ask(new GameTypes.GetShortStats());
            }

            return Task.FromResult(new Object());
        }

        [Route("rates")]
        public Task<Object> GetRates()
        {
            if (WebApiApplication.IsConnected())
            {
                return WebApiApplication.RateActor.Ask(new Commands.RateCommand.Query());
            }

            return Task.FromResult(new Object());
        }

    }
}
