using System;
using System.Collections;
using UnityEngine;
using GameBackendModule.Models;

namespace GameBackendModule.Services
{
    public interface IUserPurgeService
    {
        /// <summary>POST /users/purge-by-uid — Bearer JWT. Xóa toàn bộ user sở hữu player có uid (theo spec backend).</summary>
        IEnumerator PurgeByUid(
            PurgeUserByUidRequest request,
            Action<ApiResponse<PurgeUserByUidResponse>> onSuccess,
            Action<ErrorResponse> onError);
    }

    public class UserPurgeService : IUserPurgeService
    {
        private readonly IApiClient apiClient;

        public UserPurgeService(IApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        public IEnumerator PurgeByUid(
            PurgeUserByUidRequest request,
            Action<ApiResponse<PurgeUserByUidResponse>> onSuccess,
            Action<ErrorResponse> onError)
        {
            yield return apiClient.Post<PurgeUserByUidResponse>(ApiConstants.USERS_PURGE_BY_UID_ENDPOINT, request, onSuccess, onError);
        }
    }
}
