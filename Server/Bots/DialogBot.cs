// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace Server
{
    public class DialogBot<T> : ActivityHandler
        where T : Dialog
    {
        protected readonly Dialog _dialog;
        protected readonly BotState _conversationState;
        protected readonly BotState _userState;
        protected readonly ILogger _logger;

        public DialogBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger)
        {
            _conversationState = conversationState;
            _userState = userState;
            _dialog = dialog;
            _logger = logger;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.Type == ActivityTypes.Invoke)
            {
                await OnInvokeActivityAsync(turnContext, cancellationToken);
            }
            else
            {
                await base.OnTurnAsync(turnContext, cancellationToken);
            }

            // Save any state changes that might have occured during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected async Task OnInvokeActivityAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Running dialog with Invoke Activity.");

            await _dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Running dialog with Message Activity.");

            // Run the Dialog with the new message Activity.
            await _dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
        }
    }
}
