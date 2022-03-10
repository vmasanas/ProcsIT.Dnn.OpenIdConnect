using DotNetNuke.Services.Authentication;
using ProcsIT.Dnn.AuthServices.OpenIdConnect;
using System;
using System.Web;

namespace ProcsIT.Dnn.Authentication.OpenIdConnect.Components
{
    public class OidcClient : OidcClientBase
    {
        protected override TimeSpan GetExpiry(int expirseIn) => new TimeSpan(0, 0, expirseIn);

        protected override string GetToken(string accessToken) => accessToken;

        public OidcClient(int portalId, AuthMode mode)
          : base(portalId, mode, "Oidc")
        {
            AuthorizationEndpoint = "https://api.mpin.io/authorize";
            TokenEndpoint =         "https://api.mpin.io/oidc/token";
            UserInfoEndpoint =      "https://api.mpin.io/oidc/userinfo";

            Scope = HttpUtility.UrlEncode("openid email");

            //TODO: Request scopes on settings
            //Scope = HttpUtility.UrlEncode("openid profile offline_access api1");
        }
    }
}
