using System;
using UnityEngine;
using Zenject;
using GameBackendModule.Models;
using GameBackendModule.Services;

namespace GameBackendModule.DI
{
    public interface IGameBackendManager
    {
        bool IsAuthenticated { get; }
        string AuthToken { get; }
        UserData CurrentUser { get; }
        
        event Action<bool> OnAuthenticationChanged;
        event Action<UserData> OnUserDataChanged;
        
        void Initialize();
        void SetAuthToken(string token);
        void ClearAuth();
        void SetUserData(UserData userData);
    }

    public class GameBackendManager : IGameBackendManager, IInitializable
    {
        private readonly IApiClient apiClient;
        private readonly LazyInject<IAuthService> authService;

        public bool IsAuthenticated { get; private set; }
        public string AuthToken { get; private set; }
        public UserData CurrentUser { get; private set; }

        public event Action<bool> OnAuthenticationChanged;
        public event Action<UserData> OnUserDataChanged;

        public GameBackendManager(IApiClient apiClient, LazyInject<IAuthService> authService)
        {
            this.apiClient = apiClient;
            this.authService = authService;
        }

        public void Initialize()
        {
            // Load saved auth token if exists
            string savedToken = PlayerPrefs.GetString("auth_token", "");
            if (!string.IsNullOrEmpty(savedToken))
            {
                SetAuthToken(savedToken);
            }
        }

        public void SetAuthToken(string token)
        {
            AuthToken = token;
            IsAuthenticated = !string.IsNullOrEmpty(token);
            
            apiClient.SetAuthToken(token);
            PlayerPrefs.SetString("auth_token", token);
            PlayerPrefs.Save();
            
            OnAuthenticationChanged?.Invoke(IsAuthenticated);
            
            if (IsAuthenticated)
            {
                var service = authService.Value;
                // Load user data
                LoadUserData();
            }
        }

        public void ClearAuth()
        {
            AuthToken = null;
            IsAuthenticated = false;
            CurrentUser = null;
            
            apiClient.ClearAuthToken();
            PlayerPrefs.DeleteKey("auth_token");
            PlayerPrefs.Save();
            
            OnAuthenticationChanged?.Invoke(false);
            OnUserDataChanged?.Invoke(null);
        }

        private void LoadUserData()
        {
            // This would typically be called after successful login
            // For now, we'll leave it empty as the user data comes from login response
        }

        public void SetUserData(UserData userData)
        {
            CurrentUser = userData;
            OnUserDataChanged?.Invoke(userData);
        }
    }
}
