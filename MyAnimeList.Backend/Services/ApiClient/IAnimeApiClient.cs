using MyAnimeList.Backend.Models;

namespace MyAnimeList.Backend.Services.ApiClient
{
    public interface IAnimeApiClient
    {
        Task<List<Anime>> FetchAnimeListAsync(int page = 1, int limit = 25);
    }

}
