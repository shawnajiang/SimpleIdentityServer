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

using Newtonsoft.Json;
using SimpleIdentityServer.Uma.Core.Errors;
using SimpleIdentityServer.Uma.Core.Exceptions;
using SimpleIdentityServer.Uma.Core.Models;
using SimpleIdentityServer.Uma.Core.Parameters;
using SimpleIdentityServer.Uma.Core.Repositories;
using SimpleIdentityServer.Uma.Core.Validators;
using SimpleIdentityServer.Uma.Logging;
using System;
using System.Threading.Tasks;

namespace SimpleIdentityServer.Uma.Core.Api.ResourceSetController.Actions
{
    internal interface IUpdateResourceSetAction
    {
        Task<bool> Execute(UpdateResourceSetParameter udpateResourceSetParameter);
    }

    internal class UpdateResourceSetAction : IUpdateResourceSetAction
    {
        private readonly IResourceSetRepository _resourceSetRepository;
        private readonly IResourceSetParameterValidator _resourceSetParameterValidator;
        private readonly IUmaServerEventSource _umaServerEventSource;

        public UpdateResourceSetAction(
            IResourceSetRepository resourceSetRepository,
            IResourceSetParameterValidator resourceSetParameterValidator,
            IUmaServerEventSource umaServerEventSource)
        {
            _resourceSetRepository = resourceSetRepository;
            _resourceSetParameterValidator = resourceSetParameterValidator;
            _umaServerEventSource = umaServerEventSource;
        }

        public async Task<bool> Execute(UpdateResourceSetParameter udpateResourceSetParameter)
        {
            if (udpateResourceSetParameter == null)
            {
                throw new ArgumentNullException(nameof(udpateResourceSetParameter));
            }

            var json = JsonConvert.SerializeObject(udpateResourceSetParameter);
            _umaServerEventSource.StartToUpdateResourceSet(json);
            var resourceSet = new ResourceSet
            {
                Id = udpateResourceSetParameter.Id,
                Name = udpateResourceSetParameter.Name,
                Uri = udpateResourceSetParameter.Uri,
                Type = udpateResourceSetParameter.Type,
                Scopes = udpateResourceSetParameter.Scopes,
                IconUri = udpateResourceSetParameter.IconUri
            };

            if (string.IsNullOrWhiteSpace(udpateResourceSetParameter.Id))
            {
                throw new BaseUmaException(ErrorCodes.InvalidRequestCode, string.Format(ErrorDescriptions.TheParameterNeedsToBeSpecified, "id"));
            }
            _resourceSetParameterValidator.CheckResourceSetParameter(resourceSet);
            if (await _resourceSetRepository.Get(udpateResourceSetParameter.Id) == null)
            {
                return false;
            }

            if (!await _resourceSetRepository.Update(resourceSet))
            {
                throw new BaseUmaException(ErrorCodes.InternalError, string.Format(ErrorDescriptions.TheResourceSetCannotBeUpdated, resourceSet.Id));
            }

            _umaServerEventSource.FinishToUpdateResourceSet(json);
            return true;
        }
    }
}
