using System;
using System.Collections;
using System.Collections.Generic;
using GameBackendModule.Models;

namespace GameBackendModule.Services
{
    public interface IPlayerService
    {
        IEnumerator GetProfile(Action<ApiResponse<PlayerProfile>> onSuccess, Action<ErrorResponse> onError);
        /// <summary>GET /player/profile?uid= — public. Lấy profile player theo mã hiển thị uid.</summary>
        IEnumerator GetProfileByUid(string uid, Action<ApiResponse<PlayerProfile>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator UpdateProfile(UpdatePlayerRequest request, Action<ApiResponse<PlayerProfile>> onSuccess, Action<ErrorResponse> onError);
        /// <summary>PUT /player/info — body chỉ gồm các key cần gửi (server merge với DEFAULT_PLAYER_INFO).</summary>
        IEnumerator UpdatePlayerInfo(Dictionary<string, object> infoFields, Action<ApiResponse<PlayerPatchResponse>> onSuccess, Action<ErrorResponse> onError);
        /// <summary>PATCH /player/country — countryCode ISO alpha-2 chữ thường.</summary>
        IEnumerator UpdateCountryCode(UpdateCountryCodeRequest request, Action<ApiResponse<PlayerPatchResponse>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator GetMyPlayerSave(Action<ApiResponse<PlayerSave>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator UpsertMyPlayerSave(SaveDataRequest request, Action<ApiResponse<PlayerSave>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator DeletePlayer(string playerId, Action<ApiResponse<DeletePlayerResponse>> onSuccess, Action<ErrorResponse> onError);
    }

    public class PlayerService : IPlayerService
    {
        private readonly IApiClient apiClient;

        public PlayerService(IApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        public IEnumerator GetProfile(Action<ApiResponse<PlayerProfile>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return apiClient.Get(ApiConstants.PLAYER_PROFILE_ENDPOINT, onSuccess, onError);
        }

        public IEnumerator GetProfileByUid(string uid, Action<ApiResponse<PlayerProfile>> onSuccess, Action<ErrorResponse> onError)
        {
            if (string.IsNullOrEmpty(uid))
            {
                onError?.Invoke(new ErrorResponse
                {
                    success = false,
                    message = "uid is required",
                    error = "uid is required",
                    statusCode = 400,
                });
                yield break;
            }

            string q = Uri.EscapeDataString(uid.Trim());
            string endpoint = $"{ApiConstants.PLAYER_PROFILE_ENDPOINT}?uid={q}";
            yield return apiClient.Get(endpoint, onSuccess, onError);
        }

        public IEnumerator UpdateProfile(UpdatePlayerRequest request, Action<ApiResponse<PlayerProfile>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return apiClient.Put(ApiConstants.PLAYER_PROFILE_ENDPOINT, request, onSuccess, onError);
        }

        public IEnumerator UpdatePlayerInfo(Dictionary<string, object> infoFields, Action<ApiResponse<PlayerPatchResponse>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return apiClient.Put(ApiConstants.PLAYER_INFO_ENDPOINT, infoFields, onSuccess, onError);
        }

        public IEnumerator UpdateCountryCode(UpdateCountryCodeRequest request, Action<ApiResponse<PlayerPatchResponse>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return apiClient.Patch(ApiConstants.PLAYER_COUNTRY_ENDPOINT, request, onSuccess, onError);
        }

        public IEnumerator GetMyPlayerSave(Action<ApiResponse<PlayerSave>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return apiClient.Get(ApiConstants.PLAYER_SAVE_ENDPOINT, onSuccess, onError);
        }

        public IEnumerator UpsertMyPlayerSave(SaveDataRequest request, Action<ApiResponse<PlayerSave>> onSuccess, Action<ErrorResponse> onError)
        {
            // Đảm bảo version là UNIX timestamp hiện tại hoặc lớn hơn
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (string.IsNullOrEmpty(request.version) || !long.TryParse(request.version, out long provided) || provided < now)
            {
                request.version = now.ToString();
            }
            yield return apiClient.Post(ApiConstants.PLAYER_SAVE_ENDPOINT, request, onSuccess, onError);
        }

        public IEnumerator DeletePlayer(string playerId, Action<ApiResponse<DeletePlayerResponse>> onSuccess, Action<ErrorResponse> onError)
        {
            string endpoint = string.Format(ApiConstants.DELETE_PLAYER_ENDPOINT, playerId);
            yield return apiClient.Delete(endpoint, onSuccess, onError);
        }
    }
}
