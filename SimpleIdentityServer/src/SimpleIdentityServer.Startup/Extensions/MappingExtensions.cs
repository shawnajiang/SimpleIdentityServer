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

using SimpleIdentityServer.Core.Parameters;
using SimpleIdentityServer.Startup.ViewModels;
using System.Collections.Generic;
using System.Security.Claims;

namespace SimpleIdentityServer.Startup.Extensions
{
    public static class MappingExtensions
    {
        public static UpdateUserParameter ToParameter(this UpdateResourceOwnerViewModel updateResourceOwnerViewModel)
        {
            return new UpdateUserParameter
            {
                Password = updateResourceOwnerViewModel.NewPassword,
                TwoFactorAuthentication = updateResourceOwnerViewModel.TwoAuthenticationFactor,
                Claims = new List<Claim>
                {

                    new Claim(Core.Jwt.Constants.StandardResourceOwnerClaimNames.Name, updateResourceOwnerViewModel.Name),
                    new Claim(Core.Jwt.Constants.StandardResourceOwnerClaimNames.Email, updateResourceOwnerViewModel.Email),
                    new Claim(Core.Jwt.Constants.StandardResourceOwnerClaimNames.PhoneNumber, updateResourceOwnerViewModel.PhoneNumber)
                },
                Login = updateResourceOwnerViewModel.Name
            };
        }

        public static LocalAuthenticationParameter ToParameter(this AuthorizeViewModel viewModel)
        {
            return new LocalAuthenticationParameter
            {
                UserName = viewModel.UserName,
                Password = viewModel.Password
            };
        }

        public static LocalAuthenticationParameter ToParameter(this AuthorizeOpenIdViewModel viewModel)
        {
            return new LocalAuthenticationParameter
            {
                UserName = viewModel.UserName,
                Password = viewModel.Password                
            };
        }
    }
}