// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Engine
{
    using Crest.Host.Conversion;

    /// <summary>
    /// Creates the instances of interfaces required during initialization.
    /// </summary>
    public interface IServiceLocator
    {
        /// <summary>
        /// Gets the registered plugins to call after processing a request.
        /// </summary>
        /// <returns>A sequence of registered plugins.</returns>
        IPostRequestPlugin[] GetAfterRequestPlugins();

        /// <summary>
        /// Gets the registered plugins to call before processing a request.
        /// </summary>
        /// <returns>A sequence of registered plugins.</returns>
        IPreRequestPlugin[] GetBeforeRequestPlugins();

        /// <summary>
        /// Gets the service to use for providing configuration data.
        /// </summary>
        /// <returns>An object based on <see cref="ConfigurationService"/>.</returns>
        ConfigurationService GetConfigurationService();

        /// <summary>
        /// Gets the service to use for providing content converters for requests.
        /// </summary>
        /// <returns>An object implementing <see cref="IContentConverterFactory"/>.</returns>
        IContentConverterFactory GetContentConverterFactory();

        /// <summary>
        /// Gets the service to use for discovering assemblies and types.
        /// </summary>
        /// <returns>An object implementing <see cref="IDiscoveryService"/>.</returns>
        IDiscoveryService GetDiscoveryService();

        /// <summary>
        /// Gets the registered plugins to handle generated exceptions.
        /// </summary>
        /// <returns>A sequence of registered plugins.</returns>
        IErrorHandlerPlugin[] GetErrorHandlers();

        /// <summary>
        /// Gets the service to use for providing the template for generated HTML.
        /// </summary>
        /// <returns>An object implementing <see cref="IHtmlTemplateProvider"/>.</returns>
        IHtmlTemplateProvider GetHtmlTemplateProvider();

        /// <summary>
        /// Gets the service to use to generate responses for various status
        /// codes.
        /// </summary>
        /// <returns>An object implementing <see cref="IResponseStatusGenerator"/>.</returns>
        IResponseStatusGenerator GetResponseStatusGenerator();
    }
}
