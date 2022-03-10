﻿using DotNetNuke.Common;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Instrumentation;
using DotNetNuke.Security.Membership;
using DotNetNuke.Services.Authentication;
using DotNetNuke.Services.Authentication.Oidc;
using DotNetNuke.Services.Localization;
using IdentityModel.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace ProcsIT.Dnn.AuthServices.OpenIdConnect
{
    public abstract class OidcClientBase
    {
        protected string AuthorizationEndpoint { get; set; }
        protected string UserInfoEndpoint { get; set; }
        protected string TokenEndpoint { get; set; }
        protected string Scope { get; set; }
        protected string APIResource { get; set; }

        private string VerificationCode => HttpContext.Current.Request.Params[OAuthCodeKey];

        private TokenResponse TokenResponse { get; set; }

        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(OidcClientBase));

        private const string OAuthClientIdKey = "client_id";
        private const string OAuthClientSecretKey = "client_secret";
        private const string OAuthRedirectUriKey = "redirect_uri";
        private const string OAuthGrantTypeKey = "grant_type";
        private const string OAuthCodeKey = "code";
        private const string OAuthHybrid = "code";

        // TODO: request flow on settings: 
        // - code
        // - code id_token
        //private const string OAuthHybrid = "code id_token";

        private readonly Random _random = new Random();

        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _callbackUri;
        private readonly string _service;
        private readonly AuthMode _authMode;

        //Set default Expiry to 14 days 
        private TimeSpan AuthTokenExpiry { get; set; } = new TimeSpan(14, 0, 0, 0);

        protected OidcClientBase(int portalId, AuthMode mode, string service)
        {
            _authMode = mode;
            _service = service;

            _apiKey = OidcConfigBase.GetConfig(_service, portalId).APIKey;
            _apiSecret = OidcConfigBase.GetConfig(_service, portalId).APISecret;

            _callbackUri = _authMode == AuthMode.Login
                                    ? Globals.LoginURL(string.Empty, false)
                                    : Globals.RegisterURL(string.Empty, string.Empty);
        }

        public virtual void Authorize()
        {
            // hybrid flow
            var parameters = new List<QueryParameter>
                                        {
                                            new QueryParameter { Name = "response_type", Value = OAuthHybrid },
                                            new QueryParameter { Name = OAuthClientIdKey, Value = _apiKey },
                                            new QueryParameter { Name = OAuthRedirectUriKey, Value = _callbackUri },
                                            new QueryParameter { Name = "scope", Value = Scope },
                                            new QueryParameter { Name = "nonce", Value = GenerateNonce() },
                                            new QueryParameter { Name = "state", Value = _service },
                                            new QueryParameter { Name = "response_mode", Value = "form_post" }
                                        };

            // Call authorization endpoint
            HttpContext.Current.Response.Redirect(AuthorizationEndpoint + "?" + parameters.ToNormalizedString(), true);
        }

        public virtual AuthorisationResult Authorize(PortalSettings settings, string IPAddress)
        {
            // TODO: When user is allowed to give consent, what to do when certain items are denied?
            // refresh_token -> unable to refresh
            // userClaims => only sub is known, other claims remain empty
            // api1 => no access to api
            // The client can be configured to set required items or not ask for consent. But if not:
            // TODO: implement missing refresh token, unable to access api
            var acceptedScopes = HttpContext.Current.Request["Scope"];

            // Hybrid flow it needs to be
            var identityToken = HttpContext.Current.Request["id_token"];
            if (string.IsNullOrEmpty(identityToken))
            {
                // Authorizaton flow
                // exchange access code for token
                var codeParams = new List<QueryParameter>
                {
                    new QueryParameter { Name = OAuthClientIdKey, Value = _apiKey },
                    new QueryParameter { Name = OAuthClientSecretKey, Value = _apiSecret },
                    new QueryParameter { Name = OAuthCodeKey, Value = VerificationCode },

                    new QueryParameter { Name = OAuthRedirectUriKey, Value = _callbackUri },
                    new QueryParameter { Name = OAuthGrantTypeKey, Value = "authorization_code" },
                };

                var codeResponse = ExecuteWebRequest(HttpMethod.Post, new Uri(TokenEndpoint), codeParams.ToNormalizedString(), string.Empty);
                if (codeResponse == null)
                    return AuthorisationResult.Denied;

                TokenResponse = new TokenResponse(codeResponse);

                if (TokenResponse.IsError || string.IsNullOrEmpty(TokenResponse.IdentityToken))
                    return AuthorisationResult.Denied;

                identityToken = TokenResponse.IdentityToken;
            }

            var userId = GetUserId(identityToken);

            if (userId == null)
                return AuthorisationResult.Denied;

            var loginStatus = UserLoginStatus.LOGIN_FAILURE;
            var objUserInfo = UserController.ValidateUser(settings.PortalId, userId, string.Empty, _service, string.Empty, settings.PortalName, IPAddress, ref loginStatus);
            if (objUserInfo != null && (objUserInfo.IsDeleted || loginStatus != UserLoginStatus.LOGIN_SUCCESS))
                return AuthorisationResult.Denied;

            AuthTokenExpiry = GetExpiry(Convert.ToInt32(TokenResponse.ExpiresIn));
            return TokenResponse == null ? AuthorisationResult.Denied : AuthorisationResult.Authorized;
        }

        private string GenerateNonce()
        {
            // Just a simple implementation of a random number between 123400 and 9999999
            return _random.Next(123400, 9999999).ToString(CultureInfo.InvariantCulture);
        }

        private string ComputeHash(HashAlgorithm hashAlgorithm, string data)
        {
            if (hashAlgorithm == null)
                throw new ArgumentNullException("hashAlgorithm");

            if (string.IsNullOrEmpty(data))
                throw new ArgumentNullException("data");

            byte[] dataBuffer = Encoding.ASCII.GetBytes(data);
            byte[] hashBytes = hashAlgorithm.ComputeHash(dataBuffer);

            return Convert.ToBase64String(hashBytes);
        }

        private string ExecuteWebRequest(HttpMethod method, Uri uri, string parameters, string authHeader)
        {
            WebRequest request;

            if (method == HttpMethod.Post)
            {
                byte[] byteArray = Encoding.UTF8.GetBytes(parameters);

                request = WebRequest.CreateDefault(uri);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = byteArray.Length;

                if (!string.IsNullOrEmpty(parameters))
                {
                    Stream dataStream = request.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();
                }
            }
            else
            {
                request = WebRequest.CreateDefault(GenerateRequestUri(uri.ToString(), parameters));
            }

            if (TokenResponse?.AccessToken != null)
                request.Headers.Add($"Authorization: Bearer {TokenResponse.AccessToken}");

            try
            {
                using (WebResponse response = request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        using (var responseReader = new StreamReader(responseStream))
                        {
                            return responseReader.ReadToEnd();
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                using (Stream responseStream = ex.Response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        using (var responseReader = new StreamReader(responseStream))
                        {
                            Logger.ErrorFormat("WebResponse exception: {0}", responseReader.ReadToEnd());
                        }
                    }
                }
            }
            return null;
        }

        private string GenerateSignatureUsingHash(string signatureBase, HashAlgorithm hash)
        {
            return ComputeHash(hash, signatureBase);
        }

        private Uri GenerateRequestUri(string url, string parameters)
        {
            if (string.IsNullOrEmpty(parameters))
                return new Uri(url);

            return new Uri(string.Format("{0}{1}{2}", url, url.Contains("?") ? "&" : "?", parameters));
        }

        protected virtual TimeSpan GetExpiry(int expiresIn)
        {
            return TimeSpan.MinValue;
        }

        protected virtual string GetToken(string accessToken)
        {
            return accessToken;
        }

        public virtual void AuthenticateUser(UserData user, PortalSettings settings, string IPAddress, Action<NameValueCollection> addCustomProperties, Action<UserAuthenticatedEventArgs> onAuthenticated)
        {
            var loginStatus = UserLoginStatus.LOGIN_FAILURE;

            var objUserInfo = UserController.ValidateUser(settings.PortalId, user.Id, string.Empty, _service, user.Id, settings.PortalName, IPAddress, ref loginStatus);

            // Raise UserAuthenticated Event
            var eventArgs = new UserAuthenticatedEventArgs(objUserInfo, user.Id, loginStatus, _service)
            {
                AutoRegister = true
            };

            // TODO:
            var profileProperties = new NameValueCollection();

            if (string.IsNullOrEmpty(objUserInfo?.FirstName) && !string.IsNullOrEmpty(user.FirstName))
                profileProperties.Add("FirstName", user.FirstName);

            if (string.IsNullOrEmpty(objUserInfo?.LastName) && !string.IsNullOrEmpty(user.LastName))
                profileProperties.Add("LastName", user.LastName);

            if (string.IsNullOrEmpty(objUserInfo?.Email) && !string.IsNullOrEmpty(user.Email))
                profileProperties.Add("Email", user.Email);

            if (string.IsNullOrEmpty(objUserInfo?.DisplayName) && !string.IsNullOrEmpty(user.DisplayName))
                profileProperties.Add("DisplayName", user.DisplayName);

            if (string.IsNullOrEmpty(objUserInfo?.Profile.GetPropertyValue("Website")) && !string.IsNullOrEmpty(user.Website))
                profileProperties.Add("Website", user.Website);

            if (string.IsNullOrEmpty(objUserInfo?.Profile.GetPropertyValue("PreferredLocale")) && !string.IsNullOrEmpty(user.Locale))
            {
                if (LocaleController.IsValidCultureName(user.Locale.Replace('_', '-')))
                    profileProperties.Add("PreferredLocale", user.Locale.Replace('_', '-'));
                else
                    profileProperties.Add("PreferredLocale", settings.CultureCode);
            }

            //if (string.IsNullOrEmpty(objUserInfo.Profile.GetPropertyValue("PreferredTimeZone"))))
            //{
            //    if (string.IsNullOrEmpty(user.TimeZoneInfo))
            //    {
            //        if (Int32.TryParse(user.Timezone, out int timeZone))
            //        {
            //            var timeZoneInfo = Localization.ConvertLegacyTimeZoneOffsetToTimeZoneInfo(timeZone);

            //            profileProperties.Add("PreferredTimeZone", timeZoneInfo.Id);
            //        }
            //    }
            //    else
            //    {
            //        profileProperties.Add("PreferredTimeZone", user.TimeZoneInfo);
            //    }
            //}

            addCustomProperties(profileProperties);

            eventArgs.Profile = profileProperties;

            onAuthenticated(eventArgs);
        }

        public virtual TUserData GetCurrentUser<TUserData>() where TUserData : UserData
        {
            if (!IsCurrentUserAuthorized())
                return null;

            var responseText = ExecuteWebRequest(HttpMethod.Get, GenerateRequestUri(UserInfoEndpoint, TokenResponse.AccessToken), null, string.Empty);
            var user = JsonConvert.DeserializeObject<TUserData>(responseText);
            user.Id = GetUserId(TokenResponse?.IdentityToken);
            return user;
        }

        private string GetUserId(string identityToken)
        {
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(identityToken))
                return null;

            var token = tokenHandler.ReadJwtToken(identityToken);

            // always use email
            // was: return $"{_service}_{token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value}";
            return $"{token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value}";
        }

        public bool HasVerificationCode()
        {
            return VerificationCode != null;
        }

        public bool IsCurrentService()
        {
            var service = HttpContext.Current.Request.Params["state"];
            return !string.IsNullOrEmpty(service) && service == _service;
        }

        public bool IsCurrentUserAuthorized()
        {
            return TokenResponse?.AccessToken != null;
        }

    }
}
