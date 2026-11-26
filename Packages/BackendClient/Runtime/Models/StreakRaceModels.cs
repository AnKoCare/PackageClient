using System;

namespace GameBackendModule.Models
{
    // Lưu ý chung: JsonUtility không có khái niệm null cho class lồng nhau. Server trả
    // `round: null` thì client nhận một object rỗng với roundId = 0. Luôn kiểm tra
    // `roundId > 0` (hoặc `rank > 0`) thay vì so với null.

    // ─── GET /streak-race/status ─────────────────────────────────────────────

    /// <summary>Vòng đang chạy. <c>roundId = 0</c> nghĩa là không có vòng nào.</summary>
    [Serializable]
    public class StreakRaceRound
    {
        public int roundId;
        /// <summary>ISO 8601 UTC. Đếm ngược theo đây, không tự giả định 24 giờ.</summary>
        public string startsAt;
        public string endsAt;
        /// <summary>Số người thật mỗi bảng.</summary>
        public int x;
        /// <summary>Số bot mỗi bảng.</summary>
        public int y;
        /// <summary>Số hạng đầu được nhận thưởng.</summary>
        public int z;
        /// <summary>Chuỗi hệ số, ví dụ [1, 5, 10, 25, 100].</summary>
        public int[] multipliers;
        /// <summary>Client gộp lần nộp: tối thiểu bấy nhiêu giây giữa hai lần gửi thường.</summary>
        public int submitIntervalSeconds;
    }

    [Serializable]
    public class StreakRaceStatusResponse
    {
        public string serverTime;
        /// <summary>false = tính năng đang tắt ở server, ẩn hết UI.</summary>
        public bool enabled;
        public StreakRaceRound round;
        /// <summary>Vòng vừa kết thúc, 0 nếu không có. Chỉ hỏi kết quả khi còn giữ vé của vòng đó.</summary>
        public int previousRoundId;
    }

    // ─── POST /streak-race/submit ────────────────────────────────────────────

    /// <summary>
    /// Nộp TỔNG DỒN của vòng, không nộp phần chênh. Server giữ số lớn nhất nên gửi
    /// lại bao nhiêu lần cũng được.
    /// </summary>
    [Serializable]
    public class StreakRaceSubmitRequest
    {
        public int roundId;
        /// <summary>Null ở lần đầu của vòng; sau đó gửi lại đúng chuỗi server đã trả.</summary>
        public string seatToken;
        public int winsTotal;
        public int flagsTotal;
        /// <summary>Trận thắng gần nhất cách lúc gửi bao nhiêu giây (0 = vừa thắng, hoặc chỉ xin chỗ).</summary>
        public int agoSeconds;
    }

    [Serializable]
    public class StreakRaceSubmitResponse
    {
        public int roundId;
        /// <summary>Vé chỗ ngồi. Lưu lại và gửi kèm mọi request sau của vòng này.</summary>
        public string seatToken;
        public int groupIndex;
        public int flags;
        public int wins;
        public string serverTime;
    }

    // ─── POST /streak-race/leaderboard | result | claim ──────────────────────

    [Serializable]
    public class StreakRaceSeatRequest
    {
        public int roundId;
        public string seatToken;
    }

    /// <summary>
    /// Một dòng trên bảng. Bot cũng là một dòng như mọi dòng khác (uid thật của
    /// người được chép), nên bấm vào mở profile bằng uid như Ant League.
    /// </summary>
    [Serializable]
    public class StreakRaceEntry
    {
        public int rank;
        public string uid;
        public int flags;
        public string countryCode;
        public PlayerProfileInfo info;
    }

    /// <summary>Hạng và cờ của chính mình. <c>rank = 0</c> = chưa có trên bảng (chưa kịp ghi).</summary>
    [Serializable]
    public class StreakRaceMe
    {
        public int rank;
        public int flags;
        /// <summary>
        /// Tổng trận thắng server đang giữ. Máy vừa cài lại (tổng trên máy về 0) phải
        /// lấy lại cả <c>flags</c> lẫn <c>wins</c>, nếu không mọi lần nộp sau thấp hơn
        /// số của server và không được tính.
        /// </summary>
        public int wins;
    }

    [Serializable]
    public class StreakRaceLeaderboardResponse
    {
        public int roundId;
        public string serverTime;
        public string endsAt;
        public bool finished;
        public int z;
        /// <summary>
        /// Cờ của mình trên server có thể trễ tới ~25 giây so với số trên máy. Lấy số
        /// lớn hơn và tự xếp lại dòng của mình.
        /// </summary>
        public StreakRaceMe me;
        public StreakRaceEntry[] entries;
    }

    [Serializable]
    public class StreakRaceResultResponse
    {
        public int roundId;
        public string serverTime;
        public int z;
        /// <summary>false = vòng vừa đóng, server còn đang ghi nốt; thử lại sau <c>retryAfterSeconds</c>.</summary>
        public bool ready;
        public int retryAfterSeconds;
        /// <summary>0 = không có trên bảng.</summary>
        public int rank;
        public int flags;
        public bool isWinner;
        public bool claimed;
        public StreakRaceEntry[] entries;
    }

    [Serializable]
    public class StreakRaceClaimResponse
    {
        public int roundId;
        public int rank;
        public bool isWinner;
        /// <summary>Chỉ trao thưởng khi <c>isWinner &amp;&amp; !alreadyClaimed</c>.</summary>
        public bool alreadyClaimed;
    }
}
