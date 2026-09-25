using System;

namespace GameBackendModule.Models
{
    /// <summary>
    /// Tóm tắt do client nhúng ở gốc JSON save (field `meta`), để server trả về
    /// được level/coin mà không phải gửi cả bản save 12KB.
    ///
    /// JsonUtility KHÔNG dựng được giá trị null cho object con: server trả
    /// `"meta": null` thì field này vẫn là một object toàn số 0. Dùng
    /// <see cref="HasValue"/> để biết bản cloud có tóm tắt hay không (save do
    /// client cũ ghi thì không có).
    /// </summary>
    [Serializable]
    public class CloudSaveMeta
    {
        public int level;
        public long coin;
        public string device;
        public int iap;

        /// <summary>
        /// Avatar/khung/huy hiệu đang đeo của bản save, để popup khôi phục vẽ đúng mặt
        /// người chơi trên máy kia. Mặc định -1 = bản save cũ không mang thông tin này
        /// (JsonUtility giữ nguyên giá trị khởi tạo khi field vắng mặt trong JSON).
        /// </summary>
        public int avatar = -1;
        public int frame = -1;
        public int badge = -1;

        public bool HasValue =>
            level > 0 || coin > 0 || iap > 0 || !string.IsNullOrEmpty(device);

        /// <summary>Bản save có mang avatar/khung không.</summary>
        public bool HasProfile => avatar >= 0 || frame >= 0;
    }

    /// <summary>Trạng thái bản save trên cloud, không kèm dữ liệu save.</summary>
    [Serializable]
    public class CloudSaveMetaResponse
    {
        /// <summary>False nghĩa là cloud chưa có save nào của người chơi này.</summary>
        public bool exists;

        /// <summary>Version do server cấp, dạng yyyyMMddHHmmss. "0" khi chưa có save.</summary>
        public string version;

        /// <summary>Thời điểm lưu (ISO 8601 UTC) suy ra từ version. Rỗng khi chưa có save.</summary>
        public string savedAt;

        public CloudSaveMeta meta;

        public bool HasMeta => meta != null && meta.HasValue;
    }

    /// <summary>Bản save đầy đủ tải từ cloud.</summary>
    [Serializable]
    public class CloudSaveResponse
    {
        public bool exists;
        public string version;
        public string savedAt;
        public CloudSaveMeta meta;

        /// <summary>JSON save dạng chuỗi, đúng thứ client đã gửi lên.</summary>
        public string data;

        public bool HasMeta => meta != null && meta.HasValue;
    }

    [Serializable]
    public class CloudSaveUploadRequest
    {
        /// <summary>JSON save dạng chuỗi. Nên có field `meta` ở gốc.</summary>
        public string data;

        /// <summary>
        /// Version cloud mà máy này đang giữ. Gửi <see cref="CloudSaveConstants.EMPTY_VERSION"/>
        /// khi tin rằng cloud chưa có save nào. Lệch với version thật trên cloud → 409.
        /// </summary>
        public string baseVersion;
    }

    [Serializable]
    public class CloudSaveWriteResponse
    {
        public string version;
        public string savedAt;
    }

    [Serializable]
    public class CloudSaveResolveRequest
    {
        /// <summary><see cref="CloudSaveConstants.KEEP_LOCAL"/> hoặc <see cref="CloudSaveConstants.KEEP_CLOUD"/>.</summary>
        public string keep;

        /// <summary>Version cloud mà người chơi vừa nhìn thấy lúc hiện popup.</summary>
        public string cloudVersion;

        /// <summary>JSON save trên máy. Bắt buộc khi keep=local; keep=cloud thì gửi để lưu dự phòng.</summary>
        public string localData;

        /// <summary><see cref="CloudSaveConstants.SOURCE_CONFLICT"/> hoặc <see cref="CloudSaveConstants.SOURCE_SETTINGS"/>.</summary>
        public string source;
    }

    [Serializable]
    public class LinkGpgRequest
    {
        /// <summary>Play Games player ID của máy đang chơi.</summary>
        public string externalId;
    }

    /// <summary>Tóm tắt tài khoản đang giữ Play Games ID, để hỏi người chơi giữ bản nào.</summary>
    [Serializable]
    public class LinkedAccountSummary
    {
        public string uid;
        public int level;
        public string savedAt;
    }

    [Serializable]
    public class LinkGpgResponse
    {
        /// <summary>Xem <see cref="CloudSaveConstants"/>: LINK_*, một trong 4 giá trị.</summary>
        public string status;

        /// <summary>Chỉ có khi status = conflict.</summary>
        public LinkedAccountSummary other;
    }

    public static class CloudSaveConstants
    {
        /// <summary>Gửi làm baseVersion khi client tin cloud chưa có save nào.</summary>
        public const string EMPTY_VERSION = "0";

        /// <summary>Mã trong ErrorResponse.code khi bản trên cloud đã đổi (HTTP 409).</summary>
        public const string ERR_SAVE_CONFLICT = "SAVE_CONFLICT";

        public const string KEEP_LOCAL = "local";
        public const string KEEP_CLOUD = "cloud";

        public const string SOURCE_CONFLICT = "conflict";
        public const string SOURCE_SETTINGS = "settings";

        /// <summary>Vừa gắn Play Games ID vào tài khoản đang đăng nhập.</summary>
        public const string LINK_LINKED = "linked";

        /// <summary>Play Games ID vốn đã thuộc chính tài khoản này.</summary>
        public const string LINK_ALREADY_LINKED = "already_linked";

        /// <summary>Play Games ID thuộc tài khoản khác — hỏi người chơi giữ bản nào.</summary>
        public const string LINK_CONFLICT = "conflict";

        /// <summary>Tài khoản này đã gắn một Play Games ID khác — bỏ qua.</summary>
        public const string LINK_USER_HAS_OTHER_GPG = "user_has_other_gpg";
    }
}
