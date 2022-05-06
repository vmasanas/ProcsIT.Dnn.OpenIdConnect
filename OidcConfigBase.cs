using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Authentication;
using System;
using System.Globalization;

namespace ProcsIT.Dnn.AuthServices.OpenIdConnect
{
    /// <summary>
    /// The Config class provides a central area for management of Module Configuration Settings.
    /// </summary>
    [Serializable]
    public class OidcConfigBase : AuthenticationConfigBase
    {
        private const string _cacheKey = "OidcAuthentication";
        protected string Service { get; set; }

        public string AuthorizationEndpoint { get; set; }
        public string TokenEndpoint { get; set; }
        public string UserInfoEndpoint { get; set; }

        public string APIKey { get; set; }
        public string APISecret { get; set; }
        public bool Enabled { get; set; }
        public bool AutoLogin { get; set; }
        public bool NoIdc { get; set; }

        protected OidcConfigBase(string service, int portalId)
            : base(portalId)
        {
            Service = service;

            AuthorizationEndpoint = PortalController.GetPortalSetting(Service + "_AuthorizationEndpoint", portalId, "");
            TokenEndpoint = PortalController.GetPortalSetting(Service + "_TokenEndpoint", portalId, "");
            UserInfoEndpoint = PortalController.GetPortalSetting(Service + "_UserInfoEndpoint", portalId, "");

            APIKey = PortalController.GetPortalSetting(Service + "_APIKey", portalId, "");
            APISecret = PortalController.GetPortalSetting(Service + "_APISecret", portalId, "");
            Enabled = PortalController.GetPortalSettingAsBoolean(Service + "_Enabled", portalId, false);
            AutoLogin = PortalController.GetPortalSettingAsBoolean(Service + "_AutoLogin", portalId, false);
            NoIdc = PortalController.GetPortalSettingAsBoolean(Service + "_NoIdc", portalId, false);
        }

        private static string GetCacheKey(string service, int portalId)
        {
            return _cacheKey + "." + service + "_" + portalId;
        }

        public static void ClearConfig(string service, int portalId)
        {
            DataCache.RemoveCache(GetCacheKey(service, portalId));
        }

        public static OidcConfigBase GetConfig(string service, int portalId)
        {
            string key = GetCacheKey(service, portalId);
            var config = (OidcConfigBase)DataCache.GetCache(key);
            if (config == null)
            {
                config = new OidcConfigBase(service, portalId);
                DataCache.SetCache(key, config);
            }
            return config;
        }

        public static void UpdateConfig(OidcConfigBase config)
        {
            PortalController.UpdatePortalSetting(config.PortalID, config.Service + "_AuthorizationEndpoint", config.AuthorizationEndpoint);
            PortalController.UpdatePortalSetting(config.PortalID, config.Service + "_TokenEndpoint", config.TokenEndpoint);
            PortalController.UpdatePortalSetting(config.PortalID, config.Service + "_UserInfoEndpoint", config.UserInfoEndpoint);

            PortalController.UpdatePortalSetting(config.PortalID, config.Service + "_APIKey", config.APIKey);
            PortalController.UpdatePortalSetting(config.PortalID, config.Service + "_APISecret", config.APISecret);
            PortalController.UpdatePortalSetting(config.PortalID, config.Service + "_Enabled", config.Enabled.ToString(CultureInfo.InvariantCulture));
            PortalController.UpdatePortalSetting(config.PortalID, config.Service + "_AutoLogin", config.AutoLogin.ToString(CultureInfo.InvariantCulture));
            PortalController.UpdatePortalSetting(config.PortalID, config.Service + "_NoIdc", config.NoIdc.ToString(CultureInfo.InvariantCulture));
            ClearConfig(config.Service, config.PortalID);
        }
    }
}
