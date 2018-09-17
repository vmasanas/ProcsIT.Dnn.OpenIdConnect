using DotNetNuke.Entities.Modules;
using DotNetNuke.Services.Authentication;
using DotNetNuke.Services.Authentication.Oidc;
using DotNetNuke.Services.Localization;
using DotNetNuke.UI.Skins;
using DotNetNuke.UI.Skins.Controls;
using ProcsIT.Dnn.Authentication.OpenIdConnect.Components;
using ProcsIT.Dnn.AuthServices.OpenIdConnect;
using System;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace ProcsIT.Dnn.Authentication.OpenIdConnect
{
    public class Login : OidcLoginBase
    {
    protected HtmlGenericControl loginItem;
    protected LinkButton loginButton;

    protected override string AuthSystemApplicationName
    {
      get
      {
        return "Oidc";
      }
    }

    public override bool SupportsRegistration
    {
      get
      {
        return true;
      }
    }

    protected override UserData GetCurrentUser()
    {
      return OAuthClient.GetCurrentUser<OidcUserData>();
    }

    protected override void OnInit(EventArgs e)
    {
      base.OnInit(e);
      loginButton.Click += new EventHandler(LoginButton_Click);
      OAuthClient = new OidcClient(PortalId, Mode);
      loginItem.Visible = Mode == AuthMode.Login;
    }

    private void LoginButton_Click(object sender, EventArgs e)
    {
      if (OAuthClient.Authorize() != AuthorisationResult.Denied)
        return;
      Skin.AddModuleMessage((PortalModuleBase) this, Localization.GetString("PrivateConfirmationMessage", Localization.SharedResourceFile), ModuleMessage.ModuleMessageType.YellowWarning);
    }
  }
}
