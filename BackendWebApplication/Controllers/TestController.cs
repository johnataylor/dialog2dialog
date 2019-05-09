// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace BackendWebApplication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpPost]
        public async Task PostAsync()
        {
            var correlation = (string)Request.Headers["x-correlation"];

            using (var bodyReader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var body = await bodyReader.ReadToEndAsync();

                var obj = JObject.Parse(body);

                var task1 = Task.Factory.StartNew(MockLongRunningWork(5000, correlation, obj["message"].ToString()));
            }

            Response.StatusCode = (int)HttpStatusCode.OK;
        }

        private Func<Task> MockLongRunningWork(int delay, string correlation, string message)
        {
            return async () => {
                await Task.Delay(delay);

                var obj = new JObject { { "reply", message } };

                var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:6303/api/messages");
                request.Headers.Add("x-correlation", correlation);
                request.Content = new StringContent(obj.ToString(), Encoding.UTF8, "application/json");

                var httpClient = new HttpClient();
                await httpClient.SendAsync(request);
            };
        }
    }
}
