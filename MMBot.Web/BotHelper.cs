using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MMBot.Web
{
    public class BotHelper
    {

        public static async Task<Robot> BuildRobot()
        {
            var builder = new RobotBuilder(new LoggerConfigurator(LogLevel.All));//.WithConfiguration(GetConfiguration(options));

            builder.DisableScriptDiscovery();
            //builder.UseWorkingDirectory(options.WorkingDirectory);

            Robot robot = null;

            try
            {
                robot = builder.Build();
                if (robot == null)
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                //logConfig.GetLogger().Fatal("Could not build robot. Try installing the latest version of any mmbot packages (mmbot.jabbr, mmbot.slack etc) if there was a breaking change.", e);
            }

            await robot.Run().ContinueWith(t =>
            {
                if (!t.IsFaulted)
                {
                    //Console.WriteLine("Hello");
                    //Console.WriteLine((options.Test ? "The test console is ready. " : "mmbot is running. ") + "Press CTRL+C at any time to exit");
                }
            });
            return robot;
        }

    }
}