// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_bricksandmortarstudio
{
    /// <summary>
    /// Block for user to create a new login account.
    /// </summary>
    [DisplayName( "Miraki Login" )]
    [Category( "com_bricksandmortarstudio" )]
    [Description( "Block that associates a mac address with a user." )]

    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "Mac Address", required: true )]

    [LinkedPage( "Help Page", "Page to navigate to when user selects 'Help' option (if blank will use 'ForgotUserName' page route)", false, "", "", 1 )]
    [CodeEditorField( "Login Confirm Caption", "The text (HTML) to display when a user's account needs to be confirmed.", CodeEditorMode.Html, CodeEditorTheme.Rock, 100, false, @"
Thank-you for logging in, however, we need to confirm the email associated with this account belongs to you. We've sent you an email that contains a link for confirming.  Please click the link in your email to continue.
", "", 2 )]
    [LinkedPage( "Confirmation Page", "Page for user to confirm their account (if blank will use 'ConfirmAccount' page route)", false, "", "", 3 )]
    [SystemEmailField( "Confirm Account Template", "Confirm Account Email Template", false, Rock.SystemGuid.SystemEmail.SECURITY_CONFIRM_ACCOUNT, "", 4 )]
    [CodeEditorField( "Locked Out Caption", "The text (HTML) to display when a user's account has been locked.", CodeEditorMode.Html, CodeEditorTheme.Rock, 100, false, @"
Sorry, your account has been locked.  Please contact our office at {{ 'Global' | Attribute:'OrganizationPhone' }} or email {{ 'Global' | Attribute:'OrganizationEmail' }} to resolve this.  Thank-you. 
", "", 5 )]
    [BooleanField( "Hide New Account Option", "Should 'New Account' option be hidden?  For site's that require user to be in a role (Internal Rock Site for example), users shouldn't be able to create their own account.", false, "", 6, "HideNewAccount" )]
    [TextField( "New Account Text", "The text to show on the New Account button.", false, "Register", "", 7, "NewAccountButtonText" )]
    [RemoteAuthsField( "Remote Authorization Types", "Which of the active remote authorization types should be displayed as an option for user to use for authentication.", false, "", "", 8 )]
    [CodeEditorField( "Prompt Message", "Optional text (HTML) to display above username and password fields.", CodeEditorMode.Html, CodeEditorTheme.Rock, 100, false, @"", "", 9 )]

    [BooleanField( "Check for Duplicates", "Should people with the same email and last name be presented as a possible pre-existing record for user to choose from.", true, "", 0, "Duplicates" )]
    [TextField( "Found Duplicate Caption", "", false, "There are already one or more people in our system that have the same email address and last name as you do.  Are any of these people you?", "Captions", 1 )]
    [TextField( "Existing Account Caption", "", false, "{0}, you already have an existing account.  Would you like us to email you the username?", "Captions", 2 )]
    [TextField( "Sent Login Caption", "", false, "Your username has been emailed to you.  If you've forgotten your password, the email includes a link to reset your password.", "Captions", 3 )]
    [TextField( "Confirm Caption", "", false, "Because you've selected an existing person, we need to have you confirm the email address you entered belongs to you. We've sent you an email that contains a link for confirming.  Please click the link in your email to continue.", "Captions", 4 )]
    [TextField( "Success Caption", "", false, "{0}, Your account has been created", "Captions", 5 )]
    [SystemEmailField( "Forgot Username", "Forgot Username Email Template", false, Rock.SystemGuid.SystemEmail.SECURITY_FORGOT_USERNAME, "Email Templates", 8, "ForgotUsernameTemplate" )]
    [SystemEmailField( "Account Created", "Account Created Email Template", false, Rock.SystemGuid.SystemEmail.SECURITY_ACCOUNT_CREATED, "Email Templates", 10, "AccountCreatedTemplate" )]
    [DefinedValueField( "2E6540EA-63F0-40FE-BE50-F2A84735E600", "Connection Status", "The connection status to use for new individuals (default: 'Web Prospect'.)", true, false, "368DD475-242C-49C4-A42C-7278BE690CC2", order: 11 )]
    [DefinedValueField( "8522BADD-2871-45A5-81DD-C76DA07E2E7E", "Record Status", "The record status to use for new individuals (default: 'Pending'.)", true, false, "283999EC-7346-42E3-B807-BCE9B2BABB49", order: 12 )]
    [BooleanField( "Show Address", "Allows hiding the address field.", false, order: 13 )]
    [GroupLocationTypeField( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY, "Location Type",
        "The type of location that address should use.", false, Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME, "", 14 )]
    [BooleanField( "Address Required", "Whether the address is required.", false, order: 15 )]
    [BooleanField( "Show Phone Numbers", "Allows hiding the phone numbers.", false, order: 16 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE, "Phone Types", "The phone numbers to display for editing.", false, true, order: 17 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE, "Phone Types Required", "The phone numbers that are required.", false, true, order: 18 )]
    public partial class MirakiLogin : Rock.Web.UI.RockBlock
    {
        #region Fields

        private PlaceHolder[] PagePanels = new PlaceHolder[6];
        private List<Guid> _RequiredPhoneNumberGuids = new List<Guid>();

        #endregion

        #region Properties

        protected string Password
        {
            get { return ViewState["Password"] as string ?? string.Empty; }
            set { ViewState["Password"] = value; }
        }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            string macAddress = PageParameter( "client_mac" );
            var macAddressAttributeGuid = GetAttributeValue( "MacAddress" ).AsGuidOrNull();
            if ( macAddressAttributeGuid.HasValue )
            {
                var macAddressValue = new AttributeValueService( new RockContext() ).Queryable().Where( av =>
                  av.Attribute.Guid == macAddressAttributeGuid &&
                  av.Value == macAddress )
                  .FirstOrDefault();

                if ( macAddressValue != null )
                {
                    RedirectToContinueUrl();
                }
                else
                {
                    btnNewAccount.Visible = !GetAttributeValue( "HideNewAccount" ).AsBoolean();
                    btnNewAccount.Text = this.GetAttributeValue( "NewAccountButtonText" ) ?? "Register";

                    phExternalLogins.Controls.Clear();

                    int activeAuthProviders = 0;

                    var selectedGuids = new List<Guid>();
                    GetAttributeValue( "RemoteAuthorizationTypes" ).SplitDelimitedValues()
                        .ToList()
                        .ForEach( v => selectedGuids.Add( v.AsGuid() ) );

                    // Look for active external authentication providers
                    foreach ( var serviceEntry in AuthenticationContainer.Instance.Components )
                    {
                        var component = serviceEntry.Value.Value;

                        if ( component.IsActive &&
                            component.RequiresRemoteAuthentication &&
                            selectedGuids.Contains( component.EntityType.Guid ) )
                        {
                            string loginTypeName = component.GetType().Name;

                            // Check if returning from third-party authentication
                            if ( !IsPostBack && component.IsReturningFromAuthentication( Request ) )
                            {
                                string userName = string.Empty;
                                string returnUrl = string.Empty;
                                string redirectUrlSetting = LinkedPageUrl( "RedirectPage" );
                                if ( component.Authenticate( Request, out userName, out returnUrl ) )
                                {
                                    if ( !string.IsNullOrWhiteSpace( redirectUrlSetting ) )
                                    {
                                        LoginUser( userName, redirectUrlSetting, false );
                                        break;
                                    }
                                    else
                                    {
                                        LoginUser( userName, returnUrl, false );
                                        break;
                                    }
                                }
                            }

                            activeAuthProviders++;

                            LinkButton lbLogin = new LinkButton();
                            phExternalLogins.Controls.Add( lbLogin );
                            lbLogin.AddCssClass( "btn btn-authentication " + loginTypeName.ToLower() );
                            lbLogin.ID = "lb" + loginTypeName + "Login";
                            lbLogin.Click += lbLogin_Click;
                            lbLogin.CausesValidation = false;

                            if ( !string.IsNullOrWhiteSpace( component.ImageUrl() ) )
                            {
                                HtmlImage img = new HtmlImage();
                                lbLogin.Controls.Add( img );
                                img.Attributes.Add( "style", "border:none" );
                                img.Src = Page.ResolveUrl( component.ImageUrl() );
                            }
                            else
                            {
                                lbLogin.Text = loginTypeName;
                            }
                        }
                    }

                    // adjust the page if there are no social auth providers
                    if ( activeAuthProviders == 0 )
                    {
                        divSocialLogin.Visible = false;
                        divOrgLogin.RemoveCssClass( "col-sm-6" );
                        divOrgLogin.AddCssClass( "col-sm-12" );
                    }

                    lFoundDuplicateCaption.Text = GetAttributeValue( "FoundDuplicateCaption" );
                    lSentLoginCaption.Text = GetAttributeValue( "SentLoginCaption" );
                    lNewAccountConfirmCaption.Text = GetAttributeValue( "ConfirmCaption" );

                    rPhoneNumbers.ItemDataBound += rPhoneNumbers_ItemDataBound;
                }
            }
        }

        private void RedirectToContinueUrl()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( System.EventArgs e )
        {
            base.OnLoad( e );

            pnlNewAccountMessage.Controls.Clear();
            pnlNewAccountMessage.Visible = false;

            PagePanels[0] = phUserInfo;
            PagePanels[1] = phDuplicates;
            PagePanels[2] = phSendLoginInfo;
            PagePanels[3] = phSentLoginInfo;
            PagePanels[4] = phConfirmation;
            PagePanels[5] = phSuccess;

            if ( !Page.IsPostBack )
            {
                lPromptMessage.Text = GetAttributeValue( "PromptMessage" );
                tbLoginUserName.Focus();
            }

            pnlLoginMessage.Visible = false;
        }

        #endregion

        #region Events

        #region Login Panel

        protected void btnLogin_Click( object sender, EventArgs e )
        {
            if ( Page.IsValid )
            {
                var rockContext = new RockContext();
                var userLoginService = new UserLoginService( rockContext );
                var userLogin = userLoginService.GetByUserName( tbLoginUserName.Text );
                if ( userLogin != null && userLogin.EntityType != null )
                {
                    var component = AuthenticationContainer.GetComponent( userLogin.EntityType.Name );
                    if ( component != null && component.IsActive && !component.RequiresRemoteAuthentication )
                    {
                        if ( component.Authenticate( userLogin, tbLoginPassword.Text ) )
                        {
                            if ( ( userLogin.IsConfirmed ?? true ) && !( userLogin.IsLockedOut ?? false ) )
                            {
                                AddMacAddressToLogin( userLogin );
                                string returnUrl = Request.QueryString["user_continue_url"];
                                LoginUser( tbLoginUserName.Text, returnUrl, cbRememberMe.Checked );
                            }
                            else
                            {
                                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );

                                if ( userLogin.IsLockedOut ?? false )
                                {
                                    lLockedOutCaption.Text = GetAttributeValue( "LockedOutCaption" ).ResolveMergeFields( mergeFields );

                                    pnlLogin.Visible = false;
                                    pnlLockedOut.Visible = true;
                                }
                                else
                                {
                                    SendConfirmation( userLogin );

                                    lLoginConfirmCaption.Text = GetAttributeValue( "LoginConfirmCaption" ).ResolveMergeFields( mergeFields );

                                    pnlLogin.Visible = false;
                                    pnlConfirmation.Visible = true;
                                }
                            }

                            return;
                        }
                    }
                }
            }

            string helpUrl = string.Empty;

            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "HelpPage" ) ) )
            {
                helpUrl = LinkedPageUrl( "HelpPage" );
            }
            else
            {
                helpUrl = ResolveRockUrl( "~/ForgotUserName" );
            }

            DisplayError( string.Format( "Sorry, we couldn't find an account matching that username/password. Can we help you <a href='{0}'>recover your account information</a>?", helpUrl ) );
        }

        private void AddMacAddressToLogin( UserLogin userLogin )
        {
            var person = userLogin.Person;
            person.LoadAttributes();
            person.SetAttributeValue( "MacAddress", PageParameter( "client_mac" ) );
            person.SaveAttributeValues();
        }

        protected void lbLogin_Click( object sender, EventArgs e )
        {
            if ( sender is LinkButton )
            {
                LinkButton lb = (LinkButton)sender;

                foreach ( var serviceEntry in AuthenticationContainer.Instance.Components )
                {
                    var component = serviceEntry.Value.Value;
                    if ( component.IsActive && component.RequiresRemoteAuthentication )
                    {
                        string loginTypeName = component.GetType().Name;
                        if ( lb.ID == "lb" + loginTypeName + "Login" )
                        {
                            Uri uri = component.GenerateLoginUrl( Request );
                            if ( uri != null )
                            {
                                Response.Redirect( uri.AbsoluteUri, false );
                                Context.ApplicationInstance.CompleteRequest();
                                return;
                            }
                            else
                            {
                                DisplayError( string.Format( "ERROR: {0} does not have a remote login URL", loginTypeName ) );
                            }
                        }
                    }
                }
            }
        }

        protected void btnHelp_Click( object sender, EventArgs e )
        {
            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "HelpPage" ) ) )
            {
                NavigateToLinkedPage( "HelpPage" );
            }
            else
            {
                Response.Redirect( "~/ForgotUserName", false );
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        protected void btnNewAccount_Click( object sender, EventArgs e )
        {
            pnlLogin.Visible = false;
            pnlNewAccount.Visible = true;

            DisplayUserInfo( Direction.Forward );

            // show/hide address and phone panels
            pnlAddress.Visible = GetAttributeValue( "ShowAddress" ).AsBoolean();
            pnlPhoneNumbers.Visible = GetAttributeValue( "ShowPhoneNumbers" ).AsBoolean();
            acAddress.Required = GetAttributeValue( "AddressRequired" ).AsBoolean();

            var phoneNumbers = new List<PhoneNumber>();

            // add phone number types
            if ( pnlPhoneNumbers.Visible )
            {
                var phoneNumberTypeDefinedType = DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE ) );

                if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "PhoneTypes" ) ) )
                {
                    var selectedPhoneTypeGuids = GetAttributeValue( "PhoneTypes" ).Split( ',' ).Select( Guid.Parse ).ToList();
                    var selectedPhoneTypes = phoneNumberTypeDefinedType.DefinedValues
                        .Where( v => selectedPhoneTypeGuids.Contains( v.Guid ) )
                        .ToList();

                    foreach ( var phoneNumberType in selectedPhoneTypes )
                    {
                        var numberType = new DefinedValue();
                        numberType.Id = phoneNumberType.Id;
                        numberType.Value = phoneNumberType.Value;
                        numberType.Guid = phoneNumberType.Guid;

                        var phoneNumber = new PhoneNumber { NumberTypeValueId = numberType.Id, NumberTypeValue = numberType };

                        phoneNumbers.Add( phoneNumber );
                    }

                    if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "PhoneTypesRequired" ) ) )
                    {
                        _RequiredPhoneNumberGuids = GetAttributeValue( "PhoneTypesRequired" ).Split( ',' ).Select( Guid.Parse ).ToList();
                    }

                    rPhoneNumbers.DataSource = phoneNumbers;
                    rPhoneNumbers.DataBind();
                }
            }

        }

        #endregion

        #region User Info Panel

        /// <summary>
        /// Handles the ItemDataBound event of the rPhoneNumbers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        void rPhoneNumbers_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var pnbPhone = e.Item.FindControl( "pnbPhone" ) as PhoneNumberBox;
            if ( pnbPhone != null )
            {
                pnbPhone.ValidationGroup = BlockValidationGroup;
                var phoneNumber = e.Item.DataItem as PhoneNumber;
                if ( phoneNumber != null )
                {
                    pnbPhone.Required = _RequiredPhoneNumberGuids.Contains( phoneNumber.NumberTypeValue.Guid );
                    pnbPhone.RequiredErrorMessage = string.Format( "{0} phone is required", phoneNumber.NumberTypeValue.Value );
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnUserInfoNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnUserInfoNext_Click( object sender, EventArgs e )
        {
            Password = tbNewAccountPassword.Text;

            if ( Page.IsValid )
            {
                if ( UserLoginService.IsPasswordValid( tbNewAccountPassword.Text ) )
                {
                    var userLoginService = new Rock.Model.UserLoginService( new RockContext() );
                    var userLogin = userLoginService.GetByUserName( tbNewAccountUserName.Text );

                    if ( userLogin == null )
                    {
                        DisplayDuplicates( Direction.Forward );
                    }
                    else
                    {
                        ShowErrorMessage( "Username already exists" );
                    }
                }
                else
                {
                    ShowErrorMessage( UserLoginService.FriendlyPasswordRules() );
                }
            }
        }

        #endregion

        #region Duplicates Panel

        /// <summary>
        /// Handles the Click event of the btnDuplicatesPrev control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnDuplicatesPrev_Click( object sender, EventArgs e )
        {
            DisplayUserInfo( Direction.Back );
        }

        /// <summary>
        /// Handles the Click event of the btnDuplicatesNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnDuplicatesNext_Click( object sender, EventArgs e )
        {
            int personId = Request.Form["DuplicatePerson"].AsInteger();
            if ( personId > 0 )
            {
                var userLoginService = new Rock.Model.UserLoginService( new RockContext() );
                var userLogins = userLoginService.GetByPersonId( personId ).ToList();
                if ( userLogins.Count > 0 )
                {
                    DisplaySendLogin( personId, Direction.Forward );
                }
                else
                {
                    DisplayConfirmation( personId );
                }
            }
            else
            {
                DisplaySuccess( CreateUser( CreatePerson(), true ) );
            }
        }

        #endregion

        #region Send Login Panel

        /// <summary>
        /// Handles the Click event of the btnSendPrev control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSendPrev_Click( object sender, EventArgs e )
        {
            DisplayDuplicates( Direction.Back );
        }

        /// <summary>
        /// Handles the Click event of the btnSendYes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSendYes_Click( object sender, EventArgs e )
        {
            DisplaySentLogin( Direction.Forward );
        }

        /// <summary>
        /// Handles the Click event of the btnSendLogin control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSendLogin_Click( object sender, EventArgs e )
        {
            string loginUrl = LinkedPageUrl( "LoginPage" );
            if ( string.IsNullOrWhiteSpace( loginUrl ) )
            {
                loginUrl = ResolveRockUrl( "~/Login" );
            }

            string returnUrl = Request.QueryString["returnurl"];
            if ( !string.IsNullOrWhiteSpace( returnUrl ) && !loginUrl.Contains( "returnurl" ) )
            {
                string delimiter = "?";
                if ( loginUrl.Contains( '?' ) )
                {
                    delimiter = "&";
                }

                loginUrl += delimiter + "returnurl=" + returnUrl;
            }

            Response.Redirect( loginUrl, false );
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void btnContinue_Click( object sender, EventArgs e )
        {
            string returnUrl = Request.QueryString["user_continue_url"];
            if ( !string.IsNullOrWhiteSpace( returnUrl ) )
            {
                Response.Redirect( Server.UrlDecode( returnUrl ), false );
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        #endregion

        #endregion

        #region Methods

        #region Login Methods

        /// <summary>
        /// Displays the error.
        /// </summary>
        /// <param name="message">The message.</param>
        private void DisplayError( string message )
        {
            pnlLoginMessage.Controls.Clear();
            pnlLoginMessage.Controls.Add( new LiteralControl( message ) );
            pnlLoginMessage.Visible = true;
        }

        /// <summary>
        /// Logs in the user.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <param name="rememberMe">if set to <c>true</c> [remember me].</param>
        private void LoginUser( string userName, string returnUrl, bool rememberMe )
        {
            string redirectUrlSetting = LinkedPageUrl( "RedirectPage" );

            UserLoginService.UpdateLastLogin( userName );

            Rock.Security.Authorization.SetAuthCookie( userName, rememberMe, false );

            if ( !string.IsNullOrWhiteSpace( returnUrl ) )
            {
                string redirectUrl = Server.UrlDecode( returnUrl );
                Response.Redirect( redirectUrl );
                Context.ApplicationInstance.CompleteRequest();
            }
            else if ( !string.IsNullOrWhiteSpace( redirectUrlSetting ) )
            {
                Response.Redirect( redirectUrlSetting );
                Context.ApplicationInstance.CompleteRequest();
            }
            else
            {
                RockPage.Layout.Site.RedirectToDefaultPage();
            }
        }

        /// <summary>
        /// Sends the confirmation.
        /// </summary>
        /// <param name="userLogin">The user login.</param>
        private void SendConfirmation( UserLogin userLogin )
        {
            string url = LinkedPageUrl( "ConfirmationPage" );
            if ( string.IsNullOrWhiteSpace( url ) )
            {
                url = ResolveRockUrl( "~/ConfirmAccount" );
            }

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
            mergeFields.Add( "ConfirmAccountUrl", RootPath + url.TrimStart( new char[] { '/' } ) );

            var personDictionary = userLogin.Person.ToLiquid() as Dictionary<string, object>;
            mergeFields.Add( "Person", personDictionary );
            mergeFields.Add( "User", userLogin );

            var recipients = new List<RecipientData>();
            recipients.Add( new RecipientData( userLogin.Person.Email, mergeFields ) );

            Email.Send( GetAttributeValue( "ConfirmAccountTemplate" ).AsGuid(), recipients, ResolveRockUrl( "~/" ), ResolveRockUrl( "~~/" ), false );
        }

        #endregion

        #region New Account Methods

        /// <summary>
        /// Shows the error message.
        /// </summary>
        /// <param name="message">The message.</param>
        private void ShowErrorMessage( string message )
        {
            pnlNewAccountMessage.Controls.Add( new LiteralControl( message ) );
            pnlNewAccountMessage.Visible = true;
        }



        /// <summary>
        /// Displays the user information.
        /// </summary>
        /// <param name="direction">The direction.</param>
        private void DisplayUserInfo( Direction direction )
        {
            ShowPanel( 0 );
        }

        /// <summary>
        /// Displays the duplicates.
        /// </summary>
        /// <param name="direction">The direction.</param>
        private void DisplayDuplicates( Direction direction )
        {
            bool displayed = false;

            if ( Convert.ToBoolean( GetAttributeValue( "Duplicates" ) ) )
            {
                PersonService personService = new PersonService( new RockContext() );
                var matches = personService.Queryable().Where( p =>
                        p.Email.ToLower() == tbEmail.Text.ToLower() && p.LastName.ToLower() == tbLastName.Text.ToLower() ).ToList();

                if ( matches.Count > 0 )
                {
                    gDuplicates.AllowPaging = false;
                    gDuplicates.ShowActionRow = false;

                    gDuplicates.DataSource = matches;
                    gDuplicates.DataBind();

                    ShowPanel( 1 );

                    displayed = true;
                }
                else
                {
                    displayed = false;
                }
            }

            if ( !displayed )
            {
                if ( direction == Direction.Forward )
                {
                    DisplaySuccess( CreateUser( CreatePerson(), true ) );
                }
                else
                {
                    DisplayUserInfo( direction );
                }
            }
        }

        /// <summary>
        /// Displays the send login.
        /// </summary>
        /// <param name="personId">The person identifier.</param>
        /// <param name="direction">The direction.</param>
        private void DisplaySendLogin( int personId, Direction direction )
        {
            hfSendPersonId.Value = personId.ToString();

            lExistingAccountCaption.Text = GetAttributeValue( "ExistingAccountCaption" );
            if ( lExistingAccountCaption.Text.Contains( "{0}" ) )
            {
                PersonService personService = new PersonService( new RockContext() );
                Person person = personService.Get( personId );
                if ( person != null )
                {
                    lExistingAccountCaption.Text = string.Format( lExistingAccountCaption.Text, person.FirstName );
                }
            }

            ShowPanel( 2 );
        }

        /// <summary>
        /// Displays the sent login.
        /// </summary>
        /// <param name="direction">The direction.</param>
        private void DisplaySentLogin( Direction direction )
        {
            var rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );
            Person person = personService.Get( hfSendPersonId.Value.AsInteger() );
            if ( person != null )
            {
                string url = LinkedPageUrl( "ConfirmationPage" );
                if ( string.IsNullOrWhiteSpace( url ) )
                {
                    url = ResolveRockUrl( "~/ConfirmAccount" );
                }

                var mergeObjects = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                mergeObjects.Add( "ConfirmAccountUrl", RootPath + url.TrimStart( new char[] { '/' } ) );
                var results = new List<IDictionary<string, object>>();

                var users = new List<UserLogin>();
                var userLoginService = new UserLoginService( rockContext );
                foreach ( UserLogin user in userLoginService.GetByPersonId( person.Id ) )
                {
                    if ( user.EntityType != null )
                    {
                        var component = AuthenticationContainer.GetComponent( user.EntityType.Name );
                        if ( component.ServiceType == AuthenticationServiceType.Internal )
                        {
                            users.Add( user );
                        }
                    }
                }

                var resultsDictionary = new Dictionary<string, object>();
                resultsDictionary.Add( "Person", person );
                resultsDictionary.Add( "Users", users );
                results.Add( resultsDictionary );

                mergeObjects.Add( "Results", results.ToArray() );

                var recipients = new List<RecipientData>();
                recipients.Add( new RecipientData( person.Email, mergeObjects ) );

                Email.Send( GetAttributeValue( "ForgotUsernameTemplate" ).AsGuid(), recipients, ResolveRockUrl( "~/" ), ResolveRockUrl( "~~/" ), false );
            }
            else
            {
                ShowErrorMessage( "Invalid Person" );
            }

            ShowPanel( 3 );
        }

        /// <summary>
        /// Displays the confirmation.
        /// </summary>
        /// <param name="personId">The person identifier.</param>
        private void DisplayConfirmation( int personId )
        {
            PersonService personService = new PersonService( new RockContext() );
            Person person = personService.Get( personId );

            if ( person != null )
            {
                Rock.Model.UserLogin user = CreateUser( person, false );

                string url = LinkedPageUrl( "ConfirmationPage" );
                if ( string.IsNullOrWhiteSpace( url ) )
                {
                    url = ResolveRockUrl( "~/ConfirmAccount" );
                }

                var mergeObjects = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                mergeObjects.Add( "ConfirmAccountUrl", RootPath + url.TrimStart( new char[] { '/' } ) );
                mergeObjects.Add( "Person", person );
                mergeObjects.Add( "User", user );

                var recipients = new List<RecipientData>();
                recipients.Add( new RecipientData( person.Email, mergeObjects ) );

                Email.Send( GetAttributeValue( "ConfirmAccountTemplate" ).AsGuid(), recipients, ResolveRockUrl( "~/" ), ResolveRockUrl( "~~/" ), false );

                ShowPanel( 4 );
            }
            else
            {
                ShowErrorMessage( "Invalid Person" );
            }
        }

        /// <summary>
        /// Displays the success.
        /// </summary>
        /// <param name="user">The user.</param>
        private void DisplaySuccess( Rock.Model.UserLogin user )
        {
            FormsAuthentication.SignOut();
            Rock.Security.Authorization.SetAuthCookie( tbNewAccountUserName.Text, false, false );

            if ( user != null && user.PersonId.HasValue )
            {
                PersonService personService = new PersonService( new RockContext() );
                Person person = personService.Get( user.PersonId.Value );
                AddMacAddressToLogin( user );
                if ( person != null )
                {
                    try
                    {
                        string url = LinkedPageUrl( "ConfirmationPage" );
                        if ( string.IsNullOrWhiteSpace( url ) )
                        {
                            url = ResolveRockUrl( "~/ConfirmAccount" );
                        }

                        var mergeObjects = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                        mergeObjects.Add( "ConfirmAccountUrl", RootPath + url.TrimStart( new char[] { '/' } ) );
                        mergeObjects.Add( "Person", person );
                        mergeObjects.Add( "User", user );

                        var recipients = new List<RecipientData>();
                        recipients.Add( new RecipientData( person.Email, mergeObjects ) );

                        Email.Send( GetAttributeValue( "AccountCreatedTemplate" ).AsGuid(), recipients, ResolveRockUrl( "~/" ), ResolveRockUrl( "~~/" ), false );
                    }
                    catch ( SystemException ex )
                    {
                        ExceptionLogService.LogException( ex, Context, RockPage.PageId, RockPage.Site.Id, CurrentPersonAlias );
                    }

                    string returnUrl = Request.QueryString["user_continue_url"];
                    btnContinue.Visible = !string.IsNullOrWhiteSpace( returnUrl );

                    lSuccessCaption.Text = GetAttributeValue( "SuccessCaption" );
                    if ( lSuccessCaption.Text.Contains( "{0}" ) )
                    {
                        lSuccessCaption.Text = string.Format( lSuccessCaption.Text, person.FirstName );
                    }

                    ShowPanel( 5 );

                    // Wait 5 seconds
                }
                else
                {
                    ShowErrorMessage( "Invalid Person" );
                }
            }
            else
            {
                ShowErrorMessage( "Invalid User" );
            }
        }

        /// <summary>
        /// Shows the panel.
        /// </summary>
        /// <param name="panel">The panel.</param>
        private void ShowPanel( int panel )
        {
            for ( int i = 0; i < PagePanels.Length; i++ )
            {
                PagePanels[i].Visible = i == panel;
            }
        }

        /// <summary>
        /// Creates the person.
        /// </summary>
        /// <returns></returns>
        private Person CreatePerson()
        {
            var rockContext = new RockContext();

            DefinedValueCache dvcConnectionStatus = DefinedValueCache.Read( GetAttributeValue( "ConnectionStatus" ).AsGuid() );
            DefinedValueCache dvcRecordStatus = DefinedValueCache.Read( GetAttributeValue( "RecordStatus" ).AsGuid() );

            Person person = new Person();
            person.FirstName = tbFirstName.Text;
            person.LastName = tbLastName.Text;
            person.Email = tbEmail.Text;
            person.IsEmailActive = true;
            person.EmailPreference = EmailPreference.EmailAllowed;
            person.RecordTypeValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
            if ( dvcConnectionStatus != null )
            {
                person.ConnectionStatusValueId = dvcConnectionStatus.Id;
            }

            if ( dvcRecordStatus != null )
            {
                person.RecordStatusValueId = dvcRecordStatus.Id;
            }

            switch ( ddlGender.SelectedValue )
            {
                case "M":
                    person.Gender = Gender.Male;
                    break;
                case "F":
                    person.Gender = Gender.Female;
                    break;
                default:
                    person.Gender = Gender.Unknown;
                    break;
            }

            var birthday = bdaypBirthDay.SelectedDate;
            if ( birthday.HasValue )
            {
                person.BirthMonth = birthday.Value.Month;
                person.BirthDay = birthday.Value.Day;
                if ( birthday.Value.Year != DateTime.MinValue.Year )
                {
                    person.BirthYear = birthday.Value.Year;
                }
            }

            bool smsSelected = false;

            foreach ( RepeaterItem item in rPhoneNumbers.Items )
            {
                HiddenField hfPhoneType = item.FindControl( "hfPhoneType" ) as HiddenField;
                PhoneNumberBox pnbPhone = item.FindControl( "pnbPhone" ) as PhoneNumberBox;
                CheckBox cbUnlisted = item.FindControl( "cbUnlisted" ) as CheckBox;
                CheckBox cbSms = item.FindControl( "cbSms" ) as CheckBox;

                if ( !string.IsNullOrWhiteSpace( PhoneNumber.CleanNumber( pnbPhone.Number ) ) )
                {
                    int phoneNumberTypeId;
                    if ( int.TryParse( hfPhoneType.Value, out phoneNumberTypeId ) )
                    {
                        var phoneNumber = new PhoneNumber { NumberTypeValueId = phoneNumberTypeId };
                        person.PhoneNumbers.Add( phoneNumber );
                        phoneNumber.CountryCode = PhoneNumber.CleanNumber( pnbPhone.CountryCode );
                        phoneNumber.Number = PhoneNumber.CleanNumber( pnbPhone.Number );

                        // Only allow one number to have SMS selected
                        if ( smsSelected )
                        {
                            phoneNumber.IsMessagingEnabled = false;
                        }
                        else
                        {
                            phoneNumber.IsMessagingEnabled = cbSms.Checked;
                            smsSelected = cbSms.Checked;
                        }

                        phoneNumber.IsUnlisted = cbUnlisted.Checked;
                    }
                }
            }

            PersonService.SaveNewPerson( person, rockContext, null, false );

            // save address
            if ( pnlAddress.Visible )
            {
                if ( acAddress.IsValid && !string.IsNullOrWhiteSpace( acAddress.Street1 ) && !string.IsNullOrWhiteSpace( acAddress.City ) && !string.IsNullOrWhiteSpace( acAddress.PostalCode ) )
                {
                    Guid locationTypeGuid = GetAttributeValue( "LocationType" ).AsGuid();
                    if ( locationTypeGuid != Guid.Empty )
                    {
                        Guid familyGroupTypeGuid = Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid();
                        GroupService groupService = new GroupService( rockContext );
                        GroupLocationService groupLocationService = new GroupLocationService( rockContext );
                        var family = groupService.Queryable().Where( g => g.GroupType.Guid == familyGroupTypeGuid && g.Members.Any( m => m.PersonId == person.Id ) ).FirstOrDefault();

                        var groupLocation = new GroupLocation();
                        groupLocation.GroupId = family.Id;
                        groupLocationService.Add( groupLocation );

                        var location = new LocationService( rockContext ).Get( acAddress.Street1, acAddress.Street2, acAddress.City, acAddress.State, acAddress.PostalCode, acAddress.Country );
                        groupLocation.Location = location;

                        groupLocation.GroupLocationTypeValueId = DefinedValueCache.Read( locationTypeGuid ).Id;
                        groupLocation.IsMailingLocation = true;
                        groupLocation.IsMappedLocation = true;

                        rockContext.SaveChanges();
                    }
                }
            }

            return person;
        }

        /// <summary>
        /// Creates the user.
        /// </summary>
        /// <param name="person">The person.</param>
        /// <param name="confirmed">if set to <c>true</c> [confirmed].</param>
        /// <returns></returns>
        private Rock.Model.UserLogin CreateUser( Person person, bool confirmed )
        {
            var rockContext = new RockContext();
            var userLoginService = new Rock.Model.UserLoginService( rockContext );
            return UserLoginService.Create(
                rockContext,
                person,
                Rock.Model.AuthenticationServiceType.Internal,
                EntityTypeCache.Read( Rock.SystemGuid.EntityType.AUTHENTICATION_DATABASE.AsGuid() ).Id,
                tbNewAccountUserName.Text,
                Password,
                confirmed );
        }
        #endregion

        #endregion

        #region Enumerations

        private enum Direction
        {
            Forward,
            Back
        }

        #endregion
    }
}