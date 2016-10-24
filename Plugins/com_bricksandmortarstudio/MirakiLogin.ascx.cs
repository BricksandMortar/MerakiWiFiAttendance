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

    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "Mac Address", required: true, defaultValue: "E4A05461-81EE-490C-8866-DAF5D86FDC3E", order: 0 )]

    [DefinedValueField( "2E6540EA-63F0-40FE-BE50-F2A84735E600", "Connection Status", "The connection status to use for new individuals (default: 'Web Prospect'.)", true, false, "368DD475-242C-49C4-A42C-7278BE690CC2", order: 1 )]
    [DefinedValueField( "8522BADD-2871-45A5-81DD-C76DA07E2E7E", "Record Status", "The record status to use for new individuals (default: 'Pending'.)", true, false, "283999EC-7346-42E3-B807-BCE9B2BABB49", order: 2 )]
    [TextField( "Success Caption", "", false, "{0}, Your account has been created", "Captions", 3 )]

    [CodeEditorField( "Heading Caption", "", Rock.Web.UI.Controls.CodeEditorMode.Html, Rock.Web.UI.Controls.CodeEditorTheme.Rock, 200, false, "<div class='alert alert-info'>Enter your email address below and we'll send your account information to you right away.</div>", "Captions", 4 )]
    [CodeEditorField( "Invalid Email Caption", "", Rock.Web.UI.Controls.CodeEditorMode.Html, Rock.Web.UI.Controls.CodeEditorTheme.Rock, 200, false, "Sorry, we could not find an account for the email address you entered.", "Captions", 5 )]
    [CodeEditorField( "Lost Username Success Caption", "", Rock.Web.UI.Controls.CodeEditorMode.Html, Rock.Web.UI.Controls.CodeEditorTheme.Rock, 200, false, "Your user name has been sent with instructions on how to change your password if needed.", "Captions", 6 )]

    [SystemEmailField( "Account Created", "Account Created Email Template", false, Rock.SystemGuid.SystemEmail.SECURITY_ACCOUNT_CREATED, "Email Templates", 7, "AccountCreatedTemplate" )]
    [SystemEmailField( "Forgot Username Email Template", "Email Template to send", false, Rock.SystemGuid.SystemEmail.SECURITY_FORGOT_USERNAME, "", 8, "EmailTemplate" )]

    public partial class MirakiLogin : Rock.Web.UI.RockBlock
    {
        #region Fields

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

            if ( !Page.IsPostBack )
            {
                string macAddress = PageParameter( "client_mac" );
                var macAddressAttributeGuid = GetAttributeValue( "MacAddress" ).AsGuidOrNull();
                if ( macAddressAttributeGuid.HasValue )
                {
                    var macAddressValue = new AttributeValueService( new RockContext() ).Queryable().Where( av =>
                      av.Attribute.Guid == macAddressAttributeGuid &&
                      av.Value == macAddress )
                      .FirstOrDefault();

                    if ( macAddressValue != null && !String.IsNullOrWhiteSpace( macAddressValue.Value ) )
                    {
                        RedirectToContinueUrl();
                    }
                }

                tbLoginUserName.Focus();
            }

            pnlLoginMessage.Visible = false;
        }

        #endregion

        #region Events

        #region Login Panel

        /// <summary>
        /// Handles the Click event of the btnLogin control.
        /// This is the Login button for the Login screen.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
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
                            AddMacAddressToLogin( userLogin );
                            LoginUser( tbLoginUserName.Text, null, cbRememberMe.Checked );

                            return;
                        }
                    }
                }
            }

            pnlLoginMessage.Controls.Clear();
            pnlLoginMessage.Controls.Add( new LiteralControl( "Sorry, we couldn't find an account matching that username/password" ) );
            pnlLoginMessage.Visible = true;
        }

        protected void btnHelp_Click( object sender, EventArgs e )
        {
            pnlEntry.Visible = true;
            pnlWarning.Visible = false;
            pnlSuccess.Visible = false;
            pnlLogin.Visible = false;

            lCaption.Text = GetAttributeValue( "HeadingCaption" );
            lWarning.Text = GetAttributeValue( "InvalidEmailCaption" );
            lSuccess.Text = GetAttributeValue( "LostUsernameSuccessCaption" );
        }

        protected void btnNewAccount_Click( object sender, EventArgs e )
        {
            pnlLogin.Visible = false;
            pnlNewAccount.Visible = true;

            phUserInfo.Visible = true;
        }

        #endregion

        #region User Info Panel        

        /// <summary>
        /// Handles the Click event of the btnRegister control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnRegister_Click( object sender, EventArgs e )
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
                        DisplaySuccess( CreateUser( CreatePerson(), true ) );
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

        protected void btnRegisterBack_Click( object sender, EventArgs e )
        {
            pnlNewAccount.Visible = false;
            pnlLogin.Visible = true;
        }

        #endregion        

        #region Forgot Password Panel

        protected void btnSend_Click( object sender, EventArgs e )
        {

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
            var results = new List<IDictionary<string, object>>();

            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            var userLoginService = new UserLoginService( rockContext );

            bool hasAccountWithPasswordResetAbility = false;
            List<string> accountTypes = new List<string>();

            foreach ( Person person in personService.GetByEmail( tbEmail.Text )
                .Where( p => p.Users.Any() ) )
            {
                var users = new List<UserLogin>();
                foreach ( UserLogin user in userLoginService.GetByPersonId( person.Id ) )
                {
                    if ( user.EntityType != null )
                    {
                        var component = AuthenticationContainer.GetComponent( user.EntityType.Name );
                        if ( !component.RequiresRemoteAuthentication )
                        {
                            users.Add( user );
                            hasAccountWithPasswordResetAbility = true;
                        }

                        accountTypes.Add( user.EntityType.FriendlyName );
                    }
                }

                var resultsDictionary = new Dictionary<string, object>();
                resultsDictionary.Add( "Person", person );
                resultsDictionary.Add( "Users", users );
                results.Add( resultsDictionary );
            }

            if ( results.Count > 0 && hasAccountWithPasswordResetAbility )
            {
                mergeFields.Add( "Results", results.ToArray() );
                var recipients = new List<RecipientData>();
                recipients.Add( new RecipientData( tbEmail.Text, mergeFields ) );

                Email.Send( GetAttributeValue( "EmailTemplate" ).AsGuid(), recipients, ResolveRockUrlIncludeRoot( "~/" ), ResolveRockUrlIncludeRoot( "~~/" ), false );

                pnlEntry.Visible = false;
                pnlSuccess.Visible = true;
                pnlLogin.Visible = true;
            }
            else if ( results.Count > 0 )
            {
                // the person has user accounts but none of them are allowed to have their passwords reset (Facebook/Google/etc)

                lWarning.Text = string.Format( @"<p>We were able to find the following accounts for this email, but 
                                                none of them are able to be reset from this website.</p> <p>Accounts:<br /> {0}</p>"
                                    , string.Join( ",", accountTypes ) );
                pnlWarning.Visible = true;

                System.Threading.Thread.Sleep( 7000 );
                pnlWarning.Visible = false;
                pnlEntry.Visible = false;
                pnlLogin.Visible = true;
            }
            else
            {
                pnlWarning.Visible = true;
            }
        }

        /// <summary>
        /// Handles the Click event of the btnBack control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnBack_Click( object sender, EventArgs e )
        {
            pnlEntry.Visible = false;
            pnlLogin.Visible = true;
        }

        #endregion

        #endregion

        #region Methods

        #region Login Methods        

        /// <summary>
        /// Logs in the user.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <param name="rememberMe">if set to <c>true</c> [remember me].</param>
        private void LoginUser( string userName, string returnUrl, bool rememberMe )
        {
            UserLoginService.UpdateLastLogin( userName );

            Rock.Security.Authorization.SetAuthCookie( userName, rememberMe, false );

            RedirectToContinueUrl();
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
                        string url = ResolveRockUrl( "~/ConfirmAccount" );

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

                    lSuccessCaption.Text = GetAttributeValue( "SuccessCaption" );
                    if ( lSuccessCaption.Text.Contains( "{0}" ) )
                    {
                        lSuccessCaption.Text = string.Format( lSuccessCaption.Text, person.FirstName );
                    }

                    phUserInfo.Visible = false;
                    phSuccess.Visible = true;

                    System.Threading.Thread.Sleep( 5000 );
                    RedirectToContinueUrl();

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

            PersonService.SaveNewPerson( person, rockContext, null, false );

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

        /// <summary>
        /// Adds the mac address to login.
        /// </summary>
        /// <param name="userLogin">The user login.</param>
        private void AddMacAddressToLogin( UserLogin userLogin )
        {
            var person = userLogin.Person;
            person.LoadAttributes();
            person.SetAttributeValue( "MacAddress", PageParameter( "client_mac" ) );
            person.SaveAttributeValues();
        }

        /// <summary>
        /// Redirects to continue URL.
        /// </summary>
        private void RedirectToContinueUrl()
        {
            var continueUrl = String.Format( "{0}?continue_url={1}", PageParameter( "base_grant_url" ), PageParameter( "user_continue_url" ) );
            Response.Redirect( continueUrl, false );
            Context.ApplicationInstance.CompleteRequest();
        }

        #endregion
        
    }
}