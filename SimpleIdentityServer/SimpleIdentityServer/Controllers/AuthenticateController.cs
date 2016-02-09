﻿using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using SimpleIdentityServer.Api.DTOs.Request;
using SimpleIdentityServer.Api.Extensions;
using SimpleIdentityServer.Api.ViewModels;
using SimpleIdentityServer.Core.Errors;
using SimpleIdentityServer.Core.Exceptions;
using SimpleIdentityServer.Core.Extensions;
using SimpleIdentityServer.Core.Protector;
using SimpleIdentityServer.Core.Translation;
using SimpleIdentityServer.Core.WebSite.Authenticate;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Web.Mvc;
using SimpleIdentityServer.Logging;
using ActionResult = System.Web.Mvc.ActionResult;

namespace SimpleIdentityServer.Api.Controllers
{
    public class AuthenticateController : Controller
    {
        private readonly IAuthenticateActions _authenticateActions;

        private readonly IProtector _protector;

        private readonly IEncoder _encoder;

        private readonly ITranslationManager _translationManager;

        private readonly ISimpleIdentityServerEventSource _simpleIdentityServerEventSource;

        private const string DefaultLanguage = "en";

        private const string ExternalAuthenticateCookieName = "SimpleIdentityServer-{0}";

        public AuthenticateController(
            IAuthenticateActions authenticateActions,
            IProtector protector,
            IEncoder encoder,
            ITranslationManager translationManager,
            ISimpleIdentityServerEventSource simpleIdentityServerEventSource)
        {
            _authenticateActions = authenticateActions;
            _protector = protector;
            _encoder = encoder;
            _translationManager = translationManager;
            _simpleIdentityServerEventSource = simpleIdentityServerEventSource;
        }

        #region Public methods

        public ActionResult Logout()
        {
            var authenticationManager = this.GetAuthenticationManager();
            authenticationManager.SignOut();
            return RedirectToAction("Index", "Authenticate");
        }

        #region Normal authentication process

        [HttpGet]
        public ActionResult Index()
        {
            var authenticatedUser = this.GetAuthenticatedUser();
            if (authenticatedUser == null ||
                !authenticatedUser.IsAuthenticated())
            {
                TranslateView(DefaultLanguage);
                return View(new AuthorizeViewModel());
            }

            return RedirectToAction("Index", "User");
        }

        [HttpPost]
        public ActionResult LocalLogin(AuthorizeViewModel authorizeViewModel)
        {
            if (authorizeViewModel == null)
            {
                throw new ArgumentNullException("authorizeViewModel");
            }

            if (!ModelState.IsValid)
            {
                TranslateView(DefaultLanguage);
                return View("Index", authorizeViewModel);
            }

            try
            {
                var claims = _authenticateActions.LocalUserAuthentication(authorizeViewModel.ToParameter());
                var authenticationManager = this.GetAuthenticationManager();
                var claimIdentity = new ClaimsIdentity(claims, DefaultAuthenticationTypes.ApplicationCookie);
                authenticationManager.SignIn(
                    new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = DateTime.UtcNow.AddDays(7)
                    },
                    claimIdentity
                    );

                return RedirectToAction("Index", "User");
            }
            catch (Exception exception)
            {
                _simpleIdentityServerEventSource.Failure(exception.Message);
                TranslateView("en");
                ModelState.AddModelError("invalid_credentials", exception.Message);
                return View("Index", authorizeViewModel);
            }
        }

        [HttpPost]
        public ActionResult ExternalLogin(string provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                throw new ArgumentNullException(provider);
            }

            return new ChallengeResult(provider,
                Url.Action("LoginCallback", "Authenticate"));
        }

        public async Task<ActionResult> LoginCallback(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new IdentityServerException(
                    ErrorCodes.UnhandledExceptionCode,
                    string.Format(ErrorDescriptions.AnErrorHasBeenRaisedWhenTryingToAuthenticate, error));
            }

            var context = HttpContext.GetOwinContext();
            var authenticationManager = context.Authentication;
            var loginInformation = await authenticationManager.GetExternalLoginInfoAsync();
            if (loginInformation == null)
            {
                throw new IdentityServerException(ErrorCodes.UnhandledExceptionCode,
                    ErrorDescriptions.TheLoginInformationCannotBeExtracted);
            }
            
            var providerType = loginInformation.Login.LoginProvider;
            var claims = loginInformation.ExternalIdentity.Claims.ToList();
            var openIdClaims = _authenticateActions.ExternalUserAuthentication(claims, providerType);
            var claimIdentity = new ClaimsIdentity(openIdClaims, DefaultAuthenticationTypes.ApplicationCookie);
            authenticationManager.SignIn(
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTime.UtcNow.AddDays(7)
                },
                claimIdentity
            );

            return RedirectToAction("Index", "User");
        }

        #endregion

        #region Authentication process which follows OPENID

        [HttpGet]
        public ActionResult OpenId(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentNullException("code");
            }

            var authenticatedUser = this.GetAuthenticatedUser(); 
            var decodedCode = _encoder.Decode(code);
            var request = _protector.Decrypt<AuthorizationRequest>(decodedCode);

            var actionResult = _authenticateActions.AuthenticateResourceOwnerOpenId(
                request.ToParameter(),
                authenticatedUser,
                code);
            var result = this.CreateRedirectionFromActionResult(actionResult,
                request);
            if (result != null)
            {
                return result;
            }

            TranslateView(request.ui_locales);
            var viewModel = new AuthorizeOpenIdViewModel
            {
                Code = code
            };

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult LocalLoginOpenId(AuthorizeOpenIdViewModel authorizeOpenId)
        {
            if (authorizeOpenId == null)
            {
                throw new ArgumentNullException("authorizeOpenId");
            }

            if (string.IsNullOrWhiteSpace(authorizeOpenId.Code))
            {
                throw new ArgumentNullException("authorizeOpenId.Code");
            }

            var uiLocales = DefaultLanguage;
            try
            {
                var code = _encoder.Decode(authorizeOpenId.Code);
                var request = _protector.Decrypt<AuthorizationRequest>(code);
                uiLocales = string.IsNullOrWhiteSpace(request.ui_locales) ? DefaultLanguage : request.ui_locales;
                if (!ModelState.IsValid)
                {
                    TranslateView(uiLocales);
                    return View("OpenId", authorizeOpenId);
                }

                var authenticationManager = this.GetAuthenticationManager();
                var claims = new List<Claim>();
                var actionResult = _authenticateActions.LocalOpenIdUserAuthentication(authorizeOpenId.ToParameter(),
                    request.ToParameter(),
                    authorizeOpenId.Code,
                    out claims);

                var claimIdentity = new ClaimsIdentity(claims, DefaultAuthenticationTypes.ApplicationCookie);
                authenticationManager.SignIn(
                    new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = DateTime.UtcNow.AddDays(7)
                    },
                    claimIdentity
                );

                var result = this.CreateRedirectionFromActionResult(actionResult,
                    request);
                if (result != null)
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                _simpleIdentityServerEventSource.Failure(ex.Message);
                ModelState.AddModelError("invalid_credentials", ex.Message);
            }

            TranslateView(uiLocales);
            return View("OpenId", authorizeOpenId);
        }

        [HttpPost]
        public ActionResult ExternalLoginOpenId(string provider, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentNullException("code");
            }

            // Add the request into the cookie
            var id = Guid.NewGuid().ToString();
            var name = string.Format(ExternalAuthenticateCookieName, id);
            var cookie = new HttpCookie(name)
            {
                Value = code,                
                Expires = DateTime.UtcNow.AddMinutes(5),
                
            };
            Response.Cookies.Add(cookie);

            return new ChallengeResult(provider,
                Url.Action("LoginCallbackOpenId", "Authenticate",
                new { code = id }));
        }
        
        public async Task<ActionResult> LoginCallbackOpenId(string code, string error)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentNullException("code");
            }

            // 1 : retrieve the request from the cookie
            var context = HttpContext.GetOwinContext();
            var cookieName = string.Format(ExternalAuthenticateCookieName, code);
            var request = context.Request.Cookies[string.Format(ExternalAuthenticateCookieName, code)];
            if (request == null)
            {
                throw new IdentityServerException(ErrorCodes.InvalidRequestCode, ErrorDescriptions.TheRequestCannotBeExtractedFromTheCookie);
            }

            // 2 : remove the cookie
            var cookie = new HttpCookie(cookieName)
            {
                Expires = DateTime.UtcNow.AddDays(-1)
            };
            Response.Cookies.Add(cookie);

            // 3 : Raise an exception is there's an authentication error
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new IdentityServerException(
                    ErrorCodes.UnhandledExceptionCode, 
                    string.Format(ErrorDescriptions.AnErrorHasBeenRaisedWhenTryingToAuthenticate, error));
            }
            
            // 4 : Authenticate the user against the external identity provider
            var authenticationManager = context.Authentication;
            var loginInformation = await authenticationManager.GetExternalLoginInfoAsync();
            if (loginInformation == null)
            {
                throw new IdentityServerException(ErrorCodes.UnhandledExceptionCode,
                    ErrorDescriptions.TheLoginInformationCannotBeExtracted);
            }
            
            // 5 : Retrieve the authorization request and continue the open-id flow
            var decodedRequest = _encoder.Decode(request);
            var authorizationRequest = _protector.Decrypt<AuthorizationRequest>(decodedRequest);
            var providerType = loginInformation.Login.LoginProvider;
            var claims = loginInformation.ExternalIdentity.Claims.ToList();
            var actionResult = _authenticateActions.ExternalOpenIdUserAuthentication(
                claims,
                authorizationRequest.ToParameter(),
                request,
                providerType); 
            if (actionResult != null)
            {
                var claimIdentity = new ClaimsIdentity(claims, DefaultAuthenticationTypes.ApplicationCookie);
                authenticationManager.SignIn(
                    new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = DateTime.UtcNow.AddDays(7)
                    },
                    claimIdentity
                );

                return this.CreateRedirectionFromActionResult(actionResult,
                    authorizationRequest);
            }

            return RedirectToAction("OpenId", "Authenticate", new { code = code });
        }

        #endregion

        #endregion

        #region Private methods

        private void TranslateView(string uiLocales)
        {
            var translations = _translationManager.GetTranslations(uiLocales, new List<string>
            {
                Core.Constants.StandardTranslationCodes.LoginCode,
                Core.Constants.StandardTranslationCodes.UserNameCode,
                Core.Constants.StandardTranslationCodes.PasswordCode,
                Core.Constants.StandardTranslationCodes.RememberMyLoginCode,
                Core.Constants.StandardTranslationCodes.LoginLocalAccount,
                Core.Constants.StandardTranslationCodes.LoginExternalAccount
            });

            ViewBag.Translations = translations;
        }

        #endregion

        #region Private classes

        private class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
            }

            public string LoginProvider { get; private set; }

            public string RedirectUri { get; private set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties
                {
                    RedirectUri = RedirectUri
                };
                
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }

        #endregion
    }
}