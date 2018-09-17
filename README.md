# ProcsIT.Dnn.OpenIdConnect
OpenIdConnect provider and plugin for DNN (DotNetNuke).

I've written an OpenIdConnect provider because I needed one and no public providers were available. I am not a member of the DNN community and I have no further knowledge of DNN. The software in this repository is just what I needed for a POC.
With this implementation I've been able to login using IdentityServer4.

This is a very simple and by far not complete implementation of OpenIdConnect. The code was adapted from an old version of OpenId that I found in the archives of codeplex.

I've managed to make this work for grant type 'authorization_code' and response type 'code'. But I didn't succeed for other grant types. Nevertheless I've reached my goal, so I didn't investigate any further.

The software contains of three parts:

1. provider: handles the oidc requests
2. plugin: compiled code that contains settings and interacts with the provider
3. web UI: very basic login form and settings form

Update the settings in the plugin:

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

There is no package available to install this component. So in order to make this work, copy the provider dll and plugin dll to the \Website\bin folder. Copy the web UI to \Website\DesktopModules\AuthenticationServices\oidc.

Configure DNN to add oidc authentication. When succeeded an extra tab will be visible with a link to the IDP. After clicking on the link the user is redirected to the IDP, logs in and is redirected back to DNN. At that point the admin will receive a message that a new user needs to be approved.

Once approved, the user has access like a local user. It is quite possible that there can be some improvements. Like a direct link instead of a tab and automatically approve new users.

