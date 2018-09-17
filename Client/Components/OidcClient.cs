using DotNetNuke.Services.Authentication;
using ProcsIT.Dnn.AuthServices.OpenIdConnect;
using System;
using System.Web;

namespace ProcsIT.Dnn.Authentication.OpenIdConnect.Components
{
    public class OidcClient : OidcClientBase
    {
        public OidcClient(int portalId, AuthMode mode)
          : base(portalId, mode, "Oidc")
        {
            TokenEndpoint = "https://localhost:5001/connect/token";
            AuthorizationEndpoint = "https://localhost:5001/connect/authorize";
            Scope = HttpUtility.UrlEncode("openid profile offline_access api1");
            MeGraphEndpoint = "https://localhost:5001/connect/userinfo";
            AuthTokenName = "OidcUserToken";
            LoadTokenCookie(string.Empty);
        }

        //protected override string UserGuidKey => base.UserGuidKey;

        protected override TimeSpan GetExpiry(Int32 expiresIn)
        {
            return new TimeSpan(0, 0, expiresIn);
        }

        protected override string GetToken(string accessToken)
        {
            return accessToken;
        }
    }
}