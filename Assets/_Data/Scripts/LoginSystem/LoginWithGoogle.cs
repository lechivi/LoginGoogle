using System.Collections;
using System.Collections.Generic;
using Firebase.Extensions;
using Google;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Firebase.Auth;
using UnityEngine.UI;
using UnityEngine.Networking;

public class LoginWithGoogle : MonoBehaviour
{
    [Header("Google API")]
    private string GoogleAPI = "489625237981-qp60074mpr4icnqolhofj45r4epto2c4.apps.googleusercontent.com";
    private GoogleSignInConfiguration configuration;

    [Header("Firebase Auth")]
    private FirebaseAuth auth;
    private FirebaseUser user;

    [Header("UI References")]
    public GameObject LoginPanel;
    public Button LoginButton;
    public Button LogoutButton;
    public GameObject UserPanel;
    public TMP_Text Username;
    public TMP_Text UserEmail;
    public TMP_Text UserID;
    public Image UserProfilePic;

    private string imageUrl;
    private bool isGoogleSignInInitialized = false;

    private void Start()
    {
        InitFirebase();
        LoginPanel.SetActive(true);
        UserPanel.SetActive(false);
        LoginButton.onClick.AddListener(Login);
        LogoutButton.onClick.AddListener(Logout);
    }

    void InitFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
    }

    public void Login()
    {
        if (!isGoogleSignInInitialized)
        {
            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                RequestAuthCode = true,
                RequestIdToken = true,
                WebClientId = GoogleAPI,
                RequestEmail = true
            };
            isGoogleSignInInitialized = true;
        }

        GoogleSignIn.DefaultInstance.SignIn().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogWarning("Google sign-in was canceled.");
                return;
            }

            if (task.IsFaulted)
            {
                Debug.LogError("Google sign-in encountered an error: " + task.Exception);
                return;
            }

            GoogleSignInUser googleUser = task.Result;
            string authCode = googleUser.AuthCode;
            if (string.IsNullOrEmpty(authCode))
            {
                Debug.LogError("Google Sign-In auth code is null or empty.");
            }
            else
            {
                Debug.Log("Google Auth Code: " + authCode);
            }

            Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
            auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(authTask =>
            {
                if (authTask.IsCanceled)
                {
                    Debug.LogWarning("Firebase auth was canceled.");
                    return;
                }

                if (authTask.IsFaulted)
                {
                    Debug.LogError("Firebase auth failed: " + authTask.Exception);
                    return;
                }

                user = auth.CurrentUser;
                Username.text = user.DisplayName;
                UserEmail.text = user.Email;
                UserID.text = user.UserId;

                LoginPanel.SetActive(false);
                UserPanel.SetActive(true);

                StartCoroutine(LoadImage(CheckImageUrl(user.PhotoUrl?.ToString())));
            });
        });
    }

    // User SignOut From Firebase First Then again Sign IN With Google
    public void Logout()
    {
        GoogleSignIn.DefaultInstance.SignOut();
        LoginPanel.SetActive(true);
        UserPanel.SetActive(false);
    }

    private string CheckImageUrl(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            return url;
        }
        return imageUrl;
    }

    IEnumerator LoadImage(string imageUri)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUri);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(www);
            UserProfilePic.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            UserProfilePic.preserveAspect = true;
            Debug.Log("Image loaded successfully.");
        }
        else
        {
            Debug.LogError("Error loading profile image: " + www.error);
        }
    }

}
