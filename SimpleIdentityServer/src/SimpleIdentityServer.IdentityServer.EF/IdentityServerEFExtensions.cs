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

using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using SimpleIdentityServer.Core.Repositories;

namespace SimpleIdentityServer.IdentityServer.EF
{
    public static class IdentityServerEFExtensions
    {
        #region Public static methods

        public static IServiceCollection AddSimpleIdentityServerSqlServer(
            this IServiceCollection serviceCollection,
            string connectionString)
        {
            RegisterServices(serviceCollection);
            serviceCollection.AddEntityFramework()
                .AddDbContext<ConfigurationDbContext>(options =>
                    options.UseSqlServer(connectionString));
            serviceCollection.AddEntityFramework()
                .AddDbContext<PersistedGrantDbContext>(options =>
                    options.UseSqlServer(connectionString));
            return serviceCollection;
        }

        #endregion

        #region Private method

        private static void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IScopeRepository, ScopeRepository>();
        }

        #endregion
    }
}