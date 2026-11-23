using System;
using System.Collections;
using GameBackendModule.Models;

namespace GameBackendModule.Services
{
    /// <summary>
    /// Streak Race: giải chuỗi thắng theo vòng, mỗi bảng x người thật + y bot.
    ///
    /// Mã lỗi nghiệp vụ nằm ở <see cref="ErrorResponse.code"/>:
    /// <list type="bullet">
    /// <item>409 <c>ROUND_CLOSED</c>: vòng đã đóng hoặc sai vòng → gọi lại <c>GetStatus</c>.</item>
    /// <item>409 <c>JOIN_IN_PROGRESS</c>: một request khác của tài khoản này đang xin chỗ → thử lại sau vài giây.</item>
    /// <item>409 <c>NOT_SETTLED</c>: claim khi vòng chưa chốt → gọi <c>GetResult</c> và chờ <c>retryAfterSeconds</c>.</item>
    /// <item>409 <c>ROUND_GONE</c>: dữ liệu vòng đã bị dọn → bỏ vé của vòng đó.</item>
    /// <item>409 <c>DISABLED</c>: tính năng đang tắt.</item>
    /// <item>400 <c>INVALID_SCORE</c>: tổng gửi lên không hợp lệ → đừng gửi lại y nguyên.</item>
    /// <item>403 <c>NOT_JOINED</c>: vé không hợp lệ cho vòng này → bỏ vé, nộp lại để xin chỗ.</item>
    /// <item>403 <c>BLOCKED</c>: tài khoản bị cấm tham gia → ẩn event.</item>
    /// <item>503 <c>TRY_LATER</c>: lỗi tạm thời → giữ nguyên tổng, lần sau gửi lại.</item>
    /// </list>
    /// </summary>
    public interface IStreakRaceService
    {
        /// <summary>GET /streak-race/status — thông tin vòng, không có gì riêng của người chơi.</summary>
        IEnumerator GetStatus(
            Action<ApiResponse<StreakRaceStatusResponse>> onSuccess,
            Action<ErrorResponse> onError);

        /// <summary>
        /// POST /streak-race/submit — nộp TỔNG số trận thắng và TỔNG số cờ của vòng.
        /// <paramref name="seatToken"/> null ở lần đầu; server cấp chỗ và trả vé.
        /// </summary>
        IEnumerator Submit(
            int roundId,
            string seatToken,
            int winsTotal,
            int flagsTotal,
            Action<ApiResponse<StreakRaceSubmitResponse>> onSuccess,
            Action<ErrorResponse> onError);

        /// <summary>POST /streak-race/leaderboard — bảng của mình trong vòng (gồm cả bot).</summary>
        IEnumerator GetLeaderboard(
            int roundId,
            string seatToken,
            Action<ApiResponse<StreakRaceLeaderboardResponse>> onSuccess,
            Action<ErrorResponse> onError);

        /// <summary>POST /streak-race/result — kết quả cuối của một vòng đã kết thúc.</summary>
        IEnumerator GetResult(
            int roundId,
            string seatToken,
            Action<ApiResponse<StreakRaceResultResponse>> onSuccess,
            Action<ErrorResponse> onError);

        /// <summary>
        /// POST /streak-race/claim — ghi nhận đã nhận thưởng, đúng một lần.
        /// Server không trả nội dung thưởng; client tra bảng thưởng của mình theo <c>rank</c>.
        /// </summary>
        IEnumerator Claim(
            int roundId,
            string seatToken,
            Action<ApiResponse<StreakRaceClaimResponse>> onSuccess,
            Action<ErrorResponse> onError);
    }

    public class StreakRaceService : IStreakRaceService
    {
        private readonly IApiClient apiClient;

        public StreakRaceService(IApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        private static ErrorResponse LocalError(string message)
        {
            return new ErrorResponse
            {
                success = false,
                message = message,
                error = message,
                statusCode = 400,
            };
        }

        public IEnumerator GetStatus(
            Action<ApiResponse<StreakRaceStatusResponse>> onSuccess,
            Action<ErrorResponse> onError)
        {
            yield return apiClient.Get(ApiConstants.STREAK_RACE_STATUS_ENDPOINT, onSuccess, onError);
        }

        public IEnumerator Submit(
            int roundId,
            string seatToken,
            int winsTotal,
            int flagsTotal,
            Action<ApiResponse<StreakRaceSubmitResponse>> onSuccess,
            Action<ErrorResponse> onError)
        {
            if (roundId < 1)
            {
                onError?.Invoke(LocalError("roundId is required"));
                yield break;
            }
            // Server chỉ cấp chỗ cho người đã thắng ít nhất một trận trong vòng.
            if (winsTotal < 1 || flagsTotal < 0)
            {
                onError?.Invoke(LocalError("winsTotal must be >= 1 and flagsTotal >= 0"));
                yield break;
            }

            var body = new StreakRaceSubmitRequest
            {
                roundId = roundId,
                seatToken = string.IsNullOrEmpty(seatToken) ? null : seatToken,
                winsTotal = winsTotal,
                flagsTotal = flagsTotal,
            };
            yield return apiClient.Post(ApiConstants.STREAK_RACE_SUBMIT_ENDPOINT, body, onSuccess, onError);
        }

        public IEnumerator GetLeaderboard(
            int roundId,
            string seatToken,
            Action<ApiResponse<StreakRaceLeaderboardResponse>> onSuccess,
            Action<ErrorResponse> onError)
        {
            yield return PostSeat(ApiConstants.STREAK_RACE_LEADERBOARD_ENDPOINT, roundId, seatToken, onSuccess, onError);
        }

        public IEnumerator GetResult(
            int roundId,
            string seatToken,
            Action<ApiResponse<StreakRaceResultResponse>> onSuccess,
            Action<ErrorResponse> onError)
        {
            yield return PostSeat(ApiConstants.STREAK_RACE_RESULT_ENDPOINT, roundId, seatToken, onSuccess, onError);
        }

        public IEnumerator Claim(
            int roundId,
            string seatToken,
            Action<ApiResponse<StreakRaceClaimResponse>> onSuccess,
            Action<ErrorResponse> onError)
        {
            yield return PostSeat(ApiConstants.STREAK_RACE_CLAIM_ENDPOINT, roundId, seatToken, onSuccess, onError);
        }

        private IEnumerator PostSeat<T>(
            string endpoint,
            int roundId,
            string seatToken,
            Action<ApiResponse<T>> onSuccess,
            Action<ErrorResponse> onError)
        {
            if (roundId < 1 || string.IsNullOrEmpty(seatToken))
            {
                onError?.Invoke(LocalError("roundId and seatToken are required"));
                yield break;
            }
            var body = new StreakRaceSeatRequest { roundId = roundId, seatToken = seatToken };
            yield return apiClient.Post(endpoint, body, onSuccess, onError);
        }
    }
}
