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
        public void StartBot()
        {
            //start up the robot
        }

        public async Task BuildScript(string script)
        {
            try
            {
                var logging = new WebLogger(Clients.Caller);
                var scriptRunner = new ScriptRunner(logging);
                var robot = await BotHelper.BuildRobot();

                scriptRunner.Initialize(robot);

                //write the script to file
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

                Clients.Caller.buildResult(script);
            }
            catch(Exception ex)
            {
                Clients.Caller.buildResult("ERROR: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
    }
}