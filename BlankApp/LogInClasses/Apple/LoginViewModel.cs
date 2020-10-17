using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using BlankApp.Constants;
using Newtonsoft.Json;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BlankApp.LogInClasses.Apple
{
    public class LoginViewModel
    {
        public static string apple_token = null;
        public static string apple_email = null;

        public bool IsAppleSignInAvailable { get { return appleSignInService?.IsAvailable ?? false; } }
        public ICommand SignInWithAppleCommand { get; set; }

        public event EventHandler AppleError = delegate { };

        IAppleSignInService appleSignInService;
        public LoginViewModel()
        {
            appleSignInService = DependencyService.Get<IAppleSignInService>();
            SignInWithAppleCommand = new Command(OnAppleSignInRequest);
        }

        public async void OnAppleSignInRequest()
        {
            var account = await appleSignInService.SignInAsync();
            if (account != null)
            {
                Preferences.Set(App.LoggedInKey, true);
                await SecureStorage.SetAsync(App.AppleUserIdKey, account.UserId);

                // System.Diagnostics.Debug.WriteLine($"Signed in!\n  Name: {account?.Name ?? string.Empty}\n  Email: {account?.Email ?? string.Empty}\n  UserId: {account?.UserId ?? string.Empty}");

                var client = new HttpClient();
                var socialLogInPost = new SocialLogInPost();

                System.Diagnostics.Debug.WriteLine("apple_token:" + apple_token);
                System.Diagnostics.Debug.WriteLine("apple_email:" + apple_email);

                if (apple_token == null && apple_email == null)
                {
                    System.Diagnostics.Debug.WriteLine("First time user");
    
                    apple_token = account.Token.Substring(0, 500);
                    apple_email = account.Email;
                    AppleUserProfileAsync(apple_token, apple_email, account.Name);
                }
                else if (!apple_token.Equals(account.Token) && apple_token != null)
                {
                    System.Diagnostics.Debug.WriteLine("Second Time user");
                    DateTime today = DateTime.Now;
                    DateTime expirationDate = today.AddDays(Constant.days);
                    Application.Current.Properties["time_stamp"] = expirationDate;

                    apple_token = account.Token.Substring(0, 500);

                    UpdateTokensPost updateTokens = new UpdateTokensPost();
                    updateTokens.access_token = apple_token;
                    updateTokens.refresh_token = apple_token;
                    updateTokens.uid = (string)Application.Current.Properties["uid"];
                    updateTokens.social_timestamp = expirationDate.ToString("yyyy-MM-dd HH:mm:ss");

                    var updatePostSerilizedObject = JsonConvert.SerializeObject(updateTokens);
                    var updatePostContent = new StringContent(updatePostSerilizedObject, Encoding.UTF8, "application/json");
                    var RDSrespose = await client.PostAsync(Constant.UpdateTokensUrl, updatePostContent);

                    if (RDSrespose.IsSuccessStatusCode)
                    {
                        AppleUserProfileAsync(apple_token, apple_email, string.Empty);
                    }
                    else 
                    {
                        System.Diagnostics.Debug.WriteLine("We were not able to update your APPLE token");
                    }
                }
            }
            else
            {
                AppleError?.Invoke(this, default(EventArgs));
            }
        }

        public async void AppleUserProfileAsync(string appleToken, string appleUserEmail, string userName)
        {
            System.Diagnostics.Debug.WriteLine("LINE 95");
            var client = new HttpClient();
            var socialLogInPost = new SocialLogInPost();

            socialLogInPost.email = appleUserEmail;
            socialLogInPost.password = "";
            socialLogInPost.token = appleToken;
            socialLogInPost.signup_platform = "APPLE";

            var socialLogInPostSerialized = JsonConvert.SerializeObject(socialLogInPost);
            var postContent = new StringContent(socialLogInPostSerialized, Encoding.UTF8, "application/json");
            var RDSResponse = await client.PostAsync(Constant.LogInUrl, postContent);
            var responseContent = await RDSResponse.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine(responseContent);
            if (RDSResponse.IsSuccessStatusCode)
            {
                if (responseContent != null)
                {
                    if (responseContent.Contains(Constant.EmailNotFound))
                    {
                        Application.Current.MainPage = new SocialLogInSignUp(userName, "", appleUserEmail, appleToken, appleToken, "APPLE");
                    }
                    if (responseContent.Contains(Constant.AutheticatedSuccesful))
                    {
                        Application.Current.MainPage = new HomePage();
                    }
                }
            }
        }
    }
}
