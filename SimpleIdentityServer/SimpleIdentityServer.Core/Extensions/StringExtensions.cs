﻿using System;
using System.Linq;
using System.Text;
using SimpleIdentityServer.Core.Jwt;

namespace SimpleIdentityServer.Core.Extensions
{
    public static class StringExtensions
    {
        public static string Base64Encode(this string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(this string base64EncodedData)
        {
            base64EncodedData = base64EncodedData.Replace("-", "+");
            base64EncodedData = base64EncodedData.Replace("_", "/");
            switch (base64EncodedData.Length % 4)
            {
                case 0:
                    break;
                case 2:
                    base64EncodedData += "==";
                    break;
                case 3:
                    base64EncodedData += "=";
                    break;
            }
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        /// <summary>
        /// Convert a string into JWS algorithm.
        /// </summary>
        /// <param name="alg">String to be converted</param>
        /// <returns>JWS algorithm</returns>
        public static JwsAlg ToJwsAlg(this string alg)
        {
            var algName = Enum.GetNames(typeof (JwsAlg))
                .SingleOrDefault(a => a.ToLowerInvariant() == alg.ToLowerInvariant());
            if (algName == null)
            {
                return JwsAlg.RS256;
            }

            return (JwsAlg)Enum.Parse(typeof (JwsAlg), algName);
        }
    }
}