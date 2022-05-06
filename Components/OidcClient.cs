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
            OidcConfigBase config = OidcConfigBase.GetConfig("Oidc", portalId);

            AuthorizationEndpoint = config.AuthorizationEndpoint;
            TokenEndpoint =         config.TokenEndpoint;
            UserInfoEndpoint =      config.UserInfoEndpoint;

            Scope = HttpUtility.UrlEncode("openid email");

            //TODO: Request scopes on settings
            //Scope = HttpUtility.UrlEncode("openid profile offline_access api1");
        }
    }
}
