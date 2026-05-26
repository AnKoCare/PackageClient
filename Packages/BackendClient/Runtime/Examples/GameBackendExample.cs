// using System.Collections;
// using UnityEngine;
// using Zenject;
// using GameBackendModule.Services;
// using GameBackendModule.DI;
// using GameBackendModule.Models;

// namespace GameBackendModule.Examples
// {
//     /// <summary>
//     /// Ví dụ về cách sử dụng Game Backend Module trong Unity
//     /// </summary>
//     public class GameBackendExample : MonoBehaviour
//     {
//         [Inject] private IAuthService authService;
//         [Inject] private IPlayerService playerService;
//         [Inject] private ILeaderboardService leaderboardService;
//         [Inject] private IClanService clanService;
//         [Inject] private IGameService gameService;
//         [Inject] private IGameBackendManager backendManager;

//         private void Start()
//         {
//             // Đăng ký các event listeners
//             backendManager.OnAuthenticationChanged += OnAuthenticationChanged;
//             backendManager.OnUserDataChanged += OnUserDataChanged;
//         }

//         private void OnDestroy()
//         {
//             // Hủy đăng ký event listeners
//             if (backendManager != null)
//             {
//                 backendManager.OnAuthenticationChanged -= OnAuthenticationChanged;
//                 backendManager.OnUserDataChanged -= OnUserDataChanged;
//             }
//         }

//         #region Authentication Examples

//         /// <summary>
//         /// Ví dụ đăng ký tài khoản mới
//         /// </summary>
//         public void ExampleRegister()
//         {
//             var registerRequest = new RegisterRequest
//             {
//                 username = "testuser",
//                 email = "test@example.com",
//                 password = "password123",
//                 displayName = "Test User"
//             };

//             StartCoroutine(authService.Register(registerRequest,
//                 (response) => {
//                     if (response.success)
//                     {
//                         Debug.Log($"Đăng ký thành công! Token: {response.data.accessToken}");
//                     }
//                     else
//                     {
//                         Debug.LogError($"Đăng ký thất bại: {response.message}");
//                     }
//                 },
//                 (error) => {
//                     Debug.LogError($"Lỗi đăng ký: {error.message}");
//                 }));
//         }

//         /// <summary>
//         /// Ví dụ đăng nhập
//         /// </summary>
//         public void ExampleLogin()
//         {
//             var loginRequest = new LoginRequest
//             {
//                 email = "test@example.com",
//                 password = "password123"
//             };

//             StartCoroutine(authService.Login(loginRequest,
//                 (response) => {
//                     if (response.success)
//                     {
//                         Debug.Log($"Đăng nhập thành công! User: {response.data.user.displayName}");
//                     }
//                     else
//                     {
//                         Debug.LogError($"Đăng nhập thất bại: {response.message}");
//                     }
//                 },
//                 (error) => {
//                     Debug.LogError($"Lỗi đăng nhập: {error.message}");
//                 }));
//         }

//         /// <summary>
//         /// Ví dụ đăng nhập ngoài (device/gpg/gamecenter)
//         /// </summary>
//         public void ExampleExternalLoginDevice()
//         {
//             var request = new ExternalLoginRequest
//             {
//                 provider = "device",
//                 externalId = SystemInfo.deviceUniqueIdentifier,
//                 displayName = "Guest Player",
//                 // Nếu người chơi đã đăng nhập bằng tài khoản chính, truyền access token để liên kết
//                 mainAccessToken = backendManager != null && backendManager.IsAuthenticated ? backendManager.AuthToken : null
//             };

//             StartCoroutine(authService.ExternalLogin(request,
//                 (response) =>
//                 {
//                     if (response.success)
//                     {
//                         Debug.Log($"External login thành công! User: {response.data.user.displayName}");
//                     }
//                     else
//                     {
//                         Debug.LogError($"External login thất bại: {response.message}");
//                     }
//                 },
//                 (error) =>
//                 {
//                     Debug.LogError($"Lỗi external login: {error.message}");
//                 }));
//         }

//         /// <summary>
//         /// Ví dụ đăng nhập ngoài với Google Play Games (GPG)
//         /// Lưu ý: Cần SDK GPG để lấy playerId/uniqueId hợp lệ từ Google.
//         /// </summary>
//         public void ExampleExternalLoginGPG(string gpgPlayerId)
//         {
//             var request = new ExternalLoginRequest
//             {
//                 provider = "gpg",
//                 externalId = gpgPlayerId,
//                 displayName = "GPG Player",
//                 mainAccessToken = backendManager != null && backendManager.IsAuthenticated ? backendManager.AuthToken : null
//             };

//             StartCoroutine(authService.ExternalLogin(request,
//                 (response) =>
//                 {
//                     if (response.success)
//                     {
//                         Debug.Log($"GPG external login thành công! User: {response.data.user.displayName}");
//                     }
//                     else
//                     {
//                         Debug.LogError($"GPG external login thất bại: {response.message}");
//                     }
//                 },
//                 (error) =>
//                 {
//                     Debug.LogError($"Lỗi GPG external login: {error.message}");
//                 }));
//         }

//         /// <summary>
//         /// Ví dụ đăng nhập ngoài với Apple Game Center
//         /// Lưu ý: Cần SDK Game Center để lấy playerId (Game Center ID) hợp lệ.
//         /// </summary>
//         public void ExampleExternalLoginGameCenter(string gameCenterPlayerId)
//         {
//             var request = new ExternalLoginRequest
//             {
//                 provider = "gamecenter",
//                 externalId = gameCenterPlayerId,
//                 displayName = "GC Player",
//                 mainAccessToken = backendManager != null && backendManager.IsAuthenticated ? backendManager.AuthToken : null
//             };

//             StartCoroutine(authService.ExternalLogin(request,
//                 (response) =>
//                 {
//                     if (response.success)
//                     {
//                         Debug.Log($"Game Center external login thành công! User: {response.data.user.displayName}");
//                     }
//                     else
//                     {
//                         Debug.LogError($"Game Center external login thất bại: {response.message}");
//                     }
//                 },
//                 (error) =>
//                 {
//                     Debug.LogError($"Lỗi Game Center external login: {error.message}");
//                 }));
//         }

//         /// <summary>
//         /// Ví dụ đăng xuất
//         /// </summary>
//         public void ExampleLogout()
//         {
//             backendManager.ClearAuth();
//             Debug.Log("Đã đăng xuất!");
//         }

//         #endregion

//         #region Player Examples

//         /// <summary>
//         /// Ví dụ lấy thông tin profile player
//         /// </summary>
//         public void ExampleGetPlayerProfile()
//         {
//             StartCoroutine(playerService.GetProfile(
//                 (response) => {
//                     if (response.success)
//                     {
//                         var profile = response.data;
//                         Debug.Log($"Player Level: {profile.level}, Experience: {profile.experience}, Coins: {profile.coins}");
//                     }
//                     else
//                     {
//                         Debug.LogError($"Không thể lấy profile: {response.message}");
//                     }
//                 },
//                 (error) => {
//                     Debug.LogError($"Lỗi lấy profile: {error.message}");
//                 }));
//         }

//         /// <summary>
//         /// Ví dụ thêm experience cho player
//         /// </summary>
//         public void ExampleAddExperience()
//         {
//             var request = new AddExperienceRequest
//             {
//                 amount = 100
//             };

//             StartCoroutine(playerService.AddExperience(request,
//                 (response) => {
//                     if (response.success)
//                     {
//                         Debug.Log($"Đã thêm {request.amount} experience! Level mới: {response.data.level}");
//                     }
//                     else
//                     {
//                         Debug.LogError($"Không thể thêm experience: {response.message}");
//                     }
//                 },
//                 (error) => {
//                     Debug.LogError($"Lỗi thêm experience: {error.message}");
//                 }));
//         }

//         /// <summary>
//         /// Ví dụ cập nhật điểm số
//         /// </summary>
//         public void ExampleUpdateScore()
//         {
//             var request = new UpdateScoreRequest
//             {
//                 score = 1500,
//                 won = true
//             };

//             StartCoroutine(playerService.UpdateScore(request,
//                 (response) => {
//                     if (response.success)
//                     {
//                         Debug.Log($"Đã cập nhật điểm số! Tổng điểm: {response.data.totalScore}");
//                     }
//                     else
//                     {
//                         Debug.LogError($"Không thể cập nhật điểm số: {response.message}");
//                     }
//                 },
//                 (error) => {
//                     Debug.LogError($"Lỗi cập nhật điểm số: {error.message}");
//                 }));
//         }

//         #endregion

//         #region Player Save Examples

//         /// <summary>
//         /// Ví dụ lấy JSON save của người chơi hiện tại
//         /// </summary>
//         public void ExampleGetMyPlayerSave()
//         {
//             StartCoroutine(playerService.GetMyPlayerSave(
//                 (response) =>
//                 {
//                     if (response.success)
//                     {
//                         Debug.Log($"PlayerSave version: {response.data.version}");
//                     }
//                     else
//                     {
//                         Debug.LogError($"Không thể lấy player save: {response.message}");
//                     }
//                 },
//                 (error) =>
//                 {
//                     Debug.LogError($"Lỗi lấy player save: {error.message}");
//                 }));
//         }

//         /// <summary>
//         /// Ví dụ upsert JSON save của người chơi hiện tại (chỉ chấp nhận version cao hơn)
//         /// </summary>
//         public void ExampleUpsertMyPlayerSave()
//         {
//             var save = new SaveDataRequest
//             {
//                 data = SimpleJsonSerializer.ToJson(new System.Collections.Generic.Dictionary<string, object>
//                 {
//                     { "userdata", new System.Collections.Generic.Dictionary<string, object> { { "Level", 201 } } }
//                 }),
//                 version = ((System.DateTimeOffset)System.DateTime.UtcNow).ToUnixTimeSeconds().ToString()
//             };

//             StartCoroutine(playerService.UpsertMyPlayerSave(save,
//                 (response) =>
//                 {
//                     if (response.success)
//                     {
//                         Debug.Log($"Đã lưu player save với version: {response.data.version}");
//                     }
//                     else
//                     {
//                         Debug.LogError($"Không thể lưu player save: {response.message}");
//                     }
//                 },
//                 (error) =>
//                 {
//                     Debug.LogError($"Lỗi lưu player save: {error.message}");
//                 }));
//         }

//         #endregion

//         #region Leaderboard Examples

//         /// <summary>
//         /// Ví dụ lấy bảng xếp hạng toàn cầu
//         /// </summary>
//         public void ExampleGetGlobalLeaderboard()
//         {
//             var request = new GetLeaderboardRequest
//             {
//                 type = "global",
//                 limit = 10,
//                 offset = 0
//             };

//             StartCoroutine(leaderboardService.GetLeaderboard(request,
//                 (response) => {
//                     if (response.success)
//                     {
//                         Debug.Log($"Bảng xếp hạng toàn cầu (Top {response.data.Length}):");
//                         for (int i = 0; i < response.data.Length; i++)
//                         {
//                             var entry = response.data[i];
//                             Debug.Log($"{i + 1}. {entry.playerName} - {entry.score} điểm");
//                         }
//                     }
//                     else
//                     {
//                         Debug.LogError($"Không thể lấy bảng xếp hạng: {response.message}");
//                     }
//                 },
//                 (error) => {
//                     Debug.LogError($"Lỗi lấy bảng xếp hạng: {error.message}");
//                 }));
//         }

//         /// <summary>
//         /// Ví dụ lấy rank của player hiện tại
//         /// </summary>
//         public void ExampleGetPlayerRank()
//         {
//             StartCoroutine(leaderboardService.GetPlayerRank("global", null,
//                 (response) => {
//                     if (response.success)
//                     {
//                         Debug.Log($"Rank của bạn: {response.data.rank}/{response.data.totalPlayers}");
//                     }
//                     else
//                     {
//                         Debug.LogError($"Không thể lấy rank: {response.message}");
//                     }
//                 },
//                 (error) => {
//                     Debug.LogError($"Lỗi lấy rank: {error.message}");
//                 }));
//         }

//         #endregion

//         #region Clan Examples

//         /// <summary>
//         /// Ví dụ tạo clan mới
//         /// </summary>
//         public void ExampleCreateClan()
//         {
//             var request = new CreateClanRequest
//             {
//                 name = "My Awesome Clan",
//                 description = "Clan tuyệt vời nhất!",
//                 maxMembers = 30,
//                 isPrivate = false
//             };

//             StartCoroutine(clanService.CreateClan(request,
//                 (response) => {
//                     if (response.success)
//                     {
//                         Debug.Log($"Đã tạo clan thành công! ID: {response.data.id}");
//                     }
//                     else
//                     {
//                         Debug.LogError($"Không thể tạo clan: {response.message}");
//                     }
//                 },
//                 (error) => {
//                     Debug.LogError($"Lỗi tạo clan: {error.message}");
//                 }));
//         }

//         /// <summary>
//         /// Ví dụ tìm kiếm clan
//         /// </summary>
//         public void ExampleSearchClans()
//         {
//             var request = new SearchClanRequest
//             {
//                 name = "Awesome",
//                 limit = 10,
//                 offset = 0
//             };

//             StartCoroutine(clanService.SearchClans(request,
//                 (response) => {
//                     if (response.success)
//                     {
//                         Debug.Log($"Tìm thấy {response.data.Length} clan:");
//                         foreach (var clan in response.data)
//                         {
//                             Debug.Log($"- {clan.name}: {clan.description} ({clan.memberCount}/{clan.maxMembers} thành viên)");
//                         }
//                     }
//                     else
//                     {
//                         Debug.LogError($"Không thể tìm kiếm clan: {response.message}");
//                     }
//                 },
//                 (error) => {
//                     Debug.LogError($"Lỗi tìm kiếm clan: {error.message}");
//                 }));
//         }

//         #endregion

//         #region Game Examples

//         /// <summary>
//         /// Ví dụ bắt đầu game mới
//         /// </summary>
//         public void ExampleStartGame()
//         {
//             var request = new StartGameRequest
//             {
//                 gameMode = "classic",
//                 gameSettings = new System.Collections.Generic.Dictionary<string, object>
//                 {
//                     { "difficulty", "normal" },
//                     { "timeLimit", 300 }
//                 }
//             };

//             StartCoroutine(gameService.StartGame(request,
//                 (response) => {
//                     if (response.success)
//                     {
//                         Debug.Log($"Game đã bắt đầu! Session ID: {response.data.id}");
//                     }
//                     else
//                     {
//                         Debug.LogError($"Không thể bắt đầu game: {response.message}");
//                     }
//                 },
//                 (error) => {
//                     Debug.LogError($"Lỗi bắt đầu game: {error.message}");
//                 }));
//         }

//         /// <summary>
//         /// Ví dụ nộp kết quả game
//         /// </summary>
//         public void ExampleSubmitGameResult()
//         {
//             var request = new SubmitGameResultRequest
//             {
//                 gameId = "game-session-id",
//                 score = 2500,
//                 duration = 180,
//                 gameData = new System.Collections.Generic.Dictionary<string, object>
//                 {
//                     { "enemiesDefeated", 15 },
//                     { "powerUpsUsed", 3 },
//                     { "accuracy", 0.85f }
//                 }
//             };

//             StartCoroutine(gameService.SubmitGameResult(request,
//                 (response) => {
//                     if (response.success)
//                     {
//                         Debug.Log("Đã nộp kết quả game thành công!");
//                     }
//                     else
//                     {
//                         Debug.LogError($"Không thể nộp kết quả: {response.message}");
//                     }
//                 },
//                 (error) => {
//                     Debug.LogError($"Lỗi nộp kết quả: {error.message}");
//                 }));
//         }

//         #endregion

//         #region Event Handlers

//         private void OnAuthenticationChanged(bool isAuthenticated)
//         {
//             Debug.Log($"Trạng thái đăng nhập: {(isAuthenticated ? "Đã đăng nhập" : "Chưa đăng nhập")}");
//         }

//         private void OnUserDataChanged(UserData userData)
//         {
//             if (userData != null)
//             {
//                 Debug.Log($"Thông tin user đã cập nhật: {userData.displayName} ({userData.username})");
//             }
//             else
//             {
//                 Debug.Log("User data đã được xóa");
//             }
//         }

//         #endregion
//     }
// }
