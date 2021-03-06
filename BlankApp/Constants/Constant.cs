﻿using System;
namespace BlankApp.Constants
{
    public class Constant
    {
        // FACEBOOK CONSTANTS
        public static string FacebookScope = "email";
        public static string FacebookAuthorizeUrl = "https://www.facebook.com/dialog/oauth/";
        public static string FacebookAccessTokenUrl = "https://www.facebook.com/connect/login_success.html";
        public static string FacebookUserInfoUrl = "https://graph.facebook.com/me?fields=email,name,picture&access_token=";

        public static string FacebookAndroidClientID = "257223515515874";
        public static string FacebookiOSClientID = "257223515515874";

        public static string FacebookiOSRedirectUrl = "https://www.facebook.com/connect/login_success.html:/oauth2redirect";
        public static string FacebookAndroidRedirectUrl = "https://www.facebook.com/connect/login_success.html";

        // GOOGLE CONSTANTS
        public static string GoogleScope = "https://www.googleapis.com/auth/userinfo.profile https://www.googleapis.com/auth/userinfo.email";
        public static string GoogleAuthorizeUrl = "https://accounts.google.com/o/oauth2/v2/auth";
        public static string GoogleAccessTokenUrl = "https://www.googleapis.com/oauth2/v4/token";
        public static string GoogleUserInfoUrl = "https://www.googleapis.com/oauth2/v2/userinfo";

        public static string GoogleiOSClientID = "97916302968-f22boafqno1dicq4a0eolpr6qj8hkvbm.apps.googleusercontent.com";
        public static string GoogleAndroidClientID = "97916302968-6nlu2otc3icdefg28qpbqbk1fam2hj8d.apps.googleusercontent.com";

        public static string GoogleRedirectUrliOS = "com.googleusercontent.apps.97916302968-f22boafqno1dicq4a0eolpr6qj8hkvbm:/oauth2redirect";
        public static string GoogleRedirectUrlAndroid = "com.infiniteoptions.socialloginsxamarin:/oauth2redirect";

        // ENDPOINTS
        public static string AccountSaltUrl = "https://tsx3rnuidi.execute-api.us-west-1.amazonaws.com/dev/api/v2/AccountSalt";
        public static string LogInUrl = "https://tsx3rnuidi.execute-api.us-west-1.amazonaws.com/dev/api/v2/Login/";
        public static string SignUpUrl = "https://tsx3rnuidi.execute-api.us-west-1.amazonaws.com/dev/api/v2/SignUp";
        public static string UpdateTokensUrl = "https://tsx3rnuidi.execute-api.us-west-1.amazonaws.com/dev/api/v2/access_refresh_update";

        // RDS CODES
        public static string EmailNotFound = "404";
        public static string AutheticatedSuccesful = "200";

        // PLATFORM
        public static string Google = "GOOGLE";
        public static string Facebook = "FACEBOOK";

        // EXTENDED TIME
        public static double days = 14;
    }
}
