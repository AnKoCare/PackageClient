using System;
using UnityEngine;

namespace GameBackendModule.Models
{
    public static class ApiConstants
    {
        public const string BASE_URL = "http://localhost:3000";
        
        // Authentication endpoints
        public const string REGISTER_ENDPOINT = "/api/v1/auth/register";
        public const string LOGIN_ENDPOINT = "/api/v1/auth/login";
        public const string REFRESH_TOKEN_ENDPOINT = "/api/v1/auth/refresh";
        public const string CHANGE_PASSWORD_ENDPOINT = "/api/v1/auth/change-password";
        public const string EXTERNAL_LOGIN_ENDPOINT = "/api/v1/auth/external-login";
        
        // Player endpoints
        public const string PLAYER_PROFILE_ENDPOINT = "/api/v1/player/profile";
        public const string PLAYER_INFO_ENDPOINT = "/api/v1/player/info";
        public const string PLAYER_COUNTRY_ENDPOINT = "/api/v1/player/country";
        public const string PLAYER_SAVE_ENDPOINT = "/api/v1/player/save";
        public const string DELETE_PLAYER_ENDPOINT = "/api/v1/player/{0}";

        /// <summary>POST purge-by-uid — Bearer JWT (chủ tài khoản hoặc admin).</summary>
        public const string USERS_PURGE_BY_UID_ENDPOINT = "/api/v1/users/purge-by-uid";

        // Weekly contest (Bearer JWT)
        public const string WEEKLY_CONTEST_STATUS_ENDPOINT = "/api/v1/weekly-contest/status";
        public const string WEEKLY_CONTEST_CLAIM_ENDPOINT = "/api/v1/weekly-contest/claim";
        public const string WEEKLY_CONTEST_ADD_SCORE_ENDPOINT = "/api/v1/weekly-contest/add-score";
        /// <summary>POST [DEV] ép kết thúc tuần open — Bearer JWT (+ dev key trên production).</summary>
        public const string WEEKLY_CONTEST_DEV_END_WEEK_ENDPOINT = "/api/v1/weekly-contest/dev/end-week";
        public const string WEEKLY_CONTEST_DEV_KEY_HEADER = "X-Weekly-Contest-Dev-Key";

        // Leaderboard (POST submit cần JWT; GET top/rank public)
        public const string LEADERBOARD_SUBMIT_ENDPOINT = "/api/v1/leaderboard/submit";
        public const string LEADERBOARD_TOP_ENDPOINT = "/api/v1/leaderboard/top";
        public const string LEADERBOARD_RANK_ENDPOINT = "/api/v1/leaderboard/rank";
        /// <summary>Mã bảng World trên server (ISO + 'ww').</summary>
        public const string LEADERBOARD_WORLD_COUNTRY_CODE = "ww";
        
        // Clan endpoints
        public const string CREATE_CLAN_ENDPOINT = "/api/v1/clan";
        public const string CLAN_DETAILS_ENDPOINT = "/api/v1/clan/{0}";
        public const string UPDATE_CLAN_ENDPOINT = "/api/v1/clan/{0}";
        public const string JOIN_CLAN_ENDPOINT = "/api/v1/clan/join";
        public const string LEAVE_CLAN_ENDPOINT = "/api/v1/clan/leave";
        public const string KICK_MEMBER_ENDPOINT = "/api/v1/clan/{0}/kick";
        public const string PROMOTE_MEMBER_ENDPOINT = "/api/v1/clan/{0}/promote";
        public const string CLAN_MEMBERS_ENDPOINT = "/api/v1/clan/{0}/members";
        public const string SEARCH_CLAN_ENDPOINT = "/api/v1/clan/search";
        
        // Game endpoints
        public const string START_GAME_ENDPOINT = "/api/v1/game/start";
        
        // HTTP Headers
        public const string AUTHORIZATION_HEADER = "Authorization";
        public const string CONTENT_TYPE_HEADER = "Content-Type";
        public const string CONTENT_TYPE_JSON = "application/json";
        public const string BEARER_PREFIX = "Bearer ";
    }
}
