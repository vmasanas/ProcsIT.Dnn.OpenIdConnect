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
            AuthorizationEndpoint = "https://localhost:5001/connect/authorize";
            TokenEndpoint = "https://localhost:5001/connect/token";
            UserInfoEndpoint = "https://localhost:5001/connect/userinfo";

            Scope = HttpUtility.UrlEncode("openid profile offline_access api1");
        }
    }
}
