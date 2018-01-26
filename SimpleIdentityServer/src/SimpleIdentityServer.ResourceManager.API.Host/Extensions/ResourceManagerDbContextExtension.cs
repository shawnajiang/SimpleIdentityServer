﻿using SimpleIdentityServer.ResourceManager.API.Host.Helpers;
using SimpleIdentityServer.ResourceManager.EF;
using SimpleIdentityServer.ResourceManager.EF.Models;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace SimpleIdentityServer.ResourceManager.API.Host.Extensions
{
    internal static class ResourceManagerDbContextExtension
    {
        public static void EnsureSeedData(this ResourceManagerDbContext resourceManagerContext)
        {
            if (resourceManagerContext == null)
            {
                throw new ArgumentNullException(nameof(resourceManagerContext));
            }

            AddAssets(resourceManagerContext);
            resourceManagerContext.SaveChanges();
        }

        private static void AddAssets(ResourceManagerDbContext context)
        {
            if (!context.Assets.Any())
            {
                context.Assets.AddRange(new[]
                {
                   new Asset
                   {
                       Hash = HashHelper.GetHash("Root"),
                       Name = "Root",
                       Path = "Root",
                       IsLocked = false,
                       CanRead = true,
                       CanWrite = true,
                       CreateDateTime = DateTime.UtcNow,
                       IsDefaultWorkingDirectory = true,
                       MimeType = Constants.MimeNames.Directory
                   },
                   new Asset
                   {
                       Hash = HashHelper.GetHash("Second root"),
                       Name = "Second root",
                       Path = "Second root",
                       IsLocked = true,
                       CanRead = true,
                       CanWrite = false,
                       CreateDateTime = DateTime.UtcNow,
                       MimeType = Constants.MimeNames.Directory
                   },
                   new Asset
                   {
                       Hash = HashHelper.GetHash("Root/Sub"),
                       ResourceParentHash = HashHelper.GetHash("Root"),
                       Name = "Sub",
                       Path = "Root/Sub",
                       IsLocked = false,
                       CanRead = true,
                       CanWrite = true,
                       CreateDateTime = DateTime.UtcNow,
                       MimeType = Constants.MimeNames.Directory,
                       ResourceId = "80596bfa-e2bb-4001-bb89-b95e413757ea",
                       AuthorizationPolicyId = "986ea7da-d911-48b8-adfa-124b3827246a"
                   },
                   new Asset
                   {
                       Hash = HashHelper.GetHash("Root/another directory"),
                       ResourceParentHash = HashHelper.GetHash("Root"),
                       Name = "another directory",
                       Path = "Root/another directory",
                       IsLocked = false,
                       CanRead = true,
                       CanWrite = true,
                       CreateDateTime = DateTime.UtcNow,
                       MimeType = Constants.MimeNames.Directory
                   }
                });
            }
        }
    }
}