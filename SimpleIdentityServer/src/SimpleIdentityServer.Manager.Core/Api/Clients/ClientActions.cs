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

using SimpleIdentityServer.Core.Models;
using System.Collections.Generic;
using SimpleIdentityServer.Manager.Core.Api.Clients.Actions;

namespace SimpleIdentityServer.Manager.Core.Api.Clients
{
    public interface IClientActions
    {
        List<Client> GetClients();

        Client GetClient(string clientId);
    }

    public class ClientActions : IClientActions
    {
        private readonly IGetClientsAction _getClientsAction;

        private readonly IGetClientAction _getClientAction;

        #region Constructor

        public ClientActions(
            IGetClientsAction getClientsAction,
            IGetClientAction getClientAction)
        {
            _getClientsAction = getClientsAction;
            _getClientAction = getClientAction;
        }

        #endregion

        #region Public methods

        public List<Client> GetClients()
        {
            return _getClientsAction.Execute();
        }

        public Client GetClient(string clientId)
        {
            return _getClientAction.Execute(clientId);
        }

        #endregion
    }
}