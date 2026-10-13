using System;

namespace GameBackendModule.Models
{
    // ─── GET /royal-league/status ────────────────────────────────────────────

    /// <summary>Số liệu của chính người chơi trên season đang chạy.</summary>
    [Serializable]
    public class RoyalLeagueMe
    {
        public int crowns;
        /// <summary>0 = chưa có hạng (chưa có Crown nào, hoặc bị loại khỏi bảng).</summary>
        public int rank;
        /// <summary>Phần trăm top, ví dụ 12.4 = "Top 12,4%". 0 khi chưa có hạng.</summary>
        public float percentile;
    }

    /// <summary>Season đang chạy. Null khi không có season nào mở.</summary>
    [Serializable]
    public class RoyalLeagueCurrentSeason
    {
        /// <summary>Khớp `SheetTable` trên Remote Config. Gửi kèm mọi request khác.</summary>
        public string sheetTable;
        /// <summary>scheduled | active | frozen | settled | closed.</summary>
        public string status;
        /// <summary>ISO 8601 UTC. LẤY TỪ ĐÂY, không tự tính từ Remote Config hay giờ máy.</summary>
        public string startsAt;
        public string endsAt;
        public bool joined;
        public RoyalLeagueMe me;
        public int totalParticipants;
    }

    /// <summary>
    /// Kết quả season đã chốt mà người chơi chưa xem/nhận. Null khi không có.
    ///
    /// Trả cho CẢ người ngoài Top 10 (<c>isWinner = false</c>) — họ cũng phải được
    /// xem bảng xếp hạng cuối một lần trước khi event ẩn.
    /// </summary>
    [Serializable]
    public class RoyalLeaguePendingResult
    {
        public string sheetTable;
        public int rank;
        public int crowns;
        public int totalParticipants;
        /// <summary>true = nằm trong Top 10 và đủ tư cách nhận thưởng.</summary>
        public bool isWinner;
        public bool claimed;
    }

    /// <summary>
    /// GET /royal-league/status — một endpoint cho toàn bộ màn hình League.
    ///
    /// Trả CẢ HAI nhánh vì season cũ và season mới tồn tại song song: đầu tháng 9
    /// người chơi vừa phải nhận thưởng S1 (<c>pending</c>) vừa được mời vào S2
    /// (<c>current</c>). KHÔNG phải cái này hoặc cái kia.
    /// </summary>
    [Serializable]
    public class RoyalLeagueStatusResponse
    {
        /// <summary>ISO 8601 UTC. Dùng thay giờ máy khi kiểm tra khung thời gian.</summary>
        public string serverTime;
        public RoyalLeagueCurrentSeason current;
        public RoyalLeaguePendingResult pending;
        /// <summary>
        /// Số mùa người chơi đứng hạng 1 bảng của mình, server đếm từ kết quả đã chốt —
        /// mùa đang nằm trong <see cref="pending"/> đã được tính. Lấy số LỚN HƠN giữa số
        /// đang giữ và số này, đừng tự cộng. 0 trên server chưa hỗ trợ.
        /// </summary>
        public int leaguesWon;
    }

    // ─── POST /royal-league/join ─────────────────────────────────────────────

    [Serializable]
    public class RoyalLeagueJoinRequest
    {
        public string sheetTable;
        /// <summary>
        /// Client tự khai đã hoàn thành area task chưa. Server KHÔNG verify được
        /// (không có dữ liệu area) — chỉ ghi lại để audit, không dùng để chặn.
        /// </summary>
        public bool areaTasksDone;
    }

    [Serializable]
    public class RoyalLeagueJoinResponse
    {
        public string sheetTable;
        public bool joined;
        public int crowns;
        public int rank;
        public int totalParticipants;
    }

    // ─── POST /royal-league/crowns ───────────────────────────────────────────

    /// <summary>Body cộng Crown sau khi thắng một round.</summary>
    [Serializable]
    public class RoyalLeagueSubmitCrownsRequest
    {
        public string sheetTable;
        /// <summary>
        /// Khoá chống trùng. Sinh MỘT lần cho mỗi lần thắng round và GIỮ NGUYÊN
        /// qua mọi lần retry — đây là thứ khiến gửi lại khi mất mạng an toàn tuyệt
        /// đối. Sinh key mới cho mỗi lần retry là mất hoàn toàn tác dụng.
        /// Dùng <see cref="Services.RoyalLeagueService.NewIdempotencyKey"/>.
        /// </summary>
        public string idempotencyKey;
        /// <summary>
        /// Số Crown client tự tính: Normal 1, Hard 3, Super Hard 5, nhân buff nếu có.
        /// Server cắt về trần của season (mặc định 10) và KHÔNG trả lỗi khi vượt.
        /// </summary>
        public int crowns;
    }

    [Serializable]
    public class RoyalLeagueSubmitCrownsResponse
    {
        public string sheetTable;
        /// <summary>true = idempotencyKey đã dùng rồi, server không cộng thêm lần nữa.</summary>
        public bool duplicate;
        /// <summary>Tổng Crown sau lần cộng này — dùng giá trị này để ghi đè số hiển thị.</summary>
        public int crowns;
        /// <summary>Số Crown thực cộng (đã cắt trần). 0 khi duplicate.</summary>
        public int crownsGained;
        public int rank;
        public int totalParticipants;
    }

    // ─── GET /royal-league/leaderboard ───────────────────────────────────────

    /// <summary>
    /// Một ô trên bảng xếp hạng. Cùng bộ field với <see cref="LeaderboardTopEntry"/>
    /// của bảng thường, chỉ khác <c>score</c> đổi thành <c>crowns</c>.
    /// </summary>
    [Serializable]
    public class RoyalLeagueLeaderboardEntry
    {
        public int rank;
        public string uid;
        public int crowns;
        public string countryCode;
        public PlayerProfileInfo info;
    }

    [Serializable]
    public class RoyalLeagueLeaderboardResponse
    {
        public string sheetTable;
        public string status;
        /// <summary>Số entry trả về trong trang này.</summary>
        public int total;
        /// <summary>Tổng entry của bảng (tối đa 1000) — dùng để biết còn bao nhiêu trang.</summary>
        public int boardTotal;
        /// <summary>Tổng người tham gia season (khác boardTotal khi hơn 1000 người).</summary>
        public int totalParticipants;
        public RoyalLeagueLeaderboardEntry[] entries;
    }

    // ─── GET /royal-league/rank ──────────────────────────────────────────────

    /// <summary>Hạng của một uid bất kỳ. Server trả JSON <c>null</c> nếu uid không tham gia season đó.</summary>
    [Serializable]
    public class RoyalLeagueRankResponse
    {
        public string sheetTable;
        public string uid;
        public int crowns;
        public int rank;
        public float percentile;
        public int totalParticipants;
    }

    // ─── POST /royal-league/claim ────────────────────────────────────────────

    [Serializable]
    public class RoyalLeagueClaimRequest
    {
        public string sheetTable;
    }

    /// <summary>
    /// Kết quả ghi nhận nhận thưởng.
    ///
    /// Server KHÔNG trả nội dung phần thưởng — client tra bảng thưởng của mình
    /// (từ `SheetTable` trên Remote Config) theo <c>rank</c> rồi tự cộng vào tài khoản.
    /// Server chỉ giữ cờ đã nhận / chưa nhận.
    ///
    /// Người ngoài Top 10 gọi vẫn nhận 200 với <c>isWinner = false</c> — không phải
    /// lỗi, client cần dữ liệu đó để hiện màn hình kết quả.
    /// </summary>
    [Serializable]
    public class RoyalLeagueClaimResponse
    {
        public string sheetTable;
        public bool isWinner;
        /// <summary>Đủ tư cách nhận: nằm trong Top 10, có ít nhất 1 Crown, không bị chặn.</summary>
        public bool eligible;
        /// <summary>true = đã nhận trước đó rồi; client KHÔNG được cộng thưởng lần nữa.</summary>
        public bool alreadyClaimed;
        public int rank;
        public int crowns;
        public int totalParticipants;
    }
}
