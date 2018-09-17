using DotNetNuke.Common;
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
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(OidcClientBase));

        private const string OAuthTokenSecretKey = "oauth_token_secret";
        private const string OAuthClientIdKey = "client_id";
        private const string OAuthClientSecretKey = "client_secret";
        private const string OAuthRedirectUriKey = "redirect_uri";
        private const string OAuthGrantTypeKey = "grant_type";
        private const string OAuthCodeKey = "code";

        private readonly Random random = new Random();

        private const string UnreservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
        
        //DNN-6265 - Support OAuth V2 optional parameter resource, which is required by Microsoft Azure Active
        //Directory implementation of OAuth V2
        private const string OAuthResourceKey = "resource";

        protected OidcClientBase(int portalId, AuthMode mode, string service)
        {
            //Set default Expiry to 14 days 
            AuthTokenExpiry = new TimeSpan(14, 0, 0, 0);
            Service = service;

            APIKey = OidcConfigBase.GetConfig(Service, portalId).APIKey;
            APISecret = OidcConfigBase.GetConfig(Service, portalId).APISecret;
            Mode = mode;

            CallbackUri = Mode == AuthMode.Login
                                    ? Globals.LoginURL(string.Empty, false)
                                    : Globals.RegisterURL(string.Empty, string.Empty);
        }

        protected const string OAuthTokenKey = "oauth_token";

        protected virtual string UserGuidKey 
        {
            get { return string.Empty; }
        }

        protected string APIKey { get; set; }
        protected string APISecret { get; set; }
        protected AuthMode Mode { get; set; }

        protected TokenResponse TokenResponse { get; set; }

        protected string TokenSecret { get; set; }
        protected string UserGuid { get; set; }
        protected string AuthorizationEndpoint { get; set; }
        protected TimeSpan AuthTokenExpiry { get; set; }
        protected string MeGraphEndpoint { get; set; }
        protected string TokenEndpoint { get; set; }
        protected string AuthTokenName { get; set; }        
        protected string Scope { get; set; }
		protected string AccessToken { get; set; }
        protected string IdentityToken { get; set; }
        protected string RefreshToken { get; set; }
        protected string VerificationCode
        {
            get { return HttpContext.Current.Request.Params[OAuthCodeKey]; }
        }

        // Support "Optional" Resource Parameter required by Azure AD Oauth V2 Solution
        protected string APIResource { get; set; }

        public string CallbackUri { get; set; }
        public string Service { get; set; }

        public virtual AuthorisationResult Authorize()
        {
            string errorReason = HttpContext.Current.Request.Params["error_reason"];
            bool userDenied = (errorReason != null);
            if (userDenied)
            {
                return AuthorisationResult.Denied;
            }

            if (!HaveVerificationCode())
            {
                string nonce = GenerateNonce();
                var parameters = new List<QueryParameter>
                                        {
                                            // TODO: code id_token doesn't work. Request does not return here.

                                            //new QueryParameter { Name = "response_type", Value = "code id_token" },
                                            new QueryParameter { Name = "response_type", Value = "code" },
                                            new QueryParameter { Name = OAuthClientIdKey, Value = APIKey },
                                            new QueryParameter { Name = OAuthRedirectUriKey, Value = CallbackUri },
                                            new QueryParameter { Name = "scope", Value = Scope },
                                            new QueryParameter { Name = "nonce", Value = nonce },
                                            new QueryParameter { Name = "state", Value = Service },
                                        };

                HttpContext.Current.Response.Redirect(AuthorizationEndpoint + "?" + parameters.ToNormalizedString(), true);
                return AuthorisationResult.RequestingCode;
            }
            
            ExchangeCodeForToken();

            return TokenResponse == null ? AuthorisationResult.Denied : AuthorisationResult.Authorized;
        }

        private string GenerateNonce()
        {
            // Just a simple implementation of a random number between 123400 and 9999999
            return random.Next(123400, 9999999).ToString(CultureInfo.InvariantCulture);
        }

        private string ComputeHash(HashAlgorithm hashAlgorithm, string data)
        {
            if (hashAlgorithm == null)
            {
                throw new ArgumentNullException("hashAlgorithm");
            }

            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentNullException("data");
            }

            byte[] dataBuffer = Encoding.ASCII.GetBytes(data);
            byte[] hashBytes = hashAlgorithm.ComputeHash(dataBuffer);

            return Convert.ToBase64String(hashBytes);
        }
        
        private void ExchangeCodeForToken()
        {
            IList<QueryParameter> parameters = new List<QueryParameter>();
            parameters.Add(new QueryParameter { Name = OAuthClientIdKey, Value = APIKey });
            parameters.Add(new QueryParameter { Name = OAuthRedirectUriKey, Value = CallbackUri });
            //DNN-6265 Support for OAuth V2 Secrets which are not URL Friendly
            parameters.Add(new QueryParameter { Name = OAuthClientSecretKey, Value = APISecret });
            parameters.Add(new QueryParameter { Name = OAuthGrantTypeKey, Value = "authorization_code" });
            //parameters.Add(new QueryParameter { Name = OAuthGrantTypeKey, Value = "code id_token" });
            parameters.Add(new QueryParameter { Name = OAuthCodeKey, Value = VerificationCode });

            // Support for OAuth V2 optional parameter
            if (!string.IsNullOrEmpty(APIResource))
            {
                parameters.Add(new QueryParameter { Name = "resource", Value = APIResource });
            }

            var responseText = ExecuteWebRequest(HttpMethod.Post, new Uri(TokenEndpoint), parameters.ToNormalizedString(), String.Empty);
            TokenResponse = new TokenResponse(responseText);

            // Do something when there is an error.
            if (TokenResponse.IsError)
                ;

            AuthTokenExpiry = GetExpiry(Convert.ToInt32(TokenResponse.ExpiresIn));
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
            {
                request.Headers.Add($"Authorization: Bearer {TokenResponse.AccessToken}");
            }

            try
            {
                using (WebResponse response = request.GetResponse())
                {
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

        private void SaveTokenCookie(string suffix)
        {
            var authTokenCookie = new HttpCookie(AuthTokenName + suffix) { Path = (!string.IsNullOrEmpty(Globals.ApplicationPath) ? Globals.ApplicationPath : "/") };
            authTokenCookie.Values[OAuthTokenKey] = TokenResponse.AccessToken;
            authTokenCookie.Values[OAuthTokenSecretKey] = TokenSecret;
            authTokenCookie.Values[UserGuidKey] = UserGuid;

            authTokenCookie.Expires = DateTime.Now.Add(AuthTokenExpiry);
            HttpContext.Current.Response.SetCookie(authTokenCookie);
        }

        private Uri GenerateRequestUri(string url, string parameters)
        {
            if (string.IsNullOrEmpty(parameters))
                return new Uri(url);

            return new Uri(string.Format("{0}{1}{2}", url, url.Contains("?") ? "&" : "?", parameters));
        }

        protected virtual TimeSpan GetExpiry(Int32 expiresIn)
        {
            return TimeSpan.MinValue;
        }

        protected virtual string GetToken(string accessToken)
        {
            return accessToken;
        }

        protected void LoadTokenCookie(string suffix)
        {
            HttpCookie authTokenCookie = HttpContext.Current.Request.Cookies[AuthTokenName + suffix];
            if (authTokenCookie != null)
            {
                if (authTokenCookie.HasKeys)
                {
                    //AuthToken = authTokenCookie.Values[OAuthTokenKey];
                    TokenSecret = authTokenCookie.Values[OAuthTokenSecretKey];
                    UserGuid = authTokenCookie.Values[UserGuidKey];
                }
            }
        }

        public virtual void AuthenticateUser(UserData user, PortalSettings settings, string IPAddress, Action<NameValueCollection> addCustomProperties, Action<UserAuthenticatedEventArgs> onAuthenticated)
        {
            var loginStatus = UserLoginStatus.LOGIN_FAILURE;

            var objUserInfo = UserController.ValidateUser(settings.PortalId, user.UserName, "",
                                                                Service, "",
                                                                settings.PortalName, IPAddress,
                                                                ref loginStatus);


            // Raise UserAuthenticated Event
            var eventArgs = new UserAuthenticatedEventArgs(objUserInfo, user.UserName, loginStatus, Service)
                                            {
                                                AutoRegister = true
                                            };

            var profileProperties = new NameValueCollection();

            if (objUserInfo == null || (string.IsNullOrEmpty(objUserInfo.FirstName) && !string.IsNullOrEmpty(user.FirstName)))
            {
                profileProperties.Add("FirstName", user.FirstName);
            }
            if (objUserInfo == null || (string.IsNullOrEmpty(objUserInfo.LastName) && !string.IsNullOrEmpty(user.LastName)))
            {
                profileProperties.Add("LastName", user.LastName);
            }
            //if (objUserInfo == null || (string.IsNullOrEmpty(objUserInfo.Email) && !string.IsNullOrEmpty(user.Email)))
            //{
            //    profileProperties.Add("Email", user.PreferredEmail);
            //}
            if (objUserInfo == null || (string.IsNullOrEmpty(objUserInfo.DisplayName) && !string.IsNullOrEmpty(user.DisplayName)))
            {
                profileProperties.Add("DisplayName", user.DisplayName);
            }
            //if (objUserInfo == null || (string.IsNullOrEmpty(objUserInfo.Profile.GetPropertyValue("ProfileImage")) && !string.IsNullOrEmpty(user.ProfileImage)))
            //{
            //    profileProperties.Add("ProfileImage", user.ProfileImage);
            //}
            if (objUserInfo == null || (string.IsNullOrEmpty(objUserInfo.Profile.GetPropertyValue("Website")) && !string.IsNullOrEmpty(user.Website)))
            {
                profileProperties.Add("Website", user.Website);
            }
            if ((objUserInfo == null || string.IsNullOrEmpty(objUserInfo.Profile.GetPropertyValue("PreferredLocale"))) && !string.IsNullOrEmpty(user.Locale))
            {
                if (LocaleController.IsValidCultureName(user.Locale.Replace('_', '-')))
                {
                    profileProperties.Add("PreferredLocale", user.Locale.Replace('_', '-'));
                }
                else
                {
                    profileProperties.Add("PreferredLocale", settings.CultureCode);
                }
            }

            //if (objUserInfo == null || (string.IsNullOrEmpty(objUserInfo.Profile.GetPropertyValue("PreferredTimeZone"))))
            //{
            //    if (String.IsNullOrEmpty(user.TimeZoneInfo))
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

            if (Mode == AuthMode.Login)
            {
                SaveTokenCookie(string.Empty);
            }

            onAuthenticated(eventArgs);
        }

        public virtual TUserData GetCurrentUser<TUserData>() where TUserData : UserData
        {
            LoadTokenCookie(string.Empty);

            if (!IsCurrentUserAuthorized())
                return null;

            var responseText = ExecuteWebRequest(HttpMethod.Get, GenerateRequestUri(MeGraphEndpoint, TokenResponse.AccessToken), null, string.Empty);
            var user = JsonConvert.DeserializeObject<TUserData>(responseText);
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();

            if (tokenHandler.CanReadToken(TokenResponse?.IdentityToken))
            {
                var profile = tokenHandler.ReadJwtToken(TokenResponse?.IdentityToken);
                user.Id = $"{Service}_{profile.Claims.FirstOrDefault(c => c.Type == "sub")?.Value}";
                if (string.IsNullOrEmpty(user.UserName))
                    user.UserName = user.Name ?? profile.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? user.Id;
            }
            return user;
        }

        public bool HaveVerificationCode()
        {
            return VerificationCode != null;
        }

        public bool IsCurrentService()
        {
            string service = HttpContext.Current.Request.Params["state"];
            return !string.IsNullOrEmpty(service) && service == Service;
        }

        public bool IsCurrentUserAuthorized()
        {
            return TokenResponse?.AccessToken != null;
        }

    }
}
