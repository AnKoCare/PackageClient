using System;
using System.Collections;
using GameBackendModule.Models;

namespace GameBackendModule.Services
{
    public interface IGameService
    {
        IEnumerator StartGame(StartGameRequest request, Action<ApiResponse<StartGameResponse>> onSuccess, Action<ErrorResponse> onError);
    }

    public class GameService : IGameService
    {
        private readonly IApiClient apiClient;

        public GameService(IApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        public IEnumerator StartGame(StartGameRequest request, Action<ApiResponse<StartGameResponse>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return apiClient.Post(ApiConstants.START_GAME_ENDPOINT, request, onSuccess, onError);
        }
    }
}
