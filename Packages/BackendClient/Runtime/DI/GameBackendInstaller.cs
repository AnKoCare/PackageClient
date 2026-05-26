using UnityEngine;
using Zenject;
using GameBackendModule.Models;
using GameBackendModule.Services;

namespace GameBackendModule.DI
{
    public class GameBackendInstaller : MonoInstaller<GameBackendInstaller>
    {
        [SerializeField] public string baseUrl = ApiConstants.BASE_URL;

        public override void InstallBindings()
        {
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

            Container.Bind<IGameService>()
                .To<GameService>()
                .AsSingle();

            Container.Bind<IUserPurgeService>()
                .To<UserPurgeService>()
                .AsSingle();

            Container.Bind<IWeeklyContestService>()
                .To<WeeklyContestService>()
                .AsSingle();

            // Bind Game Backend Manager
            Container.Bind<IGameBackendManager>()
                .To<GameBackendManager>()
                .AsSingle();
        }
    }
}
