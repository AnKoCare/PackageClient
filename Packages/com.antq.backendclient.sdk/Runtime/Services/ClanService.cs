using System;
using System.Collections;
using UnityEngine;
using GameBackendModule.Models;

namespace GameBackendModule.Services
{
    public interface IClanService
    {
        IEnumerator CreateClan(CreateClanRequest request, Action<ApiResponse<ClanData>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator GetClanDetails(string clanId, Action<ApiResponse<ClanData>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator UpdateClan(string clanId, UpdateClanRequest request, Action<ApiResponse<ClanData>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator JoinClan(JoinClanRequest request, Action<ApiResponse<object>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator LeaveClan(LeaveClanRequest request, Action<ApiResponse<object>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator KickMember(string clanId, KickMemberRequest request, Action<ApiResponse<object>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator PromoteMember(string clanId, PromoteMemberRequest request, Action<ApiResponse<object>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator GetClanMembers(string clanId, Action<ApiResponse<ClanMember[]>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator SearchClans(SearchClanRequest request, Action<ApiResponse<ClanData[]>> onSuccess, Action<ErrorResponse> onError);
    }

    public class ClanService : IClanService
    {
        private readonly IApiClient apiClient;

        public ClanService(IApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        public IEnumerator CreateClan(CreateClanRequest request, Action<ApiResponse<ClanData>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return apiClient.Post(ApiConstants.CREATE_CLAN_ENDPOINT, request, onSuccess, onError);
        }

        public IEnumerator GetClanDetails(string clanId, Action<ApiResponse<ClanData>> onSuccess, Action<ErrorResponse> onError)
        {
            string endpoint = string.Format(ApiConstants.CLAN_DETAILS_ENDPOINT, clanId);
            yield return apiClient.Get(endpoint, onSuccess, onError);
        }

        public IEnumerator UpdateClan(string clanId, UpdateClanRequest request, Action<ApiResponse<ClanData>> onSuccess, Action<ErrorResponse> onError)
        {
            string endpoint = string.Format(ApiConstants.UPDATE_CLAN_ENDPOINT, clanId);
            yield return apiClient.Put(endpoint, request, onSuccess, onError);
        }

        public IEnumerator JoinClan(JoinClanRequest request, Action<ApiResponse<object>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return apiClient.Post(ApiConstants.JOIN_CLAN_ENDPOINT, request, onSuccess, onError);
        }

        public IEnumerator LeaveClan(LeaveClanRequest request, Action<ApiResponse<object>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return apiClient.Post(ApiConstants.LEAVE_CLAN_ENDPOINT, request, onSuccess, onError);
        }

        public IEnumerator KickMember(string clanId, KickMemberRequest request, Action<ApiResponse<object>> onSuccess, Action<ErrorResponse> onError)
        {
            string endpoint = string.Format(ApiConstants.KICK_MEMBER_ENDPOINT, clanId);
            yield return apiClient.Post(endpoint, request, onSuccess, onError);
        }

        public IEnumerator PromoteMember(string clanId, PromoteMemberRequest request, Action<ApiResponse<object>> onSuccess, Action<ErrorResponse> onError)
        {
            string endpoint = string.Format(ApiConstants.PROMOTE_MEMBER_ENDPOINT, clanId);
            yield return apiClient.Post(endpoint, request, onSuccess, onError);
        }

        public IEnumerator GetClanMembers(string clanId, Action<ApiResponse<ClanMember[]>> onSuccess, Action<ErrorResponse> onError)
        {
            string endpoint = string.Format(ApiConstants.CLAN_MEMBERS_ENDPOINT, clanId);
            yield return apiClient.Get(endpoint, onSuccess, onError);
        }

        public IEnumerator SearchClans(SearchClanRequest request, Action<ApiResponse<ClanData[]>> onSuccess, Action<ErrorResponse> onError)
        {
            string queryParams = $"?name={request.name}&limit={request.limit}&offset={request.offset}";
            yield return apiClient.Get(ApiConstants.SEARCH_CLAN_ENDPOINT + queryParams, onSuccess, onError);
        }
    }
}
