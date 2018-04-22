#region copyright
// Copyright 2015 Habart Thierry
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using SimpleIdentityServer.Client;
using SimpleIdentityServer.Core;
using SimpleIdentityServer.Core.Jwt;
using SimpleIdentityServer.Core.Services;
using SimpleIdentityServer.Host.Configuration;
using SimpleIdentityServer.Host.Parsers;
using SimpleIdentityServer.Host.Services;
using SimpleIdentityServer.Logging;
using System;
using System.Collections.Generic;

namespace SimpleIdentityServer.Host
{
    public static class ServiceCollectionExtensions 
    {
        public static IServiceCollection AddOpenIdApi(
            this IServiceCollection serviceCollection,
            Action<IdentityServerOptions> optionsCallback)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (optionsCallback == null)
            {
                throw new ArgumentNullException(nameof(optionsCallback));
            }
            
            var options = new IdentityServerOptions();
            optionsCallback(options);
            serviceCollection.AddOpenIdApi(
                options);
            return serviceCollection;
        }
        
        public static IServiceCollection AddOpenIdApi(
            this IServiceCollection serviceCollection,
            IdentityServerOptions options)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }   

            ConfigureSimpleIdentityServer(
                serviceCollection, 
                options);
            return serviceCollection;
        }

        public static IServiceCollection AddHostIdentityServer(this IServiceCollection serviceCollection, IdentityServerOptions options)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.AuthenticateResourceOwner == null)
            {
                serviceCollection.AddTransient<IAuthenticateResourceOwnerService, DefaultAuthenticateResourceOwerService>();
            }
            else
            {
                serviceCollection.AddTransient(typeof(IAuthenticateResourceOwnerService), options.AuthenticateResourceOwner);
            }

            if (options.ConfigurationService == null)
            {
                serviceCollection.AddTransient<IConfigurationService, DefaultConfigurationService>();
            }
            else
            {
                serviceCollection.AddTransient(typeof(IConfigurationService), options.ConfigurationService);
            }

            if (options.PasswordService == null)
            {
                serviceCollection.AddTransient<IPasswordService, DefaultPasswordService>();
            }
            else
            {
                serviceCollection.AddTransient(typeof(IPasswordService), options.PasswordService);
            }

            var twoFactorServiceStore = new TwoFactorServiceStore();
            if (options.TwoFactorAuthentications != null)
            {
                foreach(var twoFactorAuthentication in options.TwoFactorAuthentications)
                {
                    if (twoFactorAuthentication.TwoFactorAuthType != Core.Models.TwoFactorAuthentications.NONE && twoFactorAuthentication.TwoFactorAuthenticationService != null)
                    {
                        twoFactorServiceStore.Add(twoFactorAuthentication.TwoFactorAuthenticationService);
                    }
                }
            }

            serviceCollection.AddSingleton<ITwoFactorServiceStore>(twoFactorServiceStore);
            serviceCollection
                .AddSingleton(options.Authenticate)
                .AddSingleton(options.Scim)
                .AddTransient<IRedirectInstructionParser, RedirectInstructionParser>()
                .AddTransient<IActionResultParser, ActionResultParser>()
                .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
                .AddSingleton<IActionContextAccessor, ActionContextAccessor>()
                .AddDataProtection();

            return serviceCollection;
        }
   
        public static IServiceCollection AddAuthenticationWebsite(this IServiceCollection services)
        {
            /*
            services.Configure<RazorViewEngineOptions>(options =>
            {
                var embeddedFileProvider = new EmbeddedFileProvider(typeof(BookOfTheMonkViewComponent).GetTypeInfo().Assembly);
                var compositeFileProvider = new CompositeFileProvider(
                    embeddedFileProvider,
                    options.FileProvider);
                options.FileProvider = compositeFileProvider;
            }
            */
            // CONFIGURE AUTHENTICATION + CONSENT + FORM
            return services;
        }

        /// <summary>
        /// Add all the dependencies needed to run Simple Identity Server
        /// </summary>
        /// <param name="services"></param>
        private static void ConfigureSimpleIdentityServer(
            IServiceCollection services,
            IdentityServerOptions options)
        {
            services.AddSimpleIdentityServerCore()
                .AddSimpleIdentityServerJwt()
                .AddHostIdentityServer(options)
                .AddIdServerClient()
                .AddIdServerLogging()
                .AddDataProtection();

            /*
            // Configure SeriLog pipeline
            Func<LogEvent, bool> serilogFilter = (e) =>
            {
                var ctx = e.Properties["SourceContext"];
                var contextValue = ctx.ToString()
                    .TrimStart('"')
                    .TrimEnd('"');
                return contextValue.StartsWith("SimpleIdentityServer") ||
                    e.Level == LogEventLevel.Error ||
                    e.Level == LogEventLevel.Fatal;
            };
            var logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.ColoredConsole();
            if (options.Logging.FileLogOptions != null &&
                options.Logging.FileLogOptions.IsEnabled)
            {
                logger.WriteTo.RollingFile(options.Logging.FileLogOptions.PathFormat);
            }

            if (options.Logging.ElasticsearchOptions != null &&
                options.Logging.ElasticsearchOptions.IsEnabled)
            {
                logger.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(options.Logging.ElasticsearchOptions.Url))
                {
                    AutoRegisterTemplate = true,
                    IndexFormat = "simpleidserver-{0:yyyy.MM.dd}"
                });
            }
        
            var log = logger.Filter.ByIncludingOnly(serilogFilter)
                .CreateLogger();
            Log.Logger = log;
            services.AddLogging();
            services.AddSingleton<ILogger>(log);
            */
        }
    }
}