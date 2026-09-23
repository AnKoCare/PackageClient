using UnityEngine;
using Zenject;
using GameBackendModule.Models;
using GameBackendModule.Services;

namespace GameBackendModule.DI
{
    public class GameBackendInstaller : MonoInstaller<GameBackendInstaller>
    {
        [Header("Server URL (Editor / no define)")]
        [SerializeField] public string baseUrl = ApiConstants.BASE_URL;

        [Header("Server URL (theo build define)")]
        [Tooltip("Dùng khi build có CHEAT_ONLY")]
        [SerializeField] private string urlServerTest = "http://136.112.78.71:14500";

        [Tooltip("Dùng khi build có RELEASE_ONLY")]
        [SerializeField] private string urlServerProduct = "https://mahjong.aagamestudio.com";

        private string GetResolvedBaseUrl()
        {
#if CHEAT_ONLY
            return string.IsNullOrEmpty(urlServerTest) ? ApiConstants.BASE_URL : urlServerTest;
#elif RELEASE_ONLY
            return string.IsNullOrEmpty(urlServerProduct) ? ApiConstants.BASE_URL : urlServerProduct;
#else
            return string.IsNullOrEmpty(baseUrl) ? ApiConstants.BASE_URL : baseUrl;
#endif
        }

        private void Awake()
        {
            baseUrl = GetResolvedBaseUrl();
        }

        public override void InstallBindings()
        {
            baseUrl = GetResolvedBaseUrl();

            // Bind API Client
            Container.Bind<IApiClient>()
                .To<ApiClient>()
                .AsSingle()
                .WithArguments(baseUrl);

            // Bind Services
            Container.Bind<IAuthService>()
                .To<AuthService>()
                .AsSingle();

            Container.Bind<IPlayerService>()
                .To<PlayerService>()
                .AsSingle();

            Container.Bind<ILeaderboardService>()
                .To<LeaderboardService>()
                .AsSingle();

            Container.Bind<IClanService>()
                .To<ClanService>()
                .AsSingle();

            Container.Bind<ITeamService>()
                .To<TeamService>()
                .AsSingle();

            Container.Bind<IGameService>()
                .To<GameService>()
                .AsSingle();

            Container.Bind<IUserPurgeService>()
                .To<UserPurgeService>()
                .AsSingle();

            Container.Bind<IWeeklyContestService>()
                .To<WeeklyContestService>()
                .AsSingle();

            Container.Bind<IRoyalLeagueService>()
                .To<RoyalLeagueService>()
                .AsSingle();

            Container.Bind<ICloudSaveService>()
                .To<CloudSaveService>()
                .AsSingle();

            // Bind Game Backend Manager
            Container.Bind<IGameBackendManager>()
                .To<GameBackendManager>()
                .AsSingle();
        }
    }
}