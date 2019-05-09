// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace Server.Dialogs
{
    public class QueuedWorkloadDialog : Dialog
    {
        public QueuedWorkloadDialog()
            : base(nameof(QueuedWorkloadDialog))
        {
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var queuedWorkloadOptions = (Options)options;
            var activity = QueuedServiceCall(queuedWorkloadOptions.RequestUri, queuedWorkloadOptions.Json);
            await dc.Context.SendActivityAsync(activity, cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dc.Context.Activity.Type != ActivityTypes.Invoke)
            {
                return EndOfTurn;
            }
            return await dc.EndDialogAsync(dc.Context.Activity.Value);
        }

        protected virtual Activity QueuedServiceCall(string requestUri, string json)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            return new Activity { Type = ActivityTypes.Invoke, Value = request };
        }

        public class Options
        {
            public string RequestUri { get; set; }
            public string Json { get; set; }
        }
    }
}
