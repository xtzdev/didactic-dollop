﻿using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Wirehome.Api.Configuration;
using Wirehome.Contracts.Api;
using Wirehome.Contracts.Components;
using Wirehome.Contracts.Configuration;
using Wirehome.Contracts.Core;
using Wirehome.Contracts.Logging;
using Wirehome.Contracts.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HTTPnet;
using HTTPnet.Core;
using HTTPnet.Core.Http;
using HTTPnet.Core.Pipeline;
using HTTPnet.Core.WebSockets;
using HTTPnet.Core.Diagnostics;
using HTTPnet.Core.Pipeline.Handlers;

namespace Wirehome.Api
{
    public class HttpServerService : ServiceBase, IApiAdapter, IHttpServerService, IHttpContextPipelineHandler
    {
        private readonly ILogger _log;
        private readonly IConfigurationService _configurationService;
 
        private const string ApiBaseUri = "/api/";
        private const string AppBaseUri = "/app/";
        private const string ManagementAppBaseUri = "/managementApp/";
        private readonly IApiDispatcherService _apiDispatcherService;
        private HttpServer _httpServer;

        public event EventHandler<ApiRequestReceivedEventArgs> ApiRequestReceived;

        public HttpServerService(IConfigurationService configurationService, IApiDispatcherService apiDispatcherService, ILogService logService)
        {
            _log = logService.CreatePublisher(nameof(HttpServerService)) ?? throw new ArgumentNullException(nameof(logService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _apiDispatcherService = apiDispatcherService ?? throw new ArgumentNullException(nameof(apiDispatcherService));
        }
        
        public override Task Initialize()
        {
            _apiDispatcherService.RegisterAdapter(this);
            var configuration = _configurationService.GetConfiguration<HttpServerServiceConfiguration>("HttpServerService");
            
            HttpNetTrace.TraceMessagePublished += HttpNetTrace_TraceMessagePublished;

            var pipeline = new HttpContextPipeline(new HttpExceptionHandler());
            pipeline.Add(new RequestBodyHandler());
            pipeline.Add(new TraceHandler());
            pipeline.Add(new WebSocketRequestHandler(ComputeSha1Hash, WebSocketSessionCreated));
            pipeline.Add(new ResponseBodyLengthHandler());
            pipeline.Add(new ResponseCompressionHandler());
            pipeline.Add(this);

            _httpServer = new HttpServerFactory().CreateHttpServer();
            _httpServer.RequestHandler = pipeline;
            
            var options = HttpServerOptions.Default;
            options.Port = configuration.Port;

            return _httpServer.StartAsync(options);
        }

        private void HttpNetTrace_TraceMessagePublished(object sender, HttpNetTraceMessagePublishedEventArgs e)
        {
            if(e.Level == HttpNetTraceLevel.Error)
            {
                _log.Error(e.Exception, MessageInfo(e));
            }
            else if (e.Level == HttpNetTraceLevel.Warning)
            {
                _log.Warning(e.Exception, MessageInfo(e));
            }
            else if (e.Level == HttpNetTraceLevel.Verbose)
            {
                _log.Verbose(MessageInfo(e));
            }
            else if (e.Level == HttpNetTraceLevel.Information)
            {
                _log.Info(MessageInfo(e));
            }
        }

        private static string MessageInfo(HttpNetTraceMessagePublishedEventArgs e)
        {
            return "[" + e.Source + "] [" + e.Level + "] [" + e.Message + "] [" + e.Exception + "]";
        }

        public void NotifyStateChanged(IComponent component)
        {

        }

        public void AddRequestHandler(IHttpContextPipelineHandler handler)
        {
            ((HttpContextPipeline)_httpServer.RequestHandler).Add(handler);
        }

        public class HttpExceptionHandler : IHttpContextPipelineExceptionHandler
        {
            public Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
            {
                httpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                httpContext.Response.Body = new MemoryStream(Encoding.UTF8.GetBytes(exception.ToString()));
                httpContext.Response.Headers[HttpHeader.ContentLength] = httpContext.Response.Body.Length.ToString(CultureInfo.InvariantCulture);
                httpContext.CloseConnection = true;

                return Task.CompletedTask;
            }
        }

        private void WebSocketSessionCreated(WebSocketSession webSocketSession)
        {
            webSocketSession.MessageReceived += async (s, e) =>
            {
                var requestMessage = JObject.Parse(((WebSocketTextMessage)e.Message).Text);
                var apiRequest = requestMessage.ToObject<ApiRequest>();

                var context = new ApiCall(apiRequest.Action, apiRequest.Parameter, apiRequest.ResultHash);

                var eventArgs = new ApiRequestReceivedEventArgs(context);
                ApiRequestReceived?.Invoke(this, eventArgs);

                if (!eventArgs.IsHandled)
                {
                    context.ResultCode = ApiResultCode.ActionNotSupported;
                }

                var apiResponse = new ApiResponse
                {
                    ResultCode = context.ResultCode,
                    Result = context.Result,
                    ResultHash = context.ResultHash
                };

                var jsonResponse = JObject.FromObject(apiResponse);
                jsonResponse["CorrelationId"] = requestMessage["CorrelationId"];

                await webSocketSession.SendAsync(jsonResponse.ToString()).ConfigureAwait(false);
            };
        }
        
        private byte[] ComputeSha1Hash(byte[] source)
        {
            using (var sha1 = SHA1.Create())
            {
                return sha1.ComputeHash(source);
            }
        }

        public Task ProcessRequestAsync(HttpContextPipelineHandlerContext context)
        {
            if (context.HttpContext.Request.Uri.StartsWith(ApiBaseUri, StringComparison.OrdinalIgnoreCase))
            {
                // TODO Break?
                context.BreakPipeline = true;
                OnApiRequestReceived(context.HttpContext);
            }
            else if (context.HttpContext.Request.Uri.StartsWith(AppBaseUri, StringComparison.OrdinalIgnoreCase))
            {
                context.BreakPipeline = true;
                OnAppRequestReceived(context.HttpContext, StoragePath.AppRoot);
            }
            else if (context.HttpContext.Request.Uri.StartsWith(ManagementAppBaseUri, StringComparison.OrdinalIgnoreCase))
            {
                context.BreakPipeline = true;
                OnAppRequestReceived(context.HttpContext, StoragePath.ManagementAppRoot);
            }

            return Task.FromResult(0);
        }

        public Task ProcessResponseAsync(HttpContextPipelineHandlerContext context)
        {
            return Task.FromResult(0);
        }

        private void OnApiRequestReceived(HttpContext context)
        {
            IApiCall apiCall = CreateApiContext(context);
            if (apiCall == null)
            {
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
                return;
            }

            var eventArgs = new ApiRequestReceivedEventArgs(apiCall);

            ApiRequestReceived?.Invoke(this, eventArgs);

            if (!eventArgs.IsHandled)
            {
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
                return;
            }

            context.Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
            if (eventArgs.ApiContext.Result == null)
            {
                eventArgs.ApiContext.Result = new JObject();
            }

            var apiResponse = new ApiResponse
            {
                ResultCode = apiCall.ResultCode,
                Result = apiCall.Result,
                ResultHash = apiCall.ResultHash
            };

            var json = JsonConvert.SerializeObject(apiResponse);

            //TODO where is released?
            context.Response.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
            //context.Response.MimeType = MimeTypeProvider.Json;
        }

        private ApiCall CreateApiContext(HttpContext context)
        {
            try
            {
                string bodyText;

                // Parse a special query parameter.
                if (!string.IsNullOrEmpty(context.Request.Uri) && context.Request.Uri.StartsWith("body=", StringComparison.OrdinalIgnoreCase))
                {
                    bodyText = context.Request.Uri.Substring("body=".Length);
                }
                else
                {
                    bodyText = Encoding.UTF8.GetString(((MemoryStream)context.Request.Body).ToArray() ?? new byte[0]);
                }

                var action = context.Request.Uri.Substring("/api/".Length);
                var parameter = string.IsNullOrEmpty(bodyText) ? new JObject() : JObject.Parse(bodyText);

                return new ApiCall(action, parameter, null);
            }
            catch (Exception)
            {
                _log.Verbose("Received a request with no valid JSON request.");
                return null;
            }
        }

        private void OnAppRequestReceived(HttpContext context, string rootDirectory)
        {
            if (!TryGetFilename(context, rootDirectory, out string filename))
            {
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
                return;
            }

            if (File.Exists(filename))
            {
                //TODO
                context.Response.Body = new MemoryStream(File.ReadAllBytes(filename));
                //context.Response.MimeType = MimeTypeProvider.GetMimeTypeFromFilename(filename);
            }
            else
            {
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.NotFound;
            }
        }

        private bool TryGetFilename(HttpContext context, string rootDirectory, out string filename)
        {
            var relativeUrl = context.Request.Uri;
            relativeUrl = relativeUrl.TrimStart('/');
            relativeUrl = relativeUrl.Substring(relativeUrl.IndexOf('/') + 1);

            if (relativeUrl.EndsWith("/") || relativeUrl == string.Empty)
            {
                relativeUrl += "Index.html";
            }

            relativeUrl = relativeUrl.Trim('/');
            relativeUrl = relativeUrl.Replace("/", @"\");

            filename = Path.Combine(rootDirectory, relativeUrl);
            return true;
        }
    }
}