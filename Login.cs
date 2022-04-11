using DotNetNuke.Services.Authentication;
using ProcsIT.Dnn.Authentication.OpenIdConnect.Components;
using ProcsIT.Dnn.AuthServices.OpenIdConnect;
using System;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace ProcsIT.Dnn.Authentication.OpenIdConnect
{
    public class Login : OidcLoginBase
    {
        protected PlaceHolder plOidc;
        protected LinkButton loginButton;

        protected override string AuthSystemApplicationName => "Oidc";

        public override bool SupportsRegistration => false;

        protected override UserData GetCurrentUser() => OAuthClient.GetCurrentUser<OidcUserData>();

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            loginButton.Click += new EventHandler(LoginButton_Click);
            OAuthClient = new OidcClient(PortalId, Mode);

            OidcConfigBase config = OidcConfigBase.GetConfig(AuthSystemApplicationName, PortalId);

            if (Request.QueryString["NoIdc"] != null && config.NoIdc)
            {
                // do not process the automatic login
            }
            else if (Request.HttpMethod != "POST" && Request["code"] is null && !Request.IsAuthenticated && config.AutoLogin)
            {
                OAuthClient.Authorize();
            }
            else
            {
                plOidc.Visible = Mode == AuthMode.Login;
            }
        }

        private void LoginButton_Click(object sender, EventArgs e)
        {
            OAuthClient.Authorize();
        }
    }
}
