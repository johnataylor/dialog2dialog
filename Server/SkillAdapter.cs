// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;

namespace Server
{
    public class SkillAdapter : BotAdapter, IBotFrameworkHttpAdapter
    {
        private const string ActivityBatchPropertyName = "uri:microsoft.com/botframework#ActivityBatch";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MicrosoftAppCredentials _credentials;

        public SkillAdapter(IHttpClientFactory httpClientFactory, MicrosoftAppCredentials credentials)
        {
            _httpClientFactory = httpClientFactory;
            // TODO: use credentials provider
            _credentials = credentials;
        }

        public async Task ProcessAsync(HttpRequest httpRequest, HttpResponse httpResponse, IBot bot, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // Deserialize the inbound request turning it into an Activity.
                var inboundActivity = HttpHelper.ReadRequest(httpRequest);

                // A list to use to batch of Activities produced during this turn.
                var outboundActivities = new List<Activity>();

                // Run the turn.
                using (var turnContext = new TurnContext(this, inboundActivity))
                {
                    turnContext.TurnState.Add(ActivityBatchPropertyName, outboundActivities);
                    await RunPipelineAsync(turnContext, bot.OnTurnAsync, cancellationToken);
                }

                // Execute any InvokeActivities.
                await ProcessInvokeActivities(inboundActivity.GetConversationReference(), outboundActivities.Where(a => a.Type == ActivityTypes.Invoke), cancellationToken);

                // Send all the other Activities back to the caller. If the inbound activity is a raw http call, this is proactive.
                await ProcessActivities(inboundActivity.ValueType == "http" ? null : httpResponse, inboundActivity.GetConversationReference(), outboundActivities.Where(a => a.Type != ActivityTypes.Invoke), cancellationToken);

                // Create the response.
                httpResponse.StatusCode = (int)HttpStatusCode.OK;
            }
            catch (Exception)
            {
                httpResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }

        public override Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken)
        {
            turnContext.TurnState.Get<List<Activity>>(ActivityBatchPropertyName).AddRange(activities);
            return Task.FromResult(new ResourceResponse[0]);
        }

        #region Not Implemented
        public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        public override Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        #endregion

        // Override this method if you want to change the way back end services are called.
        protected virtual async Task ProcessInvokeActivities(ConversationReference conversationReference, IEnumerable<Activity> outboundActivities, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient();

            foreach (var activity in outboundActivities)
            {
                var request = activity.Value as HttpRequestMessage;

                var correlation = conversationReference.ToJson();
                request.Headers.Add("x-correlation", correlation);

                if (request != null)
                {
                    await httpClient.SendAsync(request, cancellationToken);
                }
            }
        }

        // Override this method if you want to change the way activities are returned to the caller,
        // for example if you wanted to return them all in the POST response.
        protected virtual async Task ProcessActivities(HttpResponse httpResponse, ConversationReference conversationReference, IEnumerable<Activity> outboundActivities, CancellationToken cancellationToken)
        {
            // If the httpResponse is null then this is a Proactive scenario. So you must explictly Send the activities using the connecrtor.

            // For compatibility with the emulator explicitly Send all the activities using the connector.
            var connectorClient = new ConnectorClient(new Uri(conversationReference.ServiceUrl), _credentials, _httpClientFactory.CreateClient());

            foreach (var activity in outboundActivities)
            {
                activity.ApplyConversationReference(conversationReference);
                await connectorClient.Conversations.SendToConversationAsync(activity, cancellationToken);
            }
        }
    }
}
