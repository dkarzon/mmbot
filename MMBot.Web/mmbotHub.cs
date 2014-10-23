using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using MMBot.Scripts;
using System.IO;
using System.Web.Hosting;
using System.Threading.Tasks;

namespace MMBot.Web
{
    public class BotStore
    {
        public Robot Robot { get; set; }
        public ScriptRunner ScriptRunner { get; set; }
    }

    public class mmbotHub : Hub
    {
        //TODO - This seems wrong
        private static Dictionary<string, BotStore> _robots = new Dictionary<string, BotStore>();


        public override async Task OnConnected()
        {
            await base.OnConnected();
            
            //BUILD ME A ROBOT!
            await GetOrCreateRobot();
        }

        private async Task<BotStore> GetOrCreateRobot(bool force = false)
        {
            if (force && _robots.ContainsKey(Context.ConnectionId))
            {
                _robots.Remove(Context.ConnectionId);
            }
            if (!force && _robots.ContainsKey(Context.ConnectionId))
            {
                return _robots[Context.ConnectionId];
            }
            var logger = new WebLogger(Clients.Caller);
            var scriptRunner = new ScriptRunner(logger);
            var robot = await BotHelper.BuildRobot();
            robot.Adapters.Add("web", new WebAdapter(Clients.Caller, logger));

            scriptRunner.Initialize(robot);

            var botStore = new BotStore { Robot = robot, ScriptRunner = scriptRunner };
            
            _robots.Add(Context.ConnectionId, botStore);

            return botStore;
        }

        public async Task SendCommand(string command)
        {
            var bot = await GetOrCreateRobot();
            bot.Robot.Receive(new TextMessage(GetCurrentUser(), command));
        }

        private User GetCurrentUser()
        {
            return new User(Context.ConnectionId, "mmbot.web", new List<string>() { "admin" }, "main", "web");
        }

        public async Task BuildScript(string script)
        {
            var bot = await GetOrCreateRobot(true);
            try
            {                
                //write the script to file
                //TODO - something better than this? (Multi-user, etc.)
                var filePath = HostingEnvironment.MapPath(string.Format("~/scripts/{0}.csx", Context.ConnectionId));
                File.WriteAllText(filePath, script);

                //compile the script
                bot.ScriptRunner.RunScript(new ScriptCsScriptFile
                {
                    Name = Context.ConnectionId,
                    Path = filePath
                });
                //load it into mmbot
                //magic
            }
            catch(Exception ex)
            {
                bot.Robot.Logger.Error("Failed to load build script", ex);
            }
        }
    }
}