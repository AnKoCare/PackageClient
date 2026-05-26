using System;
using System.Collections;
using System.Collections.Generic;
using GameBackendModule.Models;

namespace GameBackendModule.Services
{
    public interface IWeeklyContestService
    {
        /// <summary>GET /weekly-contest/status — Bearer JWT.</summary>
        IEnumerator GetStatus(
            Action<ApiResponse<WeeklyContestStatusResponse>> onSuccess,
            Action<ErrorResponse> onError);

        /// <summary>POST /weekly-contest/claim — Bearer JWT, body rỗng <c>{}</c>.</summary>
        IEnumerator Claim(
            Action<ApiResponse<WeeklyContestClaimResponse>> onSuccess,
            Action<ErrorResponse> onError);

        /// <summary>POST /weekly-contest/add-score — Bearer JWT.</summary>
        IEnumerator AddScore(
            WeeklyContestAddScoreRequest request,
            Action<ApiResponse<WeeklyContestAddScoreResponse>> onSuccess,
            Action<ErrorResponse> onError);

        /// <summary>POST /weekly-contest/dev/end-week — [DEV] ép kết thúc tuần open.</summary>
        IEnumerator DevEndWeek(
            string devKey,
            Action<ApiResponse<WeeklyContestDevEndWeekResponse>> onSuccess,
            Action<ErrorResponse> onError);
    }

    public class WeeklyContestService : IWeeklyContestService
    {
        private readonly IApiClient apiClient;

        public WeeklyContestService(IApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        public IEnumerator GetStatus(
            Action<ApiResponse<WeeklyContestStatusResponse>> onSuccess,
            Action<ErrorResponse> onError)
        {
            yield return apiClient.Get<WeeklyContestStatusResponse>(
                ApiConstants.WEEKLY_CONTEST_STATUS_ENDPOINT,
                onSuccess,
                onError);
        }

        public IEnumerator Claim(
            Action<ApiResponse<WeeklyContestClaimResponse>> onSuccess,
            Action<ErrorResponse> onError)
        {
            yield return apiClient.Post<WeeklyContestClaimResponse>(
                ApiConstants.WEEKLY_CONTEST_CLAIM_ENDPOINT,
                new EmptyBody(),
                onSuccess,
                onError);
        }

        public IEnumerator AddScore(
            WeeklyContestAddScoreRequest request,
            Action<ApiResponse<WeeklyContestAddScoreResponse>> onSuccess,
            Action<ErrorResponse> onError)
        {
            yield return apiClient.Post<WeeklyContestAddScoreResponse>(
                ApiConstants.WEEKLY_CONTEST_ADD_SCORE_ENDPOINT,
                request,
                onSuccess,
                onError);
        }

        public IEnumerator DevEndWeek(
            string devKey,
            Action<ApiResponse<WeeklyContestDevEndWeekResponse>> onSuccess,
            Action<ErrorResponse> onError)
        {
            IReadOnlyDictionary<string, string> extraHeaders = null;
            if (!string.IsNullOrWhiteSpace(devKey))
            {
                extraHeaders = new Dictionary<string, string>
                {
                    { ApiConstants.WEEKLY_CONTEST_DEV_KEY_HEADER, devKey.Trim() },
                };
            }

            yield return apiClient.Post<WeeklyContestDevEndWeekResponse>(
                ApiConstants.WEEKLY_CONTEST_DEV_END_WEEK_ENDPOINT,
                new EmptyBody(),
                onSuccess,
                onError,
                extraHeaders);
        }
    }
}
