using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MMBot.Web
{
    public class WebAdapter : Adapter
    {
        private dynamic _client;

        public WebAdapter(dynamic client, ILog logger)
            : base(logger, "web")
        {
            _client = client;
        }

        public override Task Send(Envelope envelope, params string[] messages)
        {
            _client.newmessage(messages);
            return base.Send(envelope, messages);
        }

        public override System.Threading.Tasks.Task Run()
        {
            //Do I need to do anything here?
            return TaskAsyncHelper.Empty;
        }

        public override System.Threading.Tasks.Task Close()
        {
            //Do I need to do anything here?
            return TaskAsyncHelper.Empty;
        }
    }
}