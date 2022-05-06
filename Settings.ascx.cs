using DotNetNuke.Services.Authentication;
using DotNetNuke.Services.Exceptions;
using ProcsIT.Dnn.AuthServices.OpenIdConnect;
using System;

namespace ProcsIT.Dnn.Authentication.OpenIdConnect
{
    public partial class Settings : AuthenticationSettingsBase
    {
        private const string Service = "Oidc";
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            try
            {
                var config = OidcConfigBase.GetConfig(Service, PortalId);

                txtAuthorizationEndpoint.Text = config.AuthorizationEndpoint;
                txtTokenEndpoint.Text = config.TokenEndpoint;
                txtUserInfoEndpoint.Text = config.UserInfoEndpoint;

                txtAppID.Text = config.APIKey;
                txtAppSecret.Text = config.APISecret;
                chkEnabled.Checked = config.Enabled;
                chkAutoLogin.Checked = config.AutoLogin;
                chkNoIdc.Checked = config.NoIdc;
            }
            catch (Exception exc)
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }
        public override void UpdateSettings()
        {
            try
            {
                var config = OidcConfigBase.GetConfig(Service, PortalId);
                config.AuthorizationEndpoint = txtAuthorizationEndpoint.Text;
                config.TokenEndpoint = txtTokenEndpoint.Text;
                config.UserInfoEndpoint = txtUserInfoEndpoint.Text;

                config.PortalID = PortalId;
                config.APIKey = txtAppID.Text;
                config.APISecret = txtAppSecret.Text;
                config.Enabled = chkEnabled.Checked;
                config.AutoLogin = chkAutoLogin.Checked;
                config.NoIdc = chkNoIdc.Checked;

                OidcConfigBase.UpdateConfig(config);
            }
            catch (Exception exc)
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }
    }
}

