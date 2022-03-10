<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Login.ascx.cs" Inherits="ProcsIT.Dnn.Authentication.OpenIdConnect.Login" %>
<style>
    /* SET SOCIAL SPECIFIC STYLES */
    ul.buttonList .oidc a span {
        padding-left: 45px;
    }

    ul.buttonList .oidc a:after {
        position: absolute;
        left: 35px;
        top: 0;
        height: 100%;
        width: 0;
        content: "";
        border-left: 1px solid rgba(0,0,0,0.2);
        border-right: 1px solid rgba(255,255,255,0.3);
    }
    /*FACEBOOK*/
    ul.buttonList .oidc a {
        color: #fff;
        text-shadow: 0px -1px 0px rgba(0,0,0,0.4);
        border-color: #888888; /* dark blue */
        background-position: 0 -500px;
        background-color: #b4b4b4;
    }

        ul.buttonList .oidc a:hover {
            color: #fff;
            text-shadow: 0px -1px 0px rgba(0,0,0,0.4);
            border-color: #888888; /* dark blue */
            background-position: 0 -550px;
            background-color: #888888;
        }

        ul.buttonList .oidc a:active {
            background-position: 0 -550px;
            border-color: #888888; /* dark blue */
            background-color: #888888;
        }
</style>


<asp:PlaceHolder ID="plOidc" runat="server">
    <br />
    <ul class="buttonList">
        <li class="oidc">
            <asp:LinkButton runat="server" ID="loginButton" CausesValidation="False">
            <span><%=LocalizeString("LoginOidc") %></span>
            </asp:LinkButton>
        </li>
    </ul>
</asp:PlaceHolder>
