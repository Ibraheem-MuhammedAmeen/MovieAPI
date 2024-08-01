namespace MinimalAPIsMovies.Services
{
    public interface IFileStorage
    {
        //recieving a container with is the folder and a file which is the picture itself
        Task<string> Store(string container, IFormFile file);
        Task Delete(string? route, string container); 
        async Task<string> Edit(string? route, string container, IFormFile file)
        {
            await Delete(route, container);
            return await Store(container, file);
        }
    }
}
