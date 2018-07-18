// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Host.IO;
    using Crest.Host.Logging;

    /// <summary>
    /// Injects the application JSON settings for the properties in a
    /// configuration class.
    /// </summary>
    internal sealed partial class JsonConfigurationProvider : IConfigurationProvider, IDisposable
    {
        private const string GlobalSettingsFile = "appsettings.json";
        private static readonly ILog Logger = Log.For<JsonConfigurationProvider>();

        private readonly string environmentSettingsFile;
        private readonly JsonClassGenerator generator;

        private readonly Dictionary<Type, TypeInitializer> initializers =
            new Dictionary<Type, TypeInitializer>();

        private readonly FileReader reader;
        private readonly FileWriteWatcher watcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonConfigurationProvider"/> class.
        /// </summary>
        /// <param name="reader">Used to read the contents of files.</param>
        /// <param name="watcher">Used to listen to file system events.</param>
        /// <param name="generator">Used to generate the object initializer.</param>
        public JsonConfigurationProvider(
            FileReader reader,
            FileWriteWatcher watcher,
            JsonClassGenerator generator)
        {
            this.environmentSettingsFile = GetEnvironmentSettingsFileName();
            this.generator = generator;
            this.reader = reader;
            this.watcher = watcher;

            this.watcher.WatchFile(this.environmentSettingsFile, this.UpdateEnvironmentSettingsAsync);
            this.watcher.WatchFile(GlobalSettingsFile, this.UpdateGlobalSettingsAsync);
        }

        /// <inheritdoc />
        public int Order => 200;

        /// <summary>
        /// Releases the resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            this.watcher.Dispose();
        }

        /// <inheritdoc />
        public async Task InitializeAsync(IEnumerable<Type> knownTypes)
        {
            foreach (Type type in knownTypes)
            {
                this.initializers.Add(type, new TypeInitializer(type));
            }

            await Task.WhenAll(this.UpdateEnvironmentSettingsAsync(), this.UpdateGlobalSettingsAsync())
                .ConfigureAwait(false);

            // Now that we've loaded the files, keep an eye on them
            this.watcher.StartMonitoring();
        }

        /// <inheritdoc />
        public void Inject(object instance)
        {
            Type type = instance.GetType();
            if (this.initializers.TryGetValue(type, out TypeInitializer initializer))
            {
                // Apply the global settings first so that any environment
                // specific setting overwrite them
                initializer.GlobalSettings(instance);
                initializer.EnvironmentSettings(instance);
            }
        }

        private static string GetEnvironmentSettingsFileName()
        {
            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (string.IsNullOrEmpty(environment))
            {
                // The environment defaults to production if it's not specified:
                // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments
                Logger.WarnFormat("No environment detected (ASPNETCORE_ENVIRONMENT), assuming production");
                return "appsettings.Production.json";
            }
            else
            {
                Logger.InfoFormat("Environment detected as '{environment}'", environment);
                return "appsettings." + environment + ".json";
            }
        }

        private TypeInitializer FindInitializer(string typeName)
        {
            IEnumerable<Type> matchingTypes =
                this.initializers.Keys
                    .Where(t => t.FullName.EndsWith(typeName, StringComparison.OrdinalIgnoreCase));

            // We want to try to find only a single match and warn when it's
            // ambiguous so the user can fully qualify the name
            using (IEnumerator<Type> e = matchingTypes.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    Logger.WarnFormat(
                        "No configuration class found for configuration setting '{key}'",
                        typeName);
                    return null;
                }

                Type type = e.Current;
                if (e.MoveNext())
                {
                    Logger.WarnFormat(
                        "Configuration setting '{key}' matches multiple types - try fully qualifying the type with its namespace",
                        typeName);
                    return null;
                }

                return this.initializers[type];
            }
        }

        private Task UpdateEnvironmentSettingsAsync()
        {
            return this.UpdateTypeInitializerAsync(
                this.environmentSettingsFile,
                (ti, init) => ti.EnvironmentSettings = init);
        }

        private Task UpdateGlobalSettingsAsync()
        {
            return this.UpdateTypeInitializerAsync(
                GlobalSettingsFile,
                (ti, init) => ti.GlobalSettings = init);
        }

        private void UpdateTypeInitializer(
            string typeName,
            string json,
            Action<TypeInitializer, Action<object>> updateInitializer)
        {
            TypeInitializer initializer = this.FindInitializer(typeName);
            if (initializer != null)
            {
                Action<object> action = this.generator.CreatePopulateMethod(
                    initializer.Type,
                    json);

                updateInitializer(initializer, action);
            }
        }

        private async Task UpdateTypeInitializerAsync(
            string filename,
            Action<TypeInitializer, Action<object>> updateInitializer)
        {
            try
            {
                byte[] json = await this.reader.ReadAllBytesAsync(filename).ConfigureAwait(false);
                var parser = new JsonObjectParser(json);
                foreach (KeyValuePair<string, string> pair in parser.GetPairs())
                {
                    this.UpdateTypeInitializer(pair.Key, pair.Value, updateInitializer);
                }
            }
            catch (FormatException ex)
            {
                Logger.ErrorException("'{filename}' contains invalid JSON", ex, filename);
            }
            catch (IOException ex)
            {
                Logger.ErrorException("Unable to read '{filename}'", ex, filename);
            }
        }
    }
}
