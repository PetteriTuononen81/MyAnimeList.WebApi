using MyAnimeList.Backend.Models;
using MyAnimeList.Backend.Models.Response;

namespace MyAnimeList.Backend.Services.ApiClient
{
    public interface IAnimeApiClient
    {
        Task<AnimeApiResponse> FetchAnimePageAsync(int page = 1, int limit = 25);
        Task<List<Anime>> FetchAnimeListAsync(int page = 1, int limit = 25);
    }
}
