<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MirakiLogin.ascx.cs" Inherits="RockWeb.Plugins.com_bricksandmortarstudio.MirakiLogin" %>
<script type="text/javascript">

    Sys.Application.add_load(function () {

        var availabilityMessageRow = $('#availabilityMessageRow');
        var usernameUnavailable = $('#availabilityMessage');
        var usernameTextbox = $('#<%= tbNewAccountUserName.ClientID %>');
        var usernameRegExp = new RegExp("<%= Rock.Web.Cache.GlobalAttributesCache.Read().GetValue( "core.ValidUsernameRegularExpression" ) %>");
        var usernameValidCaption = "<%= Rock.Web.Cache.GlobalAttributesCache.Read().GetValue( "core.ValidUsernameCaption" ) %>";

        availabilityMessageRow.hide();

        usernameTextbox.blur(function () {
            if ($(this).val() && $.trim($(this).val()) != '') {

                if (!usernameRegExp.test($(this).val())) {
                    usernameUnavailable.html('Username is not valid. ' + usernameValidCaption);
                    usernameUnavailable.addClass('alert-warning');
                    usernameUnavailable.removeClass('alert-success');
                }
                else {
                    $.ajax({
                        type: 'GET',
                        contentType: 'application/json',
                        dataType: 'json',
                        url: Rock.settings.get('baseUrl') + 'api/userlogins/available/' + escape($(this).val()),
                        success: function (getData, status, xhr) {

                            if (getData) {
                                usernameUnavailable.html('That username is available.');
                                usernameUnavailable.addClass('alert-success');
                                usernameUnavailable.removeClass('alert-warning');
                            } else {
                                availabilityMessageRow.show();
                                usernameUnavailable.html('That username is already taken!');
                                usernameUnavailable.addClass('alert-warning');
                                usernameUnavailable.removeClass('alert-success');
                            }
                        },
                        error: function (xhr, status, error) {
                            alert(status + ' [' + error + ']: ' + xhr.responseText);
                        }
                    });
                }

            } else {
                usernameUnavailable.html('Username is required!');
                usernameUnavailable.addClass('alert-warning');
                usernameUnavailable.removeClass('alert-success');
            }
            availabilityMessageRow.show();
        });

    });

</script>

<asp:UpdatePanel ID="upnlDetails" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlLogin" runat="server" DefaultButton="btnLogin" Visible="true">

            <fieldset>
                <legend>Login</legend>

                <div class="row">
                    <div id="divOrgLogin" runat="server" class="col-sm-12">

                        <asp:ValidationSummary ID="valSummary" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />

                        <asp:Panel ID="pnlSuccess" runat="server" Visible="false" CssClass="alert alert-success success">
                            <asp:Literal ID="lSuccess" runat="server"></asp:Literal>
                        </asp:Panel>
                        <Rock:RockTextBox ID="tbLoginUserName" runat="server" Label="Username" Required="true" DisplayRequiredIndicator="false"></Rock:RockTextBox>
                        <Rock:RockTextBox ID="tbLoginPassword" runat="server" Label="Password" autocomplete="off" Required="true" DisplayRequiredIndicator="false" ValidateRequestMode="Disabled" TextMode="Password"></Rock:RockTextBox>
                        <Rock:RockCheckBox ID="cbRememberMe" runat="server" Text="Remember me on this computer" />

                        <asp:Button ID="btnLogin" runat="server" Text="Login" CssClass="btn btn-primary" OnClick="btnLogin_Click" />
                        <asp:Button ID="btnNewAccount" runat="server" Text="Register" CssClass="btn btn-action" OnClick="btnNewAccount_Click" CausesValidation="false" Visible="true" />
                        <asp:Button ID="btnHelp" runat="server" Text="Forgot Account" CssClass="btn btn-link" OnClick="btnHelp_Click" CausesValidation="false" />

                        <asp:Panel ID="pnlLoginMessage" runat="server" Visible="false" CssClass="alert alert-warning block-message margin-t-md" />

                    </div>
                </div>
            </fieldset>
        </asp:Panel>
        <asp:Panel ID="pnlNewAccount" runat="server" Visible="false">
            <asp:Panel ID="pnlNewAccountMessage" runat="server" Visible="false" CssClass="alert alert-danger" />
            <asp:ValidationSummary ID="valSummaryTop" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />

            <asp:PlaceHolder ID="phUserInfo" runat="server" Visible="true">

                <div class="row">

                    <div class="col-md-6">

                        <fieldset>
                            <legend>New Account</legend>
                            <Rock:RockTextBox ID="tbNewAccountUserName" runat="server" Label="Username" Required="true"></Rock:RockTextBox>
                            <dl id="availabilityMessageRow">
                                <dt></dt>
                                <dd>
                                    <div id="availabilityMessage" class="alert" />
                                </dd>
                            </dl>
                            <Rock:RockTextBox ID="tbNewAccountPassword" runat="server" Label="Password" Required="true" TextMode="Password" ValidateRequestMode="Disabled"></Rock:RockTextBox>
                            <Rock:RockTextBox ID="tbNewAccountPasswordConfirm" runat="server" Label="Confirmation" Required="true" TextMode="Password" ValidateRequestMode="Disabled"></Rock:RockTextBox>
                            <asp:CompareValidator ID="covalPassword" runat="server" ControlToCompare="tbNewAccountPassword" ControlToValidate="tbNewAccountPasswordConfirm" ErrorMessage="Password and Confirmation do not match" Display="Dynamic" CssClass="validation-error"></asp:CompareValidator>

                        </fieldset>

                    </div>

                    <div class="col-md-6">

                        <fieldset>
                            <legend>Your Information</legend>
                            <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" Required="true" />
                            <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" Required="true" />
                            <Rock:EmailBox ID="tbEmail" runat="server" Label="Email" Required="true" />
                            <Rock:RockDropDownList ID="ddlGender" runat="server" Label="Gender">
                                <asp:ListItem Text="" Value="U"></asp:ListItem>
                                <asp:ListItem Text="Male" Value="M"></asp:ListItem>
                                <asp:ListItem Text="Female" Value="F"></asp:ListItem>
                            </Rock:RockDropDownList>
                            <Rock:BirthdayPicker ID="bdaypBirthDay" runat="server" Label="Birthday" />
                        </fieldset>
                    </div>
                </div>

                <div class="actions">
                    <asp:Button ID="btnRegister" runat="server" Text="Register" CssClass="btn btn-primary" OnClick="btnRegister_Click" />
                    <asp:Button ID="btnRegisterBack" runat="server" Text="Back" CssClass="btn btn-link" OnClick="btnRegisterBack_Click" CausesValidation="false" />

                </div>

            </asp:PlaceHolder>

            <asp:PlaceHolder ID="phSuccess" runat="server" Visible="false">

                <div class="alert alert-success">
                    <asp:Literal ID="lSuccessCaption" runat="server" />
                </div>
            </asp:PlaceHolder>
        </asp:Panel>

        <asp:Panel ID="pnlEntry" runat="server" Visible="false">

            <asp:Literal ID="lCaption" runat="server"></asp:Literal>

            <fieldset>
                <Rock:RockTextBox ID="tbForgotUserNameEmail" runat="server" Label="Email" Required="true"></Rock:RockTextBox>
            </fieldset>

            <asp:Panel ID="pnlWarning" runat="server" Visible="false" CssClass="alert alert-warning">
                <asp:Literal ID="lWarning" runat="server"></asp:Literal>
            </asp:Panel>

            <div class="actions">
                <asp:Button ID="btnSend" runat="server" Text="Send Username" CssClass="btn btn-primary" OnClick="btnSend_Click" />
                <asp:Button ID="btnBack" runat="server" Text="Back" CssClass="btn btn-link" OnClick="btnBack_Click" CausesValidation="false" />
            </div>

        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
