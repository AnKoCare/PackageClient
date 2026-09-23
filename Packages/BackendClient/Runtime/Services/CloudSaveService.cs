using System;
using System.Collections;
using GameBackendModule.Models;

namespace GameBackendModule.Services
{
    /// <summary>
    /// Cloud save theo Level Up guidelines của Google Play.
    ///
    /// Khác đường cũ (<see cref="IPlayerService.UpsertMyPlayerSave"/>) ở chỗ
    /// server cấp version và chỉ nhận khi `baseVersion` khớp bản trên cloud, nên
    /// hai máy chơi song song không âm thầm ghi đè nhau. Version lệch → HTTP 409,
    /// lúc đó <c>onError</c> nhận <see cref="ErrorResponse.cloud"/> là tóm tắt bản
    /// trên cloud, đủ để hiện popup cho người chơi chọn mà không phải gọi thêm.
    ///
    /// Đường cũ vẫn chạy bình thường cho client chưa cập nhật.
    /// </summary>
    public interface ICloudSaveService
    {
        /// <summary>
        /// GET /player/cloud-save/meta — chỉ tóm tắt, không kèm dữ liệu save.
        ///
        /// Lúc MỞ GAME thì không cần gọi: <see cref="AuthResponse.cloudSave"/> của
        /// external-login / refresh đã mang sẵn thông tin này.
        /// </summary>
        IEnumerator GetMeta(Action<ApiResponse<CloudSaveMetaResponse>> onSuccess, Action<ErrorResponse> onError);

        /// <summary>GET /player/cloud-save — tải cả bản save.</summary>
        IEnumerator Download(Action<ApiResponse<CloudSaveResponse>> onSuccess, Action<ErrorResponse> onError);

        /// <summary>
        /// POST /player/cloud-save — ghi khi `baseVersion` còn khớp bản trên cloud.
        /// Lệch thì onError nhận 409 + <see cref="ErrorResponse.cloud"/>.
        /// </summary>
        IEnumerator Upload(CloudSaveUploadRequest request, Action<ApiResponse<CloudSaveWriteResponse>> onSuccess, Action<ErrorResponse> onError);

        /// <summary>
        /// POST /player/cloud-save/resolve — người chơi đã chọn giữ bản nào. Bản bị
        /// bỏ được server lưu dự phòng. Trả về bản cuối cùng đang nằm trên cloud.
        /// </summary>
        IEnumerator Resolve(CloudSaveResolveRequest request, Action<ApiResponse<CloudSaveResponse>> onSuccess, Action<ErrorResponse> onError);

        /// <summary>
        /// POST /auth/link-gpg — gắn Play Games ID vào tài khoản đang đăng nhập
        /// (dùng cho máy đang đăng nhập bằng device ID).
        ///
        /// Nằm ở service này chứ không phải <see cref="IAuthService"/> vì nó là
        /// một phần của luồng cloud save; đường đăng nhập cũ không đổi.
        /// </summary>
        IEnumerator LinkGpg(LinkGpgRequest request, Action<ApiResponse<LinkGpgResponse>> onSuccess, Action<ErrorResponse> onError);
    }

    public class CloudSaveService : ICloudSaveService
    {
        private readonly IApiClient apiClient;

        public CloudSaveService(IApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        /// <summary>True khi lỗi là xung đột version (bản trên cloud đã đổi).</summary>
        public static bool IsConflict(ErrorResponse error)
        {
            return error != null
                   && (error.statusCode == 409
                       || string.Equals(error.code, CloudSaveConstants.ERR_SAVE_CONFLICT, StringComparison.Ordinal));
        }

        public IEnumerator GetMeta(Action<ApiResponse<CloudSaveMetaResponse>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return apiClient.Get(ApiConstants.PLAYER_CLOUD_SAVE_META_ENDPOINT, onSuccess, onError);
        }

        public IEnumerator Download(Action<ApiResponse<CloudSaveResponse>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return apiClient.Get(ApiConstants.PLAYER_CLOUD_SAVE_ENDPOINT, onSuccess, onError);
        }

        public IEnumerator Upload(CloudSaveUploadRequest request, Action<ApiResponse<CloudSaveWriteResponse>> onSuccess, Action<ErrorResponse> onError)
        {
            if (request == null || string.IsNullOrEmpty(request.data))
            {
                onError?.Invoke(BadRequest("data is required"));
                yield break;
            }

            // Server chỉ nhận chuỗi số. Rỗng/rác ở đây nghĩa là máy chưa từng đồng
            // bộ, tức là "tin rằng cloud đang trống".
            if (!IsNumeric(request.baseVersion))
            {
                request.baseVersion = CloudSaveConstants.EMPTY_VERSION;
            }

            yield return apiClient.Post(ApiConstants.PLAYER_CLOUD_SAVE_ENDPOINT, request, onSuccess, onError);
        }

        public IEnumerator Resolve(CloudSaveResolveRequest request, Action<ApiResponse<CloudSaveResponse>> onSuccess, Action<ErrorResponse> onError)
        {
            if (request == null
                || (!string.Equals(request.keep, CloudSaveConstants.KEEP_LOCAL, StringComparison.Ordinal)
                    && !string.Equals(request.keep, CloudSaveConstants.KEEP_CLOUD, StringComparison.Ordinal)))
            {
                onError?.Invoke(BadRequest("keep must be 'local' or 'cloud'"));
                yield break;
            }

            if (string.Equals(request.keep, CloudSaveConstants.KEEP_LOCAL, StringComparison.Ordinal)
                && string.IsNullOrEmpty(request.localData))
            {
                onError?.Invoke(BadRequest("localData is required when keep=local"));
                yield break;
            }

            if (!IsNumeric(request.cloudVersion))
            {
                request.cloudVersion = CloudSaveConstants.EMPTY_VERSION;
            }

            if (string.IsNullOrEmpty(request.source))
            {
                request.source = CloudSaveConstants.SOURCE_CONFLICT;
            }

            yield return apiClient.Post(ApiConstants.PLAYER_CLOUD_SAVE_RESOLVE_ENDPOINT, request, onSuccess, onError);
        }

        public IEnumerator LinkGpg(LinkGpgRequest request, Action<ApiResponse<LinkGpgResponse>> onSuccess, Action<ErrorResponse> onError)
        {
            if (request == null || string.IsNullOrEmpty(request.externalId))
            {
                onError?.Invoke(BadRequest("externalId is required"));
                yield break;
            }

            request.externalId = request.externalId.Trim();
            yield return apiClient.Post(ApiConstants.AUTH_LINK_GPG_ENDPOINT, request, onSuccess, onError);
        }

        private static bool IsNumeric(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] < '0' || value[i] > '9') return false;
            }
            return true;
        }

        /// <summary>Chặn ngay trên máy để khỏi tốn một vòng request chắc chắn bị 400.</summary>
        private static ErrorResponse BadRequest(string message)
        {
            return new ErrorResponse
            {
                success = false,
                message = message,
                error = message,
                statusCode = 400,
            };
        }
    }
}
