
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BlankApp.Constants;
using BlankApp.LogInClasses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xamarin.Auth;
using Xamarin.Forms;
using BlankApp.LogInClasses.Apple;
using System.Collections.Generic;


namespace BlankApp
{
    public partial class LogIn : ContentPage
    {
        public HttpClient client = new HttpClient();
        public static string accessToken = null;
        public static string refreshToken = null;
        public static string uid = null;
        public event EventHandler SignIn;

        public LogIn()
        {
            InitializeComponent();

            // APPLE
            var vm = new LoginViewModel();
            vm.AppleError += AppleError;
            BindingContext = vm;

            if(Device.RuntimePlatform == Device.Android)
            {
                appleButton.IsEnabled = false;
            }
        }

        private async void AppleError(object sender, EventArgs e)
        {
            await DisplayAlert("Error", "We weren't able to set an account for you", "OK");
        }

        async void SignInClick(System.Object sender, System.EventArgs e)
        {
            logInButton.IsEnabled = false;
            if (String.IsNullOrEmpty(userEmailAddress.Text) || String.IsNullOrEmpty(userPassword.Text))
            { // check if all fields are filled out
                await DisplayAlert("Error", "Please fill in all fields", "OK");
                logInButton.IsEnabled = true;
            }
            else
            {
                var accountSalt = await retrieveAccountSalt(userEmailAddress.Text.ToLower().Trim());

                if (accountSalt != null)
                {
                    var loginAttempt = await LogInUser(userEmailAddress.Text.ToLower(), userPassword.Text, accountSalt);

                    if (loginAttempt != null && loginAttempt.message != "Request failed, wrong password.")
                    {
                        System.Diagnostics.Debug.WriteLine("USER'S DATA");
                        System.Diagnostics.Debug.WriteLine("USER CUSTOMER_UID: " + loginAttempt.result[0].customer_uid);
                        System.Diagnostics.Debug.WriteLine("USER FIRST NAME: " + loginAttempt.result[0].customer_first_name);
                        System.Diagnostics.Debug.WriteLine("USER LAST NAME: " + loginAttempt.result[0].customer_last_name);
                        System.Diagnostics.Debug.WriteLine("USER EMAIL: " + loginAttempt.result[0].customer_email);
                        System.Diagnostics.Debug.WriteLine("USER SOCIAL MEDIA: " + loginAttempt.result[0].user_social_media);
                        System.Diagnostics.Debug.WriteLine("USER ACCESS TOKEN: " + loginAttempt.result[0].user_access_token);
                        System.Diagnostics.Debug.WriteLine("USER REFRESH TOKEN: " + loginAttempt.result[0].user_refresh_token);

                        Application.Current.MainPage = new HomePage();
                        Application.Current.Properties["social"] = "FALSE";
                    }
                    else
                    {
                        await DisplayAlert("Error", "Wrong password was entered", "OK");
                        logInButton.IsEnabled = true;
                    }
                }
                else
                {
                    await DisplayAlert("Error", "An account with that email does not exist. We are going to send you to our sign up page", "OK");
                    Application.Current.MainPage = new SignUps();
                    logInButton.IsEnabled = true;
                }
            }
        }

        private async Task<AccountSalt> retrieveAccountSalt(string userEmail)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(userEmail);

                SaltPost saltPost = new SaltPost();
                saltPost.email = userEmail;

                var saltPostSerilizedObject = JsonConvert.SerializeObject(saltPost);
                var saltPostContent = new StringContent(saltPostSerilizedObject, Encoding.UTF8, "application/json");

                var client = new HttpClient();
                var DRSResponse = await client.PostAsync(Constant.AccountSaltUrl, saltPostContent);

                AccountSalt userInformation = null;
                if (DRSResponse.IsSuccessStatusCode)
                {
                    var result = await DRSResponse.Content.ReadAsStringAsync();

                    AcountSaltCredentials data = new AcountSaltCredentials();
                    data = JsonConvert.DeserializeObject<AcountSaltCredentials>(result);

                    userInformation = new AccountSalt
                    {
                        password_algorithm = data.result[0].password_algorithm,
                        password_salt = data.result[0].password_salt
                    };
                }

                return userInformation;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return null;
            }
        }

        public async Task<LogInResponse> LogInUser(string userEmail, string userPassword, AccountSalt accountSalt)
        {
            try
            {
                SHA512 sHA512 = new SHA512Managed();
                byte[] data = sHA512.ComputeHash(Encoding.UTF8.GetBytes(userPassword + accountSalt.password_salt)); // take the password and account salt to generate hash
                string hashedPassword = BitConverter.ToString(data).Replace("-", string.Empty).ToLower(); // convert hash to hex

                LogInPost loginPostContent = new LogInPost();
                loginPostContent.email = userEmail;
                loginPostContent.password = hashedPassword;

                string loginPostContentJson = JsonConvert.SerializeObject(loginPostContent); // make orderContent into json

                var httpContent = new StringContent(loginPostContentJson, Encoding.UTF8, "application/json"); // encode orderContentJson into format to send to database
                var response = await client.PostAsync(Constant.LogInUrl, httpContent); // try to post to database
                

                if (response.Content != null)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonConvert.DeserializeObject<LogInResponse>(responseContent);
                    return loginResponse;
                }
                return null;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception message: " + e.Message);
                return null;
            }
        }

        public void FacebookSignInClick(System.Object sender, System.EventArgs e)
        {
            string clientID = string.Empty;
            string redirectURL = string.Empty;

            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    clientID = Constant.FacebookiOSClientID;
                    redirectURL = Constant.FacebookiOSRedirectUrl;
                    break;
                case Device.Android:
                    clientID = Constant.FacebookAndroidClientID;
                    redirectURL = Constant.FacebookAndroidRedirectUrl;
                    break;
            }

            var authenticator = new OAuth2Authenticator(clientID, Constant.FacebookScope, new Uri(Constant.FacebookAuthorizeUrl), new Uri(redirectURL), null, false);
            var presenter = new Xamarin.Auth.Presenters.OAuthLoginPresenter();

            authenticator.Completed += FacebookAuthenticatorCompleted;
            authenticator.Error += FacebookAutheticatorError;

            presenter.Login(authenticator);
        }

        public async void FacebookAuthenticatorCompleted(object sender, AuthenticatorCompletedEventArgs e)
        {
            if (e.IsAuthenticated)
            {
                // KEYS: access_token, data_access_expiration_time,  expires_in, state
                if (accessToken == null && refreshToken == null)
                {
                    accessToken = e.Account.Properties["access_token"];
                    refreshToken = accessToken;
                    FacebookUserProfileAsync(accessToken);
                }
                else if (!refreshToken.Equals(e.Account.Properties["access_token"]) && !accessToken.Equals(e.Account.Properties["access_token"]))
                {
                    DateTime today = DateTime.Now;
                    DateTime expirationDate = today.AddDays(Constant.days);
                    Application.Current.Properties["time_stamp"] = expirationDate;

                    accessToken = e.Account.Properties["access_token"];
                    refreshToken = e.Account.Properties["access_token"];

                    UpdateTokensPost updateTokens = new UpdateTokensPost();
                    updateTokens.access_token = accessToken;
                    updateTokens.refresh_token = refreshToken;
                    updateTokens.uid = (string)Application.Current.Properties["uid"];
                    updateTokens.social_timestamp = expirationDate.ToString("yyyy-MM-dd HH:mm:ss");

                    var updatePostSerilizedObject = JsonConvert.SerializeObject(updateTokens);
                    var updatePostContent = new StringContent(updatePostSerilizedObject, Encoding.UTF8, "application/json");

                    var client = new HttpClient();
                    var RDSrespose = await client.PostAsync(Constant.UpdateTokensUrl, updatePostContent);

                    if (RDSrespose.IsSuccessStatusCode)
                    {
                        FacebookUserProfileAsync(accessToken);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Unable to update google tokens");
                    }
                }
            }
        }

        public async void FacebookUserProfileAsync(string accessToken)
        {
            // MECHANISM:

            // 1. RETRIVE TOKEN FROM SOCIAL LOGIN
            // 2. PASS THIS INFORMATION TO PARVA
            // 3. WAIT FOR A RESPONSE
            // 4. BASED ON THE RESPONSE I WOULD NEED TO REDIRECT THE USER TO THE CORRECT PAGE

            var client = new HttpClient();
            var socialLogInPost = new SocialLogInPost();

            var facebookResponse = client.GetStringAsync(Constant.FacebookUserInfoUrl + accessToken);
            var userData = facebookResponse.Result;

            FacebookResponse facebookData = JsonConvert.DeserializeObject<FacebookResponse>(userData);

            socialLogInPost.email = facebookData.email;
            socialLogInPost.password = "";
            socialLogInPost.token = accessToken;
            socialLogInPost.signup_platform = "FACEBOOK";

            var socialLogInPostSerialized = JsonConvert.SerializeObject(socialLogInPost);
            var postContent = new StringContent(socialLogInPostSerialized, Encoding.UTF8, "application/json");

            System.Diagnostics.Debug.WriteLine(socialLogInPostSerialized);

            var RDSResponse = await client.PostAsync(Constant.LogInUrl, postContent);
            var responseContent = await RDSResponse.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine(responseContent);
            if (RDSResponse.IsSuccessStatusCode)
            {
                if(responseContent != null)
                {
                    if(responseContent.Contains(Constant.EmailNotFound))
                    {
                        Application.Current.MainPage = new SocialLogInSignUp(facebookData.name, "", facebookData.email, accessToken, accessToken, "FACEBOOK");
                    }
                    if(responseContent.Contains(Constant.AutheticatedSuccesful))
                    {
                        Application.Current.MainPage = new HomePage();
                    }
                }
            }
        }

        private async void FacebookAutheticatorError(object sender, AuthenticatorErrorEventArgs e)
        {
            var authenticator = sender as OAuth2Authenticator;
            if (authenticator != null)
            {
                authenticator.Completed -= FacebookAuthenticatorCompleted;
                authenticator.Error -= FacebookAutheticatorError;
            }

            await DisplayAlert("Authentication error: ", e.Message, "OK");
        }

        public async void GoogleSignInClick(System.Object sender, System.EventArgs e)
        {
            string clientId = string.Empty;
            string redirectUri = string.Empty;
            
            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    clientId = Constant.GoogleiOSClientID;
                    redirectUri = Constant.GoogleRedirectUrliOS;
                    break;

                case Device.Android:
                    clientId = Constant.GoogleAndroidClientID;
                    redirectUri = Constant.GoogleRedirectUrlAndroid;
                    break;
            }

            var authenticator = new OAuth2Authenticator(clientId, string.Empty, Constant.GoogleScope, new Uri(Constant.GoogleAuthorizeUrl), new Uri(redirectUri), new Uri(Constant.GoogleAccessTokenUrl), null, true);
            var presenter = new Xamarin.Auth.Presenters.OAuthLoginPresenter();

            authenticator.Completed += GoogleAuthenticatorCompleted;
            authenticator.Error += GoogleAuthenticatorError;

            AuthenticationState.Authenticator = authenticator;
            presenter.Login(authenticator);
        }

        private async void GoogleAuthenticatorCompleted(object sender, AuthenticatorCompletedEventArgs e)
        {
            var authenticator = sender as OAuth2Authenticator;

            if (authenticator != null)
            {
                authenticator.Completed -= GoogleAuthenticatorCompleted;
                authenticator.Error -= GoogleAuthenticatorError;
            }

            if (e.IsAuthenticated)
            {
                if (accessToken == null && refreshToken == null)
                {
                    accessToken = e.Account.Properties["access_token"];
                    refreshToken = e.Account.Properties["refresh_token"];

                    Application.Current.Properties["access_token"] = accessToken;
                    Application.Current.Properties["refresh_token"] = refreshToken;
                         
                    GoogleUserProfileAsync(accessToken, refreshToken, e);
                }
                else if (!refreshToken.Equals(e.Account.Properties["refresh_token"])&&!accessToken.Equals(e.Account.Properties["access_token"]))
                {
                    DateTime today = DateTime.Now;
                    DateTime expirationDate = today.AddDays(Constant.days);
                    Application.Current.Properties["time_stamp"] = expirationDate;

                    accessToken = e.Account.Properties["access_token"];
                    refreshToken = e.Account.Properties["refresh_token"];

                    UpdateTokensPost updateTokens = new UpdateTokensPost();
                    updateTokens.access_token = accessToken;
                    updateTokens.refresh_token = refreshToken;
                    updateTokens.uid = (string)Application.Current.Properties["uid"];
                    updateTokens.social_timestamp = expirationDate.ToString("yyyy-MM-dd HH:mm:ss");

                    var updatePostSerilizedObject = JsonConvert.SerializeObject(updateTokens);
                    var updatePostContent = new StringContent(updatePostSerilizedObject, Encoding.UTF8, "application/json");

                    var client = new HttpClient();
                    var RDSrespose = await client.PostAsync(Constant.UpdateTokensUrl, updatePostContent);

                    if (RDSrespose.IsSuccessStatusCode)
                    {
                        GoogleUserProfileAsync(accessToken, refreshToken, e);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Unable to update google tokens");
                    }
                }
            }
            else
            {
                await DisplayAlert("Error", "Google was not able to autheticate your account", "OK"); 
            }
        }

        public async void GoogleUserProfileAsync(string accessToken, string refreshToken, AuthenticatorCompletedEventArgs e)
        {
            var client = new HttpClient();
            var socialLogInPost = new SocialLogInPost();

            var request = new OAuth2Request("GET", new Uri(Constant.GoogleUserInfoUrl), null, e.Account);
            var GoogleResponse = await request.GetResponseAsync();
            var userData = GoogleResponse.GetResponseText();

            GoogleResponse googleData = JsonConvert.DeserializeObject<GoogleResponse>(userData);

            socialLogInPost.email = googleData.email;
            socialLogInPost.password = "";
            socialLogInPost.token = refreshToken;
            socialLogInPost.signup_platform = "GOOGLE";

            var socialLogInPostSerialized = JsonConvert.SerializeObject(socialLogInPost);
            var postContent = new StringContent(socialLogInPostSerialized, Encoding.UTF8, "application/json");

            var RDSResponse = await client.PostAsync(Constant.LogInUrl, postContent);
            var responseContent = await RDSResponse.Content.ReadAsStringAsync();

            if (RDSResponse.IsSuccessStatusCode)
            {
                if (responseContent != null)
                {
                    if (responseContent.Contains(Constant.EmailNotFound))
                    {
                        Application.Current.MainPage = new SocialLogInSignUp(googleData.given_name, googleData.family_name, googleData.email, accessToken, refreshToken, "GOOGLE");
                    }
                    if (responseContent.Contains(Constant.AutheticatedSuccesful))
                    {
                        Application.Current.MainPage = new HomePage();
                    }
                }
            }
        }

        private async void GoogleAuthenticatorError(object sender, AuthenticatorErrorEventArgs e)
        {
            var authenticator = sender as OAuth2Authenticator;

            if (authenticator != null)
            {
                authenticator.Completed -= GoogleAuthenticatorCompleted;
                authenticator.Error -= GoogleAuthenticatorError;
            }

            await DisplayAlert("Authentication error: ", e.Message, "OK");
        }

        void AppleSignInClick(System.Object sender, System.EventArgs e)
        {
            SignIn?.Invoke(sender, e);
            var c = (ImageButton)sender;
            c.Command?.Execute(c.CommandParameter);
        }

        public void InvokeSignInEvent(object sender, EventArgs e)
            => SignIn?.Invoke(sender, e);
    }
}

