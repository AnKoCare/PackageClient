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
        public int LogoTeamIndex;
        public int WinStreak;
        public int TotalCoinEarn;
        public int BoosterUsed;
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
        public int total;
        public LeaderboardTopEntry[] entries;
    }

    [Serializable]
    public class LeaderboardRankResponse
    {
        public int rank;
        public string uid;
        public int score;
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
