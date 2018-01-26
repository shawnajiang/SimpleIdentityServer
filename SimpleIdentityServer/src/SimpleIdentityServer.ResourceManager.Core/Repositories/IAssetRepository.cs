﻿using SimpleIdentityServer.ResourceManager.Core.Models;
using SimpleIdentityServer.ResourceManager.Core.Parameters;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleIdentityServer.ResourceManager.Core.Repositories
{
    public interface IAssetRepository
    {
        Task<IEnumerable<AssetAggregate>> Search(SearchAssetsParameter parameter);
        Task<IEnumerable<AssetAggregate>> GetAllParents(string hash);
        Task<IEnumerable<AssetAggregate>> GetAllChildren(string hash);
        Task<AssetAggregate> Get(string hash);
        Task<bool> Add(IEnumerable<AssetAggregate> asset);
        Task<bool> Remove(IEnumerable<string> hashLst);
        Task<bool> Update(AssetAggregate asset);
    }
}