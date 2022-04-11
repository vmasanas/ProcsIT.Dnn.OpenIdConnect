<%@ Control Language="C#" AutoEventWireup="false" Inherits="ProcsIT.Dnn.Authentication.OpenIdConnect.Settings" CodeBehind="Settings.ascx.cs" %>
<%@ Register TagPrefix="dnn" Namespace="DotNetNuke.UI.WebControls" Assembly="DotNetNuke" %>
<div class="dnnFormItem">
    <asp:Label ID="lblAppId" runat="server" Text="APP ID:" />
    <asp:TextBox runat="server" ID="txtAppID" Width="200px"></asp:TextBox>
</div>
<div class="dnnFormItem">
    <asp:Label ID="lblAppSecred" runat="server" Text="APP Secret:" />
    <asp:TextBox runat="server" ID="txtAppSecret" Width="400px"></asp:TextBox>
</div>
<div class="dnnFormItem">
    <asp:label id="lblEnabled" runat="server" Text="Enabled:"></asp:label>
    <asp:CheckBox Checked="true" runat="server" ID="chkEnabled"></asp:CheckBox>
</div>
<div class="dnnFormItem">
    <asp:label id="lblAutoLogin" runat="server" Text="Automatic login:"></asp:label>
    <asp:CheckBox Checked="true" runat="server" ID="chkAutoLogin"></asp:CheckBox>
</div>
<div class="dnnFormItem">
    <asp:label id="lblEnableHack" runat="server" Text="Enable 'noidc' option:"></asp:label>
    <asp:CheckBox Checked="true" runat="server" ID="chkNoIdc"></asp:CheckBox>
</div>