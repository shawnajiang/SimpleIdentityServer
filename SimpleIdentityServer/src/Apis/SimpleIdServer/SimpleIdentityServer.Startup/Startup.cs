﻿#region copyright
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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleIdentityServer.Configuration.Client;
using SimpleIdentityServer.Host;
using SimpleIdentityServer.Host.Services;
using System.Collections.Generic;
using WebApiContrib.Core.Storage;
using WebApiContrib.Core.Storage.InMemory;

namespace SimpleIdentityServer.Startup
{
    public class Startup
    {
        private const string SQLSERVER_NAME = "SQLSERVER";
        private const string SQLITE_NAME = "SQLITE";
        private const string POSTGRE_NAME = "POSTGRE";
        private const string REDIS_NAME = "REDIS";
        private const string INMEMORY_NAME = "INMEMORY";

        private IdentityServerOptions _options;
        public IConfigurationRoot Configuration { get; set; }

        public Startup(IHostingEnvironment env)
        {
            // Load all the configuration information from the "json" file & the environment variables.
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
            var twoFactorServiceStore = new TwoFactorServiceStore();
            var factory = new SimpleIdServerConfigurationClientFactory();
            twoFactorServiceStore.Add(new DefaultTwilioSmsService(factory, Configuration["ConfigurationEdp:Url"]));
            twoFactorServiceStore.Add(new DefaultEmailService(factory, Configuration["ConfigurationEdp:Url"]));
            _options = new IdentityServerOptions
            {
                IsDeveloperModeEnabled = false,
                DataSource = new DataSourceOptions
                {
                    IsOpenIdDataMigrated = true,
                    IsEvtStoreDataMigrated = true,
                },
                Logging = new LoggingOptions
                {
                    ElasticsearchOptions = new ElasticsearchOptions(),
                    FileLogOptions = new FileLogOptions()
                },
                Authenticate = new AuthenticateOptions
                {
                    CookieName = Constants.CookieName
                },
                Scim = new ScimOptions
                {
                    IsEnabled = true,
                    EndPoint = "http://localhost:5555/"
                },
                TwoFactorServiceStore = twoFactorServiceStore
            };

            var storeType = Configuration["Store:Database"];
            var openIdType = Configuration["Db:OpenIdType"];
            var evtStoreType = Configuration["Db:EvtStoreType"];
            if (string.Equals(openIdType, SQLSERVER_NAME, System.StringComparison.CurrentCultureIgnoreCase))
            {
                _options.DataSource.OpenIdDataSourceType = DataSourceTypes.SqlServer;
                _options.DataSource.OpenIdConnectionString = Configuration["Db:OpenIdConnectionString"];
            }
            else if (string.Equals(openIdType, SQLITE_NAME, System.StringComparison.CurrentCultureIgnoreCase))
            {
                _options.DataSource.OpenIdDataSourceType = DataSourceTypes.SqlLite;
                _options.DataSource.OpenIdConnectionString = Configuration["Db:OpenIdConnectionString"];
            }
            else if (string.Equals(openIdType, POSTGRE_NAME, System.StringComparison.CurrentCultureIgnoreCase))
            {
                _options.DataSource.OpenIdDataSourceType = DataSourceTypes.Postgre;
                _options.DataSource.OpenIdConnectionString = Configuration["Db:OpenIdConnectionString"];
            }
            else
            {
                _options.DataSource.OpenIdDataSourceType = DataSourceTypes.InMemory;
            }

            if (string.Equals(evtStoreType, SQLSERVER_NAME, System.StringComparison.CurrentCultureIgnoreCase))
            {
                _options.DataSource.EvtStoreDataSourceType = DataSourceTypes.SqlServer;
                _options.DataSource.EvtStoreConnectionString = Configuration["Db:EvtStoreConnectionString"];
            }
            else if (string.Equals(evtStoreType, SQLITE_NAME, System.StringComparison.CurrentCultureIgnoreCase))
            {
                _options.DataSource.EvtStoreDataSourceType = DataSourceTypes.SqlLite;
                _options.DataSource.EvtStoreConnectionString = Configuration["Db:EvtStoreConnectionString"];
            }
            else if (string.Equals(evtStoreType, POSTGRE_NAME, System.StringComparison.CurrentCultureIgnoreCase))
            {
                _options.DataSource.EvtStoreDataSourceType = DataSourceTypes.Postgre;
                _options.DataSource.EvtStoreConnectionString = Configuration["Db:EvtStoreConnectionString"];
            }
            else
            {
                _options.DataSource.EvtStoreDataSourceType = DataSourceTypes.InMemory;
            }

            if (string.Equals(storeType, REDIS_NAME, System.StringComparison.CurrentCultureIgnoreCase))
            {
                _options.Storage.Type = CachingTypes.Redis;
                _options.Storage.ConnectionString = Configuration["Store:ConnectionString"];
                _options.Storage.InstanceName = Configuration["Store:InstanceName"];
                var port = Configuration["Store:Port"];
                int portNumber;
                if (!int.TryParse(port, out portNumber))
                {
                    portNumber = 6379;
                }

                _options.Storage.Port = portNumber;
            }
            else
            {
                _options.Storage.Type = CachingTypes.InMemory;
            }

            bool isLogFileEnabled,
                isElasticSearchEnabled;
            if (bool.TryParse(Configuration["Log:File:Enabled"], out isLogFileEnabled))
            {
                _options.Logging.FileLogOptions.IsEnabled = isLogFileEnabled;
                if (isLogFileEnabled)
                {
                    _options.Logging.FileLogOptions.PathFormat = Configuration["Log:File:PathFormat"];
                }
            }
            
            if (bool.TryParse(Configuration["Log:Elasticsearch:Enabled"], out isElasticSearchEnabled))
            {
                _options.Logging.ElasticsearchOptions.IsEnabled = isElasticSearchEnabled;
                if (isElasticSearchEnabled)
                {
                    _options.Logging.ElasticsearchOptions.Url = Configuration["Log:Elasticsearch:Url"];
                }
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var cachingDatabase = Configuration["Caching:Database"];
            if (string.IsNullOrWhiteSpace(cachingDatabase))
            {
                cachingDatabase = INMEMORY_NAME;
            }

            // 1. Configure the caching
            if (string.Equals(cachingDatabase, REDIS_NAME, System.StringComparison.CurrentCultureIgnoreCase))
            {
                services.AddStorage(opt => opt.UseRedis(o =>
                {
                    o.Configuration = Configuration["Caching:ConnectionString"];
                    o.InstanceName = Configuration["Caching:InstanceName"];
                }));
            }
            else if (string.Equals(cachingDatabase, INMEMORY_NAME, System.StringComparison.CurrentCultureIgnoreCase))
            {
                services.AddStorage(opt => opt.UseInMemory());
            }

            // 2. Add the dependencies needed to enable CORS
            services.AddCors(options => options.AddPolicy("AllowAll", p => p.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()));

            // 3. Configure the rate limitation

            // 4. Configure Simple identity server
            services.AddSimpleIdentityServer(_options);
            // 5. Enable logging
            services.AddLogging();
            services.AddAuthentication(Constants.CookieName)
                .AddCookie(Constants.CookieName, opts =>
                {
                    opts.LoginPath = "/Authenticate";
                });
            // 6. Configure MVC
            services.AddMvc();
            // 7. Add authentication dependencies & configure it.
            services.AddAuthorization(opts =>
            {
                opts.AddPolicy("Connected", policy => policy.RequireAssertion((ctx) => {
                    return ctx.User.Identity != null && ctx.User.Identity.AuthenticationType == Constants.CookieName;
                }));
            });
        }

        public void Configure(IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory)
        {
            //1 . Enable CORS.
            app.UseCors("AllowAll");
            app.UseAuthentication();
            // 2. Use static files.
            app.UseStaticFiles();
            // 3. Redirect error to custom pages.
            app.UseStatusCodePagesWithRedirects("~/Error/{0}");
            // 4. Enable cookie authentication.
            // 5. Enable SimpleIdentityServer
            app.UseSimpleIdentityServer(_options, loggerFactory);
            // 6. Configure ASP.NET MVC
            app.UseMvc(routes =>
            {
                routes.MapRoute("Error401Route",
                    Host.Constants.EndPoints.Get401,
                    new
                    {
                        controller = "Error",
                        action = "Get401"
                    });
                routes.MapRoute("Error404Route",
                    Host.Constants.EndPoints.Get404,
                    new
                    {
                        controller = "Error",
                        action = "Get404"
                    });
                routes.MapRoute("Error500Route",
                    Host.Constants.EndPoints.Get500,
                    new
                    {
                        controller = "Error",
                        action = "Get500"
                    });
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
