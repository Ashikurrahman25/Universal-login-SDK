using Facebook.Unity;
using Google;
using RestAPIHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace RestAPIHelper
{
    public class Authentication
    {
        Config config { get; set; }
        APIHelper api { get; set; }

        public Authentication(Config config, APIHelper api)
        {
            this.config = config;
            this.api = api;

            if (!FB.IsInitialized)
            {
                FB.Init(InitCallback, OnHideUnity);
            }
            else
            {
                FB.ActivateApp();
            }

            Debug.Log("Authentication Initialized");
        }

        private void InitCallback()
        {
            if (FB.IsInitialized)
            {
                // Signal an app activation App Event
                FB.ActivateApp();
            }
            else
                Debug.Log("Failed to Initialize the Facebook SDK");

        }

        private void OnHideUnity(bool isGameShown)
        {
            if (!isGameShown)
                Time.timeScale = 0;

            else
                Time.timeScale = 1;
        }

        public void FacebookLogin()
        {
            var perms = new List<string>() { "public_profile", "email" };
            FB.LogInWithReadPermissions(perms, AuthCallback);
        }

        private void AuthCallback(ILoginResult result)
        {
            if (FB.IsLoggedIn)
            {
                var aToken = AccessToken.CurrentAccessToken;

                Debug.Log(aToken.TokenString);
                api.LoginWithProvider(Provider.Facebook, aToken.TokenString, (user) =>
                {
                    //Debug.Log(user);
                    UIController.instance.HandleFacebookLogin(user);
                });
            }
            else
            {
                UIController.instance.HideLoadingPanel();
                Debug.Log("User cancelled login");
            }
        }

        public void GoogleLogin(Action<string> data)
        {
            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                RequestIdToken = true,
                // Copy this ClientID from GCP OAuth Client IDs(Step 4).
                WebClientId = config.webClientId,
                RequestAuthCode = true,
                AdditionalScopes = new List<string>()
                {
                    "email", // Scope for Email
                    "profile" // Scope for Public profile
                }
            };

            //Google Sign-In SDK
            Task<GoogleSignInUser> signIn = GoogleSignIn.DefaultInstance.SignIn();

            signIn.ContinueWith(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("Cancelled");
                    UIController.instance.HideLoadingPanel();
                }
                else if (task.IsFaulted)
                {
                    Debug.LogError(task.Exception.Message);
                    UIController.instance.HideLoadingPanel();
                    using (IEnumerator<System.Exception> enumerator =
                        task.Exception.InnerExceptions.GetEnumerator())
                    {
                        if (enumerator.MoveNext())
                        {
                            GoogleSignIn.SignInException error =
                                    (GoogleSignIn.SignInException)enumerator.Current;
                            Debug.LogError("Got Error: " + error.Status + " " + error.Message);
                        }
                        else
                        {
                            Debug.LogError("Got Unexpected Exception?!?" + task.Exception);
                        }
                    }
                }

                //No Error
                else
                {
                    string authCode = task.Result.IdToken; //Auth Code

                    //Debug.Log(authCode);
                    api.LoginWithProvider(Provider.Google, authCode, (user) =>
                    {
                        data(user);
                    });
                }
            });
        }
    }
}

