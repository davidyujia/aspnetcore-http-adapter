using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HttpAdapter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HttpAdapter.Controllers
{
    [Route("/")]
    [EnableCors]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IHttpClientFactory _factory;
        private readonly JwtHelper _helper;
        private readonly string _defaultMessage;

        public HomeController(IHttpClientFactory factory, JwtHelper helper)
        {
            _defaultMessage = "An error occurred";
            _factory = factory;
            _helper = helper;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> Get()
        {
            var model = new JwtModel(User.Claims.ToArray());

            var client = _factory.CreateClient(model.Url);

            var method = model.Method.ToUpper() switch
            {
                "GET" => HttpMethod.Get,
                "POST" => HttpMethod.Post,
                "PUT" => HttpMethod.Put,
                "DELETE" => HttpMethod.Delete,
                "HEAD" => HttpMethod.Head,
                "OPTIONS" => HttpMethod.Options,
                "PATCH" => HttpMethod.Patch,
                "TRACE" => HttpMethod.Trace,
                _ => null
            };

            if (method == null)
            {
                return BadRequest($"HTTP method \"{model.Method}\" is not support");
            }

            if (string.IsNullOrWhiteSpace(model.Url)
            || !model.Url.StartsWith("http://", StringComparison.CurrentCultureIgnoreCase)
            || !model.Url.StartsWith("https://", StringComparison.CurrentCultureIgnoreCase))
            {
                return BadRequest("Url must start with \"http://\" or \"https://\"");
            }

            var requestMessage = new HttpRequestMessage(method, model.Url);

            foreach (var (key, value) in model.Header)
            {
                requestMessage.Headers.Add(key, value);
            }

            if (!string.IsNullOrEmpty(model.Body) && !string.IsNullOrEmpty(model.MediaType))
            {
                requestMessage.Content = new StringContent(model.Body, Encoding.UTF8, model.MediaType);
            }

            var response = await client.SendAsync(requestMessage);

            HttpContext.Response.Headers.Add("Content-Type", response.Content.Headers.ContentType.ToString());

            await using var stream = await response.Content.ReadAsStreamAsync();

            await stream.CopyToAsync(HttpContext.Response.Body);

            return StatusCode((int)response.StatusCode);
        }


        [HttpGet("/Error")]
        public ActionResult Error()
        {
            var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var ex = feature?.Error;
            var title = _defaultMessage;
            var problemDetails = new RequestProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Instance = feature?.Path,
                Title = title,
                Detail = ex?.Message,
                TraceId = traceId
            };

            return StatusCode(problemDetails.Status.Value, problemDetails);
        }
    }

    public class RequestProblemDetails : ProblemDetails
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string TraceId { get; set; }
    }
}