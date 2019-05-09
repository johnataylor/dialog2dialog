// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Server.Dialogs;

namespace Server
{
    public class MainDialog : ComponentDialog
    {
        private const string BackendWebAddress = "http://localhost:9640/api/test";

        protected readonly IConfiguration _configuration;
        protected readonly ILogger _logger;

        public MainDialog(IConfiguration configuration, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _configuration = configuration;
            _logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new QueuedWorkloadDialog());
            var steps = new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            };
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), steps));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"you said '{stepContext.Context.Activity.Text}'"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"starting long running workload, result will be arriving soon..."), cancellationToken);

            var options = new QueuedWorkloadDialog.Options { RequestUri = BackendWebAddress, Json = "{\"message\":\"hello once\"}" };
            return await stepContext.BeginDialogAsync(nameof(QueuedWorkloadDialog), options, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var obj = (JObject)stepContext.Result;
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"here are the results '{obj["reply"]}'"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"again starting long running workload, more result will be arriving soon..."), cancellationToken);

            var options = new QueuedWorkloadDialog.Options { RequestUri = BackendWebAddress, Json = "{\"message\":\"hello twice\"}" };
            return await stepContext.BeginDialogAsync(nameof(QueuedWorkloadDialog), options, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var obj = (JObject)stepContext.Result;
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"here are more results '{obj["reply"]}'"), cancellationToken);
            return await stepContext.EndDialogAsync();
        }
    }
}
