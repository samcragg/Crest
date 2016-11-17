// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Host.Engine;

    /// <summary>
    /// Processes the HTTP request, routing it through applicable plug-ins and
    /// invoking the matched registered function.
    /// </summary>
    public abstract partial class RequestProcessor
    {
        private static readonly Task<IResponseData> EmptyResponse = Task.FromResult<IResponseData>(null);
        private readonly Bootstrapper bootstrapper;
        private readonly IRouteMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestProcessor"/> class.
        /// </summary>
        /// <param name="bootstrapper">Contains application settings.</param>
        protected RequestProcessor(Bootstrapper bootstrapper)
        {
            Check.IsNotNull(bootstrapper, nameof(bootstrapper));

            this.bootstrapper = bootstrapper;
            this.mapper = bootstrapper.GetService<IRouteMapper>();
        }

        // NOTE: The methods here should just be protected, however, they've
        //       been made internal as well to allow unit testing.

        /// <summary>
        /// Processes a request and generates a response.
        /// </summary>
        /// <param name="request">The request data to process.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected internal async Task HandleRequest(IRequestData request)
        {
            IResponseData response = null;
            try
            {
                response = await this.OnBeforeRequest(request).ConfigureAwait(false);
                if (response == null)
                {
                    response = await this.InvokeHandler(request).ConfigureAwait(false);
                    response = await this.OnAfterRequest(request, response).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                // TODO: If the response is null use an internal error one...
                response = await this.OnError(request, ex).ConfigureAwait(false);
            }

            await this.WriteResponse(request, response).ConfigureAwait(false);
        }

        /// <summary>
        /// Invokes the registered handler and converts the response.
        /// </summary>
        /// <param name="request">The request data to process.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The value of the
        /// <c>TResult</c> parameter contains the response to send.
        /// </returns>
        protected internal virtual async Task<IResponseData> InvokeHandler(IRequestData request)
        {
            RouteMethod method = this.mapper.GetAdapter(request.Handler);
            if (method == null)
            {
                throw new InvalidOperationException("Request data contains an invalid method.");
            }

            // Check if result is NoContent.Value
            object result = await method(request.Parameters).ConfigureAwait(false);
            if (result == NoContent.Value)
            {
                return ResponseData.NoContent;
            }
            else
            {
                // TODO: Plug in the serialization logic here...
                return new ResponseData(string.Empty, (int)HttpStatusCode.OK);
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
            IReadOnlyDictionary<string, object> parameters;
            MethodInfo method = this.mapper.Match(verb, path, query, out parameters);
            if (method == null)
            {
                return default(MatchResult);
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
        protected internal virtual async Task<IResponseData> OnAfterRequest(IRequestData request, IResponseData response)
        {
            IPostRequestPlugin[] plugins = this.bootstrapper.GetAfterRequestPlugins();
            Array.Sort(plugins, (a, b) => a.Order.CompareTo(b.Order));

            for (int i = 0; i < plugins.Length; i++)
            {
                response = await plugins[i].Process(request, response).ConfigureAwait(false);
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
        protected internal virtual async Task<IResponseData> OnBeforeRequest(IRequestData request)
        {
            IPreRequestPlugin[] plugins = this.bootstrapper.GetBeforeRequestPlugins();
            Array.Sort(plugins, (a, b) => a.Order.CompareTo(b.Order));

            for (int i = 0; i < plugins.Length; i++)
            {
                IResponseData response = await plugins[i].Process(request).ConfigureAwait(false);
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
        protected internal virtual Task<IResponseData> OnError(IRequestData request, Exception exception)
        {
            IErrorHandlerPlugin[] plugins = this.bootstrapper.GetErrorHandlers();
            Array.Sort(plugins, (a, b) => a.Order.CompareTo(b.Order));

            for (int i = 0; i < plugins.Length; i++)
            {
                if (plugins[i].CanHandle(exception))
                {
                    return plugins[i].Process(request, exception);
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
        protected internal abstract Task WriteResponse(IRequestData request, IResponseData response);
    }
}
