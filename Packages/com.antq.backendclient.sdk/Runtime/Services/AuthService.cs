using System;
using System.Collections;
using UnityEngine;
using GameBackendModule.Models;
using GameBackendModule.DI;

namespace GameBackendModule.Services
{
    public interface IAuthService
    {
        IEnumerator Register(RegisterRequest request, Action<ApiResponse<AuthResponse>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator Login(LoginRequest request, Action<ApiResponse<AuthResponse>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator RefreshToken(RefreshTokenRequest request, Action<ApiResponse<AuthResponse>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator ChangePassword(ChangePasswordRequest request, Action<ApiResponse<object>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator ExternalLogin(ExternalLoginRequest request, Action<ApiResponse<AuthResponse>> onSuccess, Action<ErrorResponse> onError);
    }

    public class AuthService : IAuthService
    {
        private readonly IApiClient apiClient;
        private readonly IGameBackendManager backendManager;

        public AuthService(IApiClient apiClient, IGameBackendManager backendManager)
        {
            this.apiClient = apiClient;
            this.backendManager = backendManager;
        }

        public IEnumerator Register(RegisterRequest request, Action<ApiResponse<AuthResponse>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return apiClient.Post<AuthResponse>(ApiConstants.REGISTER_ENDPOINT, request, 
                (response) => {
                    if (response.success && response.data != null)
                    {
                        backendManager.SetAuthToken(response.data.accessToken);
                        backendManager.SetUserData(response.data.user);
                    }
                    onSuccess?.Invoke(response);
                }, 
                onError);
        }

        public IEnumerator Login(LoginRequest request, Action<ApiResponse<AuthResponse>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return apiClient.Post<AuthResponse>(ApiConstants.LOGIN_ENDPOINT, request, 
                (response) => {
                    if (response.success && response.data != null)
                    {
                        backendManager.SetAuthToken(response.data.accessToken);
                        backendManager.SetUserData(response.data.user);
                    }
                    onSuccess?.Invoke(response);
                }, 
                onError);
        }

        public IEnumerator RefreshToken(RefreshTokenRequest request, Action<ApiResponse<AuthResponse>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return apiClient.Post<AuthResponse>(ApiConstants.REFRESH_TOKEN_ENDPOINT, request, 
                (response) => {
                    if (response.success && response.data != null)
                    {
                        backendManager.SetAuthToken(response.data.accessToken);
                        backendManager.SetUserData(response.data.user);
                    }
                    onSuccess?.Invoke(response);
                }, 
                onError);
        }

        public IEnumerator ChangePassword(ChangePasswordRequest request, Action<ApiResponse<object>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return apiClient.Post<object>(ApiConstants.CHANGE_PASSWORD_ENDPOINT, request, onSuccess, onError);
        }

        public IEnumerator ExternalLogin(ExternalLoginRequest request, Action<ApiResponse<AuthResponse>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return apiClient.Post<AuthResponse>(ApiConstants.EXTERNAL_LOGIN_ENDPOINT, request,
                (response) => {
                    if (response.success && response.data != null)
                    {
                        backendManager.SetAuthToken(response.data.accessToken);
                        backendManager.SetUserData(response.data.user);
                    }
                    onSuccess?.Invoke(response);
                },
                onError);
        }
    }
}
