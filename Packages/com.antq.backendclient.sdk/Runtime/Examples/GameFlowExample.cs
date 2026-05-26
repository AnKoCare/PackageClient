// using UnityEngine;
// using Zenject;
// using GameBackendModule.DI;
// using GameBackendModule.Services;
// using GameBackendModule.Models;

// namespace GameBackendModule.Examples
// {
//     /// <summary>
//     /// Ví dụ về cách sử dụng Game Backend Module trong một game thực tế
//     /// Script này minh họa cách tích hợp module vào game flow
//     /// </summary>
//     public class GameFlowExample : MonoBehaviour
//     {
//         [Header("Game Settings")]
//         [SerializeField] private string gameMode = "classic";
//         [SerializeField] private int timeLimit = 300;
        
//         [Inject] private IAuthService authService;
//         [Inject] private IPlayerService playerService;
//         [Inject] private ILeaderboardService leaderboardService;
//         [Inject] private IGameService gameService;
//         [Inject] private IGameBackendManager backendManager;

//         private string currentGameSessionId;
//         private int currentScore = 0;
//         private float gameStartTime;

//         private void Start()
//         {
//             // Đăng ký event listeners
//             backendManager.OnAuthenticationChanged += OnAuthenticationChanged;
            
//             // Kiểm tra trạng thái đăng nhập
//             if (!backendManager.IsAuthenticated)
//             {
//                 ShowLoginScreen();
//             }
//             else
//             {
//                 ShowMainMenu();
//             }
//         }

//         private void OnDestroy()
//         {
//             if (backendManager != null)
//             {
//                 backendManager.OnAuthenticationChanged -= OnAuthenticationChanged;
//             }
//         }

//         #region Authentication Flow

//         private void ShowLoginScreen()
//         {
//             Debug.Log("Hiển thị màn hình đăng nhập");
//             // Ở đây bạn sẽ hiển thị UI đăng nhập
//         }

//         private void ShowMainMenu()
//         {
//             Debug.Log("Hiển thị menu chính");
//             // Ở đây bạn sẽ hiển thị menu chính của game
//         }

//         public void OnLoginButtonClicked(string email, string password)
//         {
//             var loginRequest = new LoginRequest
//             {
//                 email = email,
//                 password = password
//             };

//             StartCoroutine(authService.Login(loginRequest,
//                 (response) => {
//                     if (response.success)
//                     {
//                         Debug.Log($"Đăng nhập thành công! Chào mừng {response.data.user.displayName}");
//                         ShowMainMenu();
//                         LoadPlayerData();
//                     }
//                     else
//                     {
//                         Debug.LogError($"Đăng nhập thất bại: {response.message}");
//                         ShowLoginError(response.message);
//                     }
//                 },
//                 (error) => {
//                     Debug.LogError($"Lỗi đăng nhập: {error.message}");
//                     ShowLoginError(error.message);
//                 }));
//         }

//         public void OnRegisterButtonClicked(string username, string email, string password, string displayName)
//         {
//             var registerRequest = new RegisterRequest
//             {
//                 username = username,
//                 email = email,
//                 password = password,
//                 displayName = displayName
//             };

//             StartCoroutine(authService.Register(registerRequest,
//                 (response) => {
//                     if (response.success)
//                     {
//                         Debug.Log($"Đăng ký thành công! Chào mừng {response.data.user.displayName}");
//                         ShowMainMenu();
//                         LoadPlayerData();
//                     }
//                     else
//                     {
//                         Debug.LogError($"Đăng ký thất bại: {response.message}");
//                         ShowLoginError(response.message);
//                     }
//                 },
//                 (error) => {
//                     Debug.LogError($"Lỗi đăng ký: {error.message}");
//                     ShowLoginError(error.message);
//                 }));
//         }

//         private void ShowLoginError(string message)
//         {
//             Debug.LogError($"Lỗi: {message}");
//             // Ở đây bạn sẽ hiển thị thông báo lỗi trên UI
//         }

//         #endregion

//         #region Player Data Management

//         private void LoadPlayerData()
//         {
//             StartCoroutine(playerService.GetProfile(
//                 (response) => {
//                     if (response.success)
//                     {
//                         var profile = response.data;
//                         Debug.Log($"Player Level: {profile.level}, Coins: {profile.coins}, Gems: {profile.gems}");
//                         UpdatePlayerUI(profile);
//                     }
//                     else
//                     {
//                         Debug.LogError($"Không thể tải dữ liệu player: {response.message}");
//                     }
//                 },
//                 (error) => {
//                     Debug.LogError($"Lỗi tải dữ liệu player: {error.message}");
//                 }));
//         }

//         private void UpdatePlayerUI(PlayerProfile profile)
//         {
//             Debug.Log($"Cập nhật UI với Level: {profile.level}, Coins: {profile.coins}");
//             // Ở đây bạn sẽ cập nhật UI hiển thị thông tin player
//         }

//         #endregion

//         #region Game Flow

//         public void OnStartGameButtonClicked()
//         {
//             var startRequest = new StartGameRequest
//             {
//                 gameMode = gameMode,
//                 gameSettings = new System.Collections.Generic.Dictionary<string, object>
//                 {
//                     { "difficulty", "normal" },
//                     { "timeLimit", timeLimit }
//                 }
//             };

//             StartCoroutine(gameService.StartGame(startRequest,
//                 (response) => {
//                     if (response.success)
//                     {
//                         currentGameSessionId = response.data.id;
//                         gameStartTime = Time.time;
//                         currentScore = 0;
                        
//                         Debug.Log($"Game đã bắt đầu! Session ID: {currentGameSessionId}");
//                         StartGameplay();
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

//         private void StartGameplay()
//         {
//             Debug.Log("Bắt đầu gameplay!");
//             // Ở đây bạn sẽ khởi tạo gameplay logic
//         }

//         public void OnGameEnded(int finalScore, float duration)
//         {
//             if (string.IsNullOrEmpty(currentGameSessionId))
//             {
//                 Debug.LogError("Không có game session để nộp kết quả!");
//                 return;
//             }

//             var resultRequest = new SubmitGameResultRequest
//             {
//                 gameId = currentGameSessionId,
//                 score = finalScore,
//                 duration = (int)duration,
//                 gameData = new System.Collections.Generic.Dictionary<string, object>
//                 {
//                     { "enemiesDefeated", UnityEngine.Random.Range(10, 20) },
//                     { "powerUpsUsed", UnityEngine.Random.Range(1, 5) },
//                     { "accuracy", UnityEngine.Random.Range(0.7f, 1.0f) }
//                 }
//             };

//             StartCoroutine(gameService.SubmitGameResult(resultRequest,
//                 (response) => {
//                     if (response.success)
//                     {
//                         Debug.Log("Đã nộp kết quả game thành công!");
//                         OnGameResultSubmitted(finalScore);
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

//         private void OnGameResultSubmitted(int score)
//         {
//             // Cập nhật leaderboard
//             var updateRequest = new UpdateLeaderboardRequest
//             {
//                 score = score,
//                 type = "global"
//             };

//             StartCoroutine(leaderboardService.UpdateLeaderboard(updateRequest,
//                 (response) => {
//                     if (response.success)
//                     {
//                         Debug.Log("Đã cập nhật leaderboard!");
//                         ShowGameResults(score);
//                     }
//                     else
//                     {
//                         Debug.LogError($"Không thể cập nhật leaderboard: {response.message}");
//                     }
//                 },
//                 (error) => {
//                     Debug.LogError($"Lỗi cập nhật leaderboard: {error.message}");
//                 }));
//         }

//         private void ShowGameResults(int score)
//         {
//             Debug.Log($"Game kết thúc! Điểm số: {score}");
            
//             // Lấy rank của player
//             StartCoroutine(leaderboardService.GetPlayerRank("global", null,
//                 (response) => {
//                     if (response.success)
//                     {
//                         Debug.Log($"Rank của bạn: {response.data.rank}/{response.data.totalPlayers}");
//                         ShowRankUI(response.data.rank, response.data.totalPlayers);
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

//         private void ShowRankUI(int rank, int totalPlayers)
//         {
//             Debug.Log($"Hiển thị UI kết quả: Rank {rank}/{totalPlayers}");
//             // Ở đây bạn sẽ hiển thị UI kết quả game với rank
//         }

//         #endregion

//         #region Leaderboard

//         public void OnShowLeaderboardButtonClicked()
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
//                         Debug.Log($"Bảng xếp hạng (Top {response.data.Length}):");
//                         for (int i = 0; i < response.data.Length; i++)
//                         {
//                             var entry = response.data[i];
//                             Debug.Log($"{i + 1}. {entry.playerName} - {entry.score} điểm");
//                         }
//                         ShowLeaderboardUI(response.data);
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

//         private void ShowLeaderboardUI(LeaderboardEntry[] entries)
//         {
//             Debug.Log("Hiển thị UI bảng xếp hạng");
//             // Ở đây bạn sẽ hiển thị UI bảng xếp hạng
//         }

//         #endregion

//         #region Event Handlers

//         private void OnAuthenticationChanged(bool isAuthenticated)
//         {
//             if (isAuthenticated)
//             {
//                 Debug.Log("Người dùng đã đăng nhập");
//                 ShowMainMenu();
//                 LoadPlayerData();
//             }
//             else
//             {
//                 Debug.Log("Người dùng đã đăng xuất");
//                 ShowLoginScreen();
//             }
//         }

//         #endregion

//         #region Public Methods for UI

//         public void AddCoins(int amount)
//         {
//             var request = new AddCoinsRequest { amount = amount };
            
//             StartCoroutine(playerService.AddCoins(request,
//                 (response) => {
//                     if (response.success)
//                     {
//                         Debug.Log($"Đã thêm {amount} coins! Tổng coins: {response.data.coins}");
//                         UpdatePlayerUI(response.data);
//                     }
//                     else
//                     {
//                         Debug.LogError($"Không thể thêm coins: {response.message}");
//                     }
//                 },
//                 (error) => {
//                     Debug.LogError($"Lỗi thêm coins: {error.message}");
//                 }));
//         }

//         public void AddGems(int amount)
//         {
//             var request = new AddGemsRequest { amount = amount };
            
//             StartCoroutine(playerService.AddGems(request,
//                 (response) => {
//                     if (response.success)
//                     {
//                         Debug.Log($"Đã thêm {amount} gems! Tổng gems: {response.data.gems}");
//                         UpdatePlayerUI(response.data);
//                     }
//                     else
//                     {
//                         Debug.LogError($"Không thể thêm gems: {response.message}");
//                     }
//                 },
//                 (error) => {
//                     Debug.LogError($"Lỗi thêm gems: {error.message}");
//                 }));
//         }

//         public void Logout()
//         {
//             backendManager.ClearAuth();
//             Debug.Log("Đã đăng xuất");
//         }

//         #endregion
//     }
// }
