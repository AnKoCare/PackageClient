using System;

namespace GameBackendModule.Models
{
    [Serializable]
    public class ApiResponse<T>
    {
        public bool success;
        public string message;
        public T data;
        public int statusCode;
		public string responseDate;
    }

    [Serializable]
    public class ErrorResponse
    {
        public bool success;
        public string message;
        public string error;
        public int statusCode;
		public string responseDate;

        /// <summary>
        /// Mã lỗi nghiệp vụ do server gửi kèm (hiện chỉ Royal League dùng):
        /// UNKNOWN_SEASON, SEASON_CLOSED, SEASON_NOT_ACTIVE, SEASON_NOT_SETTLED, RATE_LIMITED.
        /// Null với các endpoint khác — client nên branch theo field này thay vì so chuỗi message.
        /// </summary>
        public string code;

        /// <summary>
        /// Chỉ có ở lỗi 409 của cloud save (code = SAVE_CONFLICT): tóm tắt bản
        /// đang nằm trên cloud, đủ để hiện popup cho người chơi chọn mà không
        /// phải gọi thêm request.
        /// </summary>
        public CloudSaveMetaResponse cloud;
    }

    // Generic helpers
    [Serializable]
    public class GenericAcknowledge
    {
        public int affected;
    }
    [Serializable]
    public class EmptyBody { }

    // Authentication Models
    [Serializable]
    public class RegisterRequest
    {
        public string username;
        public string email;
        public string password;
        public string displayName;
    }

    [Serializable]
    public class LoginRequest
    {
        public string email;
        public string password;
    }

    /// <summary>JSON profile mở rộng trên bảng `players` (khớp DEFAULT_PLAYER_INFO server).</summary>
    [Serializable]
    public class PlayerProfileInfo
    {
        public int Level;
        public string PlayerName;
        public int AvatarIndex;
        public int FrameIndex;
        public int BadgeIndex;
        public int EffectNameIndex;
        public string TeamName;
        /// <summary>Id team đang ở (UUID), rỗng = không ở team nào. Có trên bảng xếp hạng
        /// người chơi để mở thẳng trang team mà không phải search theo tên.</summary>
        public string TeamId;
        public int LogoTeamIndex;
        public int WinStreak;
        public int TotalCoinEarn;
        public int BoosterUsed;
        public int SweetMealChampion;
        public int FirstTryWin;
        public int CollectionsDone;
        public int CompleteTreetopMission;
        public int CompleteNestQuest;
        /// <summary>Tổng LeaguePoint cộng dồn qua mọi mùa Royal League.</summary>
        public int TotalLeaguePoint;
        /// <summary>LeaguePoint cao nhất từng đạt trong MỘT mùa Royal League.</summary>
        public int HighestLeaguePoint;
    }

    /// <summary>Player kèm auth (register / external-login).</summary>
    [Serializable]
    public class AuthPlayerPayload
    {
        public string id;
        public string userId;
        public string uid;
        /// <summary>ISO 3166-1 alpha-2 chữ thường hoặc null.</summary>
        public string countryCode;
        public PlayerProfileInfo info;
    }

    [Serializable]
    public class AuthResponse
    {
        public string accessToken;
        public string refreshToken;
        public UserData user;
        public AuthPlayerPayload player;

        /// <summary>
        /// Tóm tắt bản save trên cloud, server gắn kèm mỗi lần đăng nhập /
        /// refresh. Nhờ vậy lúc mở game client không phải gọi thêm request nào để
        /// biết cloud đang có gì. `exists=false` nghĩa là cloud chưa có save.
        /// </summary>
        public CloudSaveMetaResponse cloudSave;

        public string serverTimeUtc;
    }

    [Serializable]
    public class ExternalLoginRequest
    {
        // provider: 'device' | 'gpg' | 'gamecenter'
        public string provider;
        public string externalId;
        public string displayName;
        // Tùy chọn: token truy cập chính để liên kết identity với tài khoản hiện có
        public string mainAccessToken;
    }

    [Serializable]
    public class RefreshTokenRequest
    {
        public string refreshToken;
    }

    [Serializable]
    public class ChangePasswordRequest
    {
        public string currentPassword;
        public string newPassword;
    }

    // User Models
    [Serializable]
    public class UserData
    {
        public string id;
        public string username;
        public string email;
        public string displayName;
        public string avatar;
        public bool isEmailVerified;
        public bool isActive;
        public string lastLoginAt;
        public string createdAt;
        public string updatedAt;
    }

    /// <summary>Object user lồng trong GET/PUT /player/profile (backend hiện tại).</summary>
    [Serializable]
    public class PlayerProfileUserRef
    {
        public string id;
        public string username;
        public string displayName;
        public string avatar;
    }

    // Player Models
    [Serializable]
    public class PlayerProfile
    {
        public string id;
        public string userId;
        /// <summary>Mã hiển thị 10 chữ số, sinh khi tạo player lần đầu.</summary>
        public string uid;
        public PlayerProfileInfo info;
        /// <summary>ISO 3166-1 alpha-2 chữ thường; null nếu chưa set.</summary>
        public string countryCode;
        public PlayerProfileUserRef user;
        public string createdAt;
        public string updatedAt;
    }

    [Serializable]
    public class UpdatePlayerRequest
    {
        public string displayName;
        public string avatar;
    }

    /// <summary>Body PATCH /player/country — countryCode chữ thường (vd. vn).</summary>
    [Serializable]
    public class UpdateCountryCodeRequest
    {
        public string countryCode;
    }

    /// <summary>Phản hồi PATCH /player/country hoặc PUT /player/info (id, userId, uid + phần cập nhật).</summary>
    [Serializable]
    public class PlayerPatchResponse
    {
        public string id;
        public string userId;
        public string uid;
        public string countryCode;
        public PlayerProfileInfo info;
    }

    // Player Save Models
    [Serializable]
    public class PlayerSave
    {
        public string playerId;
        /// <summary>Mã hiển thị 10 chữ số của player (server trả kèm save).</summary>
        public string uid;
        public string data;
        public string version; // dùng string để tránh tràn số lớn
    }

    [Serializable]
    public class SaveDataRequest
    {
        public string data;
        public string version;
        public bool force;
    }

    [Serializable]
    public class DeletePlayerResponse
    {
        public string message;
        public string playerId;
    }

    /// <summary>Body POST /users/purge-by-uid.</summary>
    [Serializable]
    public class PurgeUserByUidRequest
    {
        public string uid;
    }

    /// <summary>Phản hồi 200 purge-by-uid.</summary>
    [Serializable]
    public class PurgeUserByUidResponse
    {
        public bool deleted;
        public string uid;
        public string userId;
        public string[] playerIds;
        public string[] clansDeleted;
    }

    // Weekly contest (weekly-contest.controller + weekly-contest.service)
    [Serializable]
    public class WeeklyContestWeekSummary
    {
        public string id;
        public string startsAt;
        public string endsAt;
        public string status;
    }

    [Serializable]
    public class WeeklyContestGroupSummary
    {
        public string id;
        public int memberCount;
        public int minLevel;
        public int maxLevel;
    }

    [Serializable]
    public class WeeklyContestLeaderboardEntry
    {
        public int rank;
        public string uid;
        public int score;
        public string lastPointAt;
        public PlayerProfileInfo info;
    }

    [Serializable]
    public class WeeklyContestPendingClaim
    {
        public string weekId;
        public int finalRank;
        public int finalScore;
        public WeeklyContestLeaderboardEntry[] leaderboard;
    }

    /// <summary>GET /weekly-contest/status.</summary>
    [Serializable]
    public class WeeklyContestStatusResponse
    {
        public WeeklyContestWeekSummary currentWeek;
        public bool enrolled;
        public WeeklyContestGroupSummary group;
        public int myRank;
        public int myScore;
        public WeeklyContestLeaderboardEntry[] leaderboard;
        public WeeklyContestPendingClaim pendingClaim;
    }

    /// <summary>POST /weekly-contest/claim.</summary>
    [Serializable]
    public class WeeklyContestClaimResponse
    {
        public string ackedWeekId;
        public bool enrolled;
        public string groupId;
        public string weekId;
    }

    /// <summary>Body POST /weekly-contest/add-score.</summary>
    [Serializable]
    public class WeeklyContestAddScoreRequest
    {
        public string idempotencyKey;
        /// <summary>1–10; mặc định 1 nếu không gửi (server merge).</summary>
        public int points = 1;
    }

    /// <summary>POST /weekly-contest/add-score.</summary>
    [Serializable]
    public class WeeklyContestAddScoreResponse
    {
        public bool duplicate;
        public int score;
        public int rank;
    }

    /// <summary>POST /weekly-contest/dev/end-week — [DEV] ép kết thúc tuần open.</summary>
    [Serializable]
    public class WeeklyContestDevEndWeekResponse
    {
        public string frozenWeekId;
        public string newWeekId;
        public string newWeekStatus;
        public string newWeekStartsAt;
        public string newWeekEndsAt;
        public WeeklyContestPendingClaim pendingClaim;
    }

    // Leaderboard Models (khớp leaderboard.controller + leaderboard.service)
    [Serializable]
    public class LeaderboardSubmitScoreRequest
    {
        public int score;
    }

    [Serializable]
    public class LeaderboardSubmitScoreResponse
    {
        public string playerId;
        public string uid;
        public int score;
        public string countryCode;
        public string scoreUpdatedAt;
    }

    [Serializable]
    public class LeaderboardTopEntry
    {
        public int rank;
        public string uid;
        public int score;
        public string countryCode;
        public PlayerProfileInfo info;
    }

    [Serializable]
    public class LeaderboardTopResponse
    {
        public string countryCode;
        /// <summary>Số entry trả về trong <see cref="entries"/>.</summary>
        public int total;
        /// <summary>
        /// Tổng entry board đang có (≤ 1000) — dùng để biết còn bao nhiêu trang phải tải.
        /// Bằng 0 nếu server chưa hỗ trợ field này → fallback về <see cref="total"/>.
        /// </summary>
        public int boardTotal;
        public LeaderboardTopEntry[] entries;
    }

    [Serializable]
    public class LeaderboardRankResponse
    {
        public int rank;
        public string uid;
        public int score;
        /// <summary>Crown (TotalLeaguePoint) — tie-break khi cùng score.</summary>
        public int leaguePoint;
        public string countryCode;
        public string board;
        public int total;
    }

    // Clan Models
    [Serializable]
    public class CreateClanRequest
    {
        public string name;
        public string description;
        public string logo;
        public int maxMembers = 50;
        public bool isPrivate = false;
    }

    [Serializable]
    public class UpdateClanRequest
    {
        public string name;
        public string description;
        public string logo;
        public int maxMembers;
        public bool isPrivate;
    }

    [Serializable]
    public class ClanData
    {
        public string id;
        public string name;
        public string description;
        public string logo;
        public int totalScore;
        public int memberCount;
        public int maxMembers;
        public bool isPrivate;
        public string leaderId;
        public string createdAt;
        public string updatedAt;
    }

    [Serializable]
    public class ClanMember
    {
        public string id;
        public string clanId;
        public string userId;
        public string userName;
        public string role;
        public int contribution;
        public string joinedAt;
        public string updatedAt;
    }

    [Serializable]
    public class JoinClanRequest
    {
        public string clanId;
    }

    [Serializable]
    public class LeaveClanRequest
    {
        public string clanId;
    }

    [Serializable]
    public class KickMemberRequest
    {
        public string userId;
    }

    [Serializable]
    public class PromoteMemberRequest
    {
        public string userId;
        public string role;
    }

    [Serializable]
    public class SearchClanRequest
    {
        public string name;
        public int limit = 20;
        public int offset = 0;
    }

    // Team Models (team.controller + team.service — docs/TEAM_API.md)
    public static class TeamType
    {
        public const string Open = "open";
        public const string Closed = "closed";
    }

    public static class TeamRole
    {
        public const string Leader = "leader";
        public const string CoLeader = "co_leader";
        public const string Member = "member";
    }

    [Serializable]
    public class CreateTeamRequest
    {
        public string name;
        public int badgeId;
        public string description;
        public int minLevelRequired;
        /// <summary>Crown tối thiểu để gia nhập (0 = không yêu cầu). Server chốt trần 2000.</summary>
        public int minCupsRequired;
        /// <summary><see cref="TeamType.Open"/> hoặc <see cref="TeamType.Closed"/>.</summary>
        public string type;
        /// <summary>ISO alpha-2 viết thường. Bỏ trống → server lấy từ player creator.</summary>
        public string countryCode;
    }

    [Serializable]
    public class UpdateTeamRequest
    {
        public int badgeId;
        public string description;
        public int minLevelRequired;
        /// <summary>Crown tối thiểu để gia nhập (0 = không yêu cầu). Server chốt trần 2000.</summary>
        public int minCupsRequired;
        public string type;
        /// <summary>ISO alpha-2 viết thường (Leader chỉnh).</summary>
        public string countryCode;
    }

    [Serializable]
    public class TeamUserRef
    {
        public string id;
        /// <summary>Tên hiển thị của user — server set = PlayerName trong players.info.</summary>
        public string username;
        // Cosmetics từ players.info (server đọc từ player level cao nhất của user).
        public int AvatarIndex;
        public int FrameIndex;
        public int BadgeIndex;
        public int EffectNameIndex;
    }

    [Serializable]
    public class TeamMemberData
    {
        public string id;
        public string role;
        public string joinedAt;
        /// <summary>F4 — hoạt động gần nhất (~aTi client Skewer).</summary>
        public string lastActiveAt;
        /// <summary>F4 — level live từ players.info.Level.</summary>
        public int level;
        /// <summary>Crown lũy kế (players.info.TotalLeaguePoint), snapshot trên team_members.</summary>
        public int leaguePoint;
        /// <summary>F4 — tổng số lần giúp đồng đội.</summary>
        public int contribution;
        public TeamUserRef user;
    }

    [Serializable]
    public class TeamData
    {
        public string id;
        public string name;
        public int badgeId;
        public string description;
        public int minLevelRequired;
        /// <summary>Crown tối thiểu để gia nhập (0 = không yêu cầu).</summary>
        public int minCupsRequired;
        public string type;
        /// <summary>F1 — ISO alpha-2 viết thường (null nếu chưa set).</summary>
        public string countryCode;
        public int memberCount;
        public int maxMembers;
        /// <summary>F1 — tổng level thành viên (điểm xếp hạng).</summary>
        public int teamScore;
        /// <summary>F4 — tổng contribution thành viên.</summary>
        public int totalContribution;
        /// <summary>F1 — World rank (số team teamScore cao hơn + 1).</summary>
        public int rank;
        /// <summary>Độ tích cực tuần: "High" / "Med" / "Low" (server tính theo teamScore tăng trong tuần).</summary>
        public string activity;
        public string leaderId;
        public string createdAt;
        public string updatedAt;
        public TeamMemberData[] members;
        /// <summary>POST /team/join trên closed team — <c>pending</c>.</summary>
        public string status;
        /// <summary>Thông báo khi join request pending.</summary>
        public string message;
    }

    [Serializable]
    public class TeamSuggestionItem
    {
        public string id;
        public string name;
        public int badgeId;
        public string description;
        public string type;
        public string countryCode;
        public int minLevelRequired;
        public int memberCount;
        public int maxMembers;
        public int teamScore;
        public int spotsLeft;
    }

    [Serializable]
    public class TeamSuggestionsResponse
    {
        public int playerLevel;
        public TeamSuggestionItem[] suggestions;
    }

    [Serializable]
    public class TeamSearchItem
    {
        public string id;
        public string name;
        public int badgeId;
        public string description;
        public string type;
        public string countryCode;
        public int minLevelRequired;
        public int memberCount;
        public int maxMembers;
        public int teamScore;
        public bool isFull;
        public int spotsLeft;
    }

    [Serializable]
    public class TeamSearchPagination
    {
        public int page;
        public int limit;
        public int total;
        public int totalPages;
    }

    [Serializable]
    public class TeamSearchResponse
    {
        public TeamSearchItem[] teams;
        public TeamSearchPagination pagination;
    }

    [Serializable]
    public class JoinTeamRequest
    {
        public string teamId;
    }

    [Serializable]
    public class LeaveTeamRequest
    {
        public string teamId;
    }

    [Serializable]
    public class KickTeamMemberRequest
    {
        public string userId;
    }

    [Serializable]
    public class PromoteTeamMemberRequest
    {
        public string userId;
        /// <summary><see cref="TeamRole.CoLeader"/> hoặc <see cref="TeamRole.Member"/>.</summary>
        public string role;
    }

    [Serializable]
    public class TransferTeamLeadershipRequest
    {
        public string userId;
    }

    [Serializable]
    public class TeamJoinRequestData
    {
        public string id;
        public string status;
        public string createdAt;
        public TeamUserRef user;
    }

    [Serializable]
    public class TeamMessageResponse
    {
        public string message;
        public string userId;
    }

    [Serializable]
    public class SendTeamChatRequest
    {
        public string text;
    }

    [Serializable]
    public class TeamChatEventData
    {
        public string actorName;
        public string targetName;
    }

    [Serializable]
    public class TeamChatMessage
    {
        public string id;
        /// <summary><c>user</c> hoặc <c>system</c>.</summary>
        public string type;
        public string userId;
        /// <summary>Tên người gửi = PlayerName trong players.info.</summary>
        public string playerName;
        public string text;
        /// <summary>JSON key <c>event</c> — system message event type.</summary>
        public string @event;
        public TeamChatEventData eventData;
        public string timestamp;
    }

    [Serializable]
    public class TeamLivesCooldown
    {
        public string cooldownEndsAt;
    }

    [Serializable]
    public class TeamLivesRequestResponse
    {
        public string cooldownEndsAt;
    }

    [Serializable]
    public class TeamLivesHelperReward
    {
        public int coins;
    }

    [Serializable]
    public class TeamLivesHelpResponse
    {
        public TeamLivesHelperReward helperReward;
        public int totalHelpers;
        public bool fulfilled;
    }

    [Serializable]
    public class HelpTeammateRequest
    {
        public string targetUserId;
    }

    [Serializable]
    public class TeamActiveLivesRequest
    {
        public string userId;
        public int helpersCount;
        /// <summary>Viewer hiện tại đã help request này chưa — client ẩn nút Help nếu true.</summary>
        public bool helpedByMe;
        /// <summary>Thời điểm tạo request (ISO UTC) — dùng sort feed theo thời gian.</summary>
        public string createdAt;
        public string cooldownEndsAt;
    }

    [Serializable]
    public class TeamLivesStatusResponse
    {
        public TeamLivesCooldown myLivesCooldown;
        public TeamActiveLivesRequest[] activeRequests;
    }

    // ─── F1: Ranking + Country ──────────────────────────────────────────────────
    [Serializable]
    public class TeamRankingItem
    {
        public int rank;
        public string id;
        public string name;
        public int badgeId;
        public string type;
        public string countryCode;
        public int minLevelRequired;
        public int memberCount;
        public int maxMembers;
        public int teamScore;
    }

    [Serializable]
    public class TeamRankingResponse
    {
        /// <summary><c>world</c> hoặc <c>country</c>.</summary>
        public string scope;
        public string countryCode;
        public TeamSearchPagination pagination;
        public TeamRankingItem[] teams;
        /// <summary>Team của người gọi kèm hạng trong board này (null nếu không có team / ngoài board country).</summary>
        public TeamRankingItem myTeam;
    }

    // ─── F2: Team Gift ──────────────────────────────────────────────────────────
    [Serializable]
    public class SendGiftRequest
    {
        /// <summary>Payload gift do client tự hiểu (≤ 500 ký tự).</summary>
        public string payload;
    }

    [Serializable]
    public class TeamGiftData
    {
        public string id;
        public string senderId;
        public string payload;
        public string createdAt;
        public string expiresAt;
        /// <summary>Chỉ có ở GET /gifts — user hiện tại đã claim chưa.</summary>
        public bool claimed;
    }

    [Serializable]
    public class ClaimGiftResponse
    {
        /// <summary>Client tự cộng reward từ payload này.</summary>
        public string payload;
    }

    // ─── F3: Ask Card ───────────────────────────────────────────────────────────
    [Serializable]
    public class AskCardRequest
    {
        public int cardIndex;
    }

    [Serializable]
    public class AskCardResponse
    {
        public string cooldownEndsAt;
        public int cardIndex;
    }

    [Serializable]
    public class GiveCardRequest
    {
        /// <summary>userId của người đang xin card.</summary>
        public string targetUserId;
    }

    [Serializable]
    public class GiveCardResponse
    {
        public bool fulfilled;
    }

    [Serializable]
    public class TeamCardCooldown
    {
        public string cooldownEndsAt;
        public int cardIndex;
    }

    [Serializable]
    public class TeamActiveCardRequest
    {
        public string userId;
        public int cardIndex;
        /// <summary>Thời điểm tạo request (ISO UTC) — dùng sort feed theo thời gian.</summary>
        public string createdAt;
        public string cooldownEndsAt;
    }

    [Serializable]
    public class TeamCardStatusResponse
    {
        public TeamCardCooldown myCardCooldown;
        public TeamActiveCardRequest[] activeRequests;
    }

    /// <summary>
    /// Response của endpoint feed gộp (GET /team/{id}/feed) — 1 request thay cho
    /// chat + lives/status + cards/status (+ gifts). Dùng cho poll ở tải cao.
    /// </summary>
    [Serializable]
    public class TeamFeedResponse
    {
        public TeamChatMessage[] chat;
        public TeamLivesStatusResponse lives;
        public TeamCardStatusResponse cards;
        /// <summary>Chỉ có khi gọi kèm gifts=true; ngược lại null.</summary>
        public TeamGiftData[] gifts;
    }

    // Game Models (POST /api/v1/game/start — khớp StartGameDto server)
    [Serializable]
    public class StartGameRequest
    {
        public string gameType;
        public int difficulty;
    }

    [Serializable]
    public class StartGameResponse
    {
        public string sessionId;
        public string gameType;
        public int difficulty;
        public string startTime;
    }
}
