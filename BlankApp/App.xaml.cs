using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using BlankApp.LogInClasses.Apple;
using BlankApp.Constants;

namespace BlankApp
{
    public partial class App : Application
    {
        public const string LoggedInKey = "LoggedIn";
        public const string AppleUserIdKey = "AppleUserIdKey";
        string userId;

        public App()
        {
            InitializeComponent();

            if (Preferences.Get(LoggedInKey, false))
            {
                MainPage = new HomePage();
            }
            else
            {
                DateTime today = DateTime.Now;
                DateTime expTime = today;

                if (this.Properties.ContainsKey("time_stamp"))
                {
                    expTime = (DateTime)this.Properties["time_stamp"];
                }

                if (this.Properties.ContainsKey("access_token")
                    && this.Properties.ContainsKey("refresh_token")
                    && this.Properties.ContainsKey("time_stamp") && today <= expTime)
                {

                    MainPage = new HomePage();

                }
                else if (this.Properties.ContainsKey("access_token")
                   && this.Properties.ContainsKey("refresh_token")
                   && this.Properties.ContainsKey("time_stamp") && today > expTime)
                {

                    LogIn myPage = new LogIn();

                    System.Object sender = new System.Object();
                    System.EventArgs e = new System.EventArgs();

                    if (this.Properties["platform"].Equals(Constant.Google))
                    {
                        myPage.GoogleSignInClick(sender, e);
                    }
                    if (this.Properties["platform"].Equals(Constant.Facebook))
                    {
                        myPage.FacebookSignInClick(sender, e);
                    }
                }else if (this.Properties.ContainsKey("social"))
                {
                    MainPage = new HomePage();
                }
                else
                {
                    MainPage = new LogIn();
                }
            }
        }

        protected override async void OnStart()
        {
            var appleSignInService = DependencyService.Get<IAppleSignInService>();

            if (appleSignInService != null)
            {
                userId = await SecureStorage.GetAsync(AppleUserIdKey);

                if (appleSignInService.IsAvailable && !string.IsNullOrEmpty(userId))
                {
                    var credentialState = await appleSignInService.GetCredentialStateAsync(userId);
                    switch (credentialState)
                    {
                        case AppleSignInCredentialState.Authorized:
                            break;
                        case AppleSignInCredentialState.NotFound:
                        case AppleSignInCredentialState.Revoked:
                            //Logout;
                            SecureStorage.Remove(AppleUserIdKey);
                            Preferences.Set(LoggedInKey, false);
                            MainPage = new LogIn();
                            break;
                    }
                }
            }
        }

        protected override void OnSleep()
        {

        }

        protected override void OnResume()
        {

        }
    }
}
