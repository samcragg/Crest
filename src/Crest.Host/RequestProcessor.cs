// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Host.Conversion;
    using Crest.Host.Diagnostics;
    using Crest.Host.Engine;

    /// <summary>
    /// Processes the HTTP request, routing it through applicable plug-ins and
    /// invoking the matched registered function.
    /// </summary>
    public abstract partial class RequestProcessor
    {
        private static readonly Task<IResponseData> EmptyResponse = Task.FromResult<IResponseData>(null);

        private static readonly MatchResult NoMatch = new MatchResult(
            typeof(RequestProcessor).GetMethod(nameof(OverrideMethodAdapter), BindingFlags.NonPublic | BindingFlags.Static),
            new Dictionary<string, object>());

        private readonly IContentConverterFactory converterFactory;
        private readonly IRouteMapper mapper;
        private readonly MatchResult notFound;
        private readonly IResponseStatusGenerator responseGenerator;
        private readonly IServiceLocator serviceLocator;
        private readonly BlockStreamPool streamPool = new BlockStreamPool();

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestProcessor"/> class.
        /// </summary>
        /// <param name="bootstrapper">Contains application settings.</param>
        protected RequestProcessor(Bootstrapper bootstrapper)
        {
            Check.IsNotNull(bootstrapper, nameof(bootstrapper));

            this.serviceLocator = bootstrapper.ServiceLocator;
            this.mapper = bootstrapper.RouteMapper;

            this.converterFactory = this.serviceLocator.GetContentConverterFactory();
            this.responseGenerator = this.serviceLocator.GetResponseStatusGenerator();
            this.notFound = new MatchResult(this.responseGenerator.NotFoundAsync);

            this.PrimeConverterFactory();
        }

        // NOTE: The methods here should just be protected, however, they've
        //       been made internal as well to allow unit testing.

        /// <summary>
        /// Processes a request and generates a response.
        /// </summary>
        /// <param name="match">
        /// The result of calling <see cref="Match(string, string, ILookup{string, string})"/>.
        /// </param>
        /// <param name="request">Used to create the request data to process.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected internal async Task HandleRequestAsync(MatchResult match, Func<MatchResult, IRequestData> request)
        {
            IRequestData requestData = null;
            IResponseData response;

            try
            {
                requestData = request(match.IsOverride ? NoMatch : match);
                IContentConverter converter = this.GetConverter(requestData);
                if (converter == null)
                {
                    response = await this.responseGenerator.NotAcceptableAsync(requestData).ConfigureAwait(false);
                }
                else
                {
                    if (match.IsOverride)
                    {
                        response = await match.Override(requestData, converter).ConfigureAwait(false);
                    }
                    else
                    {
                        response = await this.ProcessRequestAsync(requestData, converter).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                response = await this.GetErrorResponseAsync(requestData, ex);
            }

            await this.WriteResponseAsync(requestData, response).ConfigureAwait(false);
        }

        /// <summary>
        /// Invokes the registered handler and converts the response.
        /// </summary>
        /// <param name="request">The request data to process.</param>
        /// <param name="converter">
        /// Allows the conversion to the requested content type.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The value of the
        /// <c>TResult</c> parameter contains the response to send.
        /// </returns>
        protected internal virtual async Task<IResponseData> InvokeHandlerAsync(IRequestData request, IContentConverter converter)
        {
            RouteMethod method = this.mapper.GetAdapter(request.Handler);
            if (method == null)
            {
                throw new InvalidOperationException("Request data contains an invalid method.");
            }

            object result = await method(request.Parameters).ConfigureAwait(false);
            if (result == NoContent.Value)
            {
                return await this.responseGenerator.NoContentAsync(request, converter).ConfigureAwait(false);
            }
            else if (result == null)
            {
                return await this.responseGenerator.NotFoundAsync(request, converter).ConfigureAwait(false);
            }
            else
            {
                return this.SerializeResponse(converter, result);
            }
        }

        /// <summary>
        /// Matches the request information to a handler.
        /// </summary>
        /// <param name="verb">The HTTP verb.</param>
        /// <param name="path">The URL path.</param>
        /// <param name="query">Contains the query parameters.</param>
        /// <returns>
        /// An object containing the result of the match.
        /// </returns>
        protected internal MatchResult Match(string verb, string path, ILookup<string, string> query)
        {
            OverrideMethod direct = this.mapper.FindOverride(verb, path);
            if (direct != null)
            {
                return new MatchResult(direct);
            }

            MethodInfo method = this.mapper.Match(
                verb,
                path,
                query,
                out IReadOnlyDictionary<string, object> parameters);

            if (method == null)
            {
                return this.notFound;
            }
            else
            {
                return new MatchResult(method, parameters);
            }
        }

        /// <summary>
        /// Called after the request has been processed but before it is sent
        /// back to the originator.
        /// </summary>
        /// <param name="request">The request data.</param>
        /// <param name="response">The generator response data.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The value of the
        /// <c>TResult</c> parameter contains the response to send.
        /// </returns>
        protected internal virtual async Task<IResponseData> OnAfterRequestAsync(IRequestData request, IResponseData response)
        {
            IPostRequestPlugin[] plugins = this.serviceLocator.GetAfterRequestPlugins();
            Array.Sort(plugins, (a, b) => a.Order.CompareTo(b.Order));

            for (int i = 0; i < plugins.Length; i++)
            {
                response = await plugins[i].ProcessAsync(request, response).ConfigureAwait(false);
            }

            return response;
        }

        /// <summary>
        /// Called before the request is processed and allow the early reply
        /// if the returned value is not null.
        /// </summary>
        /// <param name="request">The request data to process.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The value of the
        /// <c>TResult</c> parameter may contain the response to send. If this
        /// is null then the request is allowed to be processed further.
        /// </returns>
        /// <remarks>
        /// Return a task with a null result to allow the request to be
        /// processed in the normal way.
        /// </remarks>
        protected internal virtual async Task<IResponseData> OnBeforeRequestAsync(IRequestData request)
        {
            IPreRequestPlugin[] plugins = this.serviceLocator.GetBeforeRequestPlugins();
            Array.Sort(plugins, (a, b) => a.Order.CompareTo(b.Order));

            for (int i = 0; i < plugins.Length; i++)
            {
                IResponseData response = await plugins[i].ProcessAsync(request).ConfigureAwait(false);
                if (response != null)
                {
                    return response;
                }
            }

            return null;
        }

        /// <summary>
        /// Called when an error occurs during processing of the request.
        /// </summary>
        /// <param name="request">The request data.</param>
        /// <param name="exception">The generated exception.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The value of the
        /// <c>TResult</c> parameter contains the response to send.
        /// </returns>
        protected internal virtual Task<IResponseData> OnErrorAsync(IRequestData request, Exception exception)
        {
            IErrorHandlerPlugin[] plugins = this.serviceLocator.GetErrorHandlers();
            Array.Sort(plugins, (a, b) => a.Order.CompareTo(b.Order));

            for (int i = 0; i < plugins.Length; i++)
            {
                if (plugins[i].CanHandle(exception))
                {
                    return plugins[i].ProcessAsync(request, exception);
                }
            }

            return EmptyResponse;
        }

        /// <summary>
        /// Called to write the response to the originator of the request.
        /// </summary>
        /// <param name="request">The request data.</param>
        /// <param name="response">The response data to send.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected internal abstract Task WriteResponseAsync(IRequestData request, IResponseData response);

        private static Task OverrideMethodAdapter()
        {
            throw new InvalidOperationException(
                "The request matched an override method and, therefore, this method MUST not be called.");
        }

        private IContentConverter GetConverter(IRequestData request)
        {
            request.Headers.TryGetValue("Accept", out string accept);
            return this.converterFactory.GetConverter(accept);
        }

        private async Task<IResponseData> GetErrorResponseAsync(IRequestData request, Exception exception)
        {
            IResponseData response = null;
            try
            {
                response = await this.OnErrorAsync(request, exception).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                TraceSources.Routing.TraceError(
                    "An exception occurred handling the request: {0}:{1}",
                    ex.GetType().Name,
                    ex.Message);

                try
                {
                    response = await this.responseGenerator.InternalErrorAsync(ex).ConfigureAwait(false);
                }
                catch (Exception inner)
                {
                    TraceSources.Routing.TraceError(
                        "An exception occurred handling an exception: {0}:{1}",
                        inner.GetType().Name,
                        inner.Message);
                }
            }

            return response ?? ResponseGenerator.InternalError;
        }

        private void PrimeConverterFactory()
        {
            TraceSources.Routing.TraceInformation(
                "Priming converter factory with known return types.");

            bool ReturnsGenericTask(MethodInfo method)
            {
                TypeInfo returnType = method.ReturnType.GetTypeInfo();
                return returnType.IsGenericType &&
                      (returnType.GetGenericTypeDefinition() == typeof(Task<>));
            }

            IEnumerable<MethodInfo> methods =
                this.mapper.GetKnownMethods()
                    .Distinct()
                    .Where(ReturnsGenericTask);

            foreach (MethodInfo method in methods)
            {
                this.converterFactory.PrimeConverters(
                    method.ReturnType.GetGenericArguments()[0]);
            }
        }

        private async Task<IResponseData> ProcessRequestAsync(IRequestData request, IContentConverter converter)
        {
            IResponseData response = await this.OnBeforeRequestAsync(request).ConfigureAwait(false);
            if (response == null)
            {
                response = await this.InvokeHandlerAsync(request, converter).ConfigureAwait(false);
                response = await this.OnAfterRequestAsync(request, response).ConfigureAwait(false);
            }

            return response;
        }

        private ResponseData SerializeResponse(IContentConverter converter, object value)
        {
            Func<Stream, Task> convert = async dest =>
            {
                using (Stream memory = this.streamPool.GetStream())
                {
                    converter.WriteTo(memory, value);
                    memory.Position = 0;
                    await memory.CopyToAsync(dest).ConfigureAwait(false);
                }
            };

            return new ResponseData(
                converter.ContentType,
                (int)HttpStatusCode.OK,
                convert);
        }
    }
}
