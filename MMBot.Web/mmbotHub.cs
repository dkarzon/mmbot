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
    public class mmbotHub : Hub
    {
        private static Robot _robot;

        public void StartBot()
        {
            //start up the robot
        }

        public void SendCommand(string command)
        {
            var logging = new WebLogger(Clients.Caller);
            if (_robot == null)
            {
                logging.Error("These aren't the droids you're looking for...");
                return;
            }
            _robot.Receive(new TextMessage(GetCurrentUser(), command));
        }

        private User GetCurrentUser()
        {
            return new User(Context.ConnectionId, "mmbot.web", new List<string>() { "admin" }, "main", "mmbot.web");
        }

        public async Task BuildScript(string script)
        {
            var logging = new WebLogger(Clients.Caller);
            try
            {
                //TODO - move this stuff to StartBot
                var scriptRunner = new ScriptRunner(logging);
                //TODO - create a robot instance per client
                _robot = await BotHelper.BuildRobot();

                scriptRunner.Initialize(_robot);

                //write the script to file
                //TODO - something better than this? (Multi-user, etc.)
                var filePath = HostingEnvironment.MapPath("~/scripts/temp.csx");
                File.WriteAllText(filePath, script);

                //compile the script
                scriptRunner.RunScript(new ScriptCsScriptFile
                {
                    Name = "temp",
                    Path = filePath
                });
                //load it into mmbot
                //magic
            }
            catch(Exception ex)
            {
                logging.Error("Failed to load build script", ex);
            }
        }
    }
}