﻿using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MinimalAPIsMovies.DTOs;
using MinimalAPIsMovies.Entities;
using MinimalAPIsMovies.Filters;
using MinimalAPIsMovies.Repositories;
using MinimalAPIsMovies.Services;
using System.ComponentModel;
using System.Linq;

namespace MinimalAPIsMovies.Endpoints
{
    public static class MoviesEndpoint
    {
        private readonly static string container = "movies";
        public static RouteGroupBuilder MapMovies(this RouteGroupBuilder group)
        {
            group.MapGet("/", GetAll)
               .CacheOutput(c => c.Expire(TimeSpan.FromSeconds(60)).Tag("movies-get"));
            group.MapGet("/{id:int}", GetById);
            group.MapPost("/", Create).DisableAntiforgery()
                .AddEndpointFilter<ValidationFilter<CreateMovieDTO>>();
            group.MapPut("/{id:int}", Update).DisableAntiforgery()
                 .AddEndpointFilter<ValidationFilter<CreateMovieDTO>>(); ;
            group.MapDelete("/{id:int}", Delete);
            group.MapPost("/{id:int}/assignGenres", AssignGenres);
            group.MapPost("/{id:int}/assignActors", AssignActors);
            
            return group;
        }

        static async Task<Ok<List<MovieDTO>>> GetAll(IMoviesRepository repository,
            IMapper mapper, int page = 1, int recordsPerPage = 10)
        {
            var pagination = new PaginationDTO { Page = page, RecordsPerPage = recordsPerPage };
            var movies = await repository.GetAll(pagination);
            var moviesDTO = mapper.Map<List<MovieDTO>>(movies);
            return TypedResults.Ok(moviesDTO);
        }

        static async Task<Results<Ok<MovieDTO>, NotFound>> GetById(int id,
            IMoviesRepository repository, IMapper mapper)
        {
            var movie = await repository.GetById(id);

            if (movie is null)
            {
                return TypedResults.NotFound();
            }

            var movieDTO = mapper.Map<MovieDTO>(movie);
            return TypedResults.Ok(movieDTO);
        }


        static async Task<Created<MovieDTO>> Create([FromForm] CreateMovieDTO createMovieDTO,
            IFileStorage fileStorage, IOutputCacheStore outputCacheStore,
            IMapper mapper, IMoviesRepository repository)
        {
            var movie = mapper.Map<Movie>(createMovieDTO);

            if (createMovieDTO.Poster is not null)
            {
                var url = await fileStorage.Store(container, createMovieDTO.Poster);
                movie.poster = url;
            }

            var id = await repository.Create(movie);
            await outputCacheStore.EvictByTagAsync("movies-get", default);
            var movieDTO = mapper.Map<MovieDTO>(movie);
            return TypedResults.Created($"movies/{id}", movieDTO);
        }

        static async Task<Results<NoContent, NotFound>> Update(int id,
            [FromForm] CreateMovieDTO createMovieDTO, IMoviesRepository repository,
            IFileStorage fileStorage, IOutputCacheStore outputCacheStore,
            IMapper mapper)
        {
            var movieDB = await repository.GetById(id);

            if (movieDB is null)
            {
                return TypedResults.NotFound();
            }

            var movieForUpdate = mapper.Map<Movie>(createMovieDTO);
            movieForUpdate.Id = id;
            movieForUpdate.poster = movieDB.poster;

            if (createMovieDTO.Poster is not null)
            {
                var url = await fileStorage.Edit(movieForUpdate.poster, container,
                    createMovieDTO.Poster);
                movieForUpdate.poster = url;
            }

            await repository.Update(movieForUpdate);
            await outputCacheStore.EvictByTagAsync("movies-get", default);
            return TypedResults.NoContent();
        }

        static async Task<Results<NoContent, NotFound>> Delete(int id, IMoviesRepository repository,
            IOutputCacheStore outputCacheStore, IFileStorage fileStorage)
        {
            var movieDB = await repository.GetById(id);

            if (movieDB is null)
            {
                return TypedResults.NotFound();
            }

            await repository.Delete(id);
            await fileStorage.Delete(movieDB.poster, container);
            await outputCacheStore.EvictByTagAsync("movies-get", default);
            return TypedResults.NoContent();
        }

        static async Task<Results<NoContent, NotFound, BadRequest<string>>> AssignGenres
            (int id, List<int> genresIds, IMoviesRepository moviesRepository,
            IGenresRepository genresRepository)
        {
            if (!await moviesRepository.Exists(id))
            {
                return TypedResults.NotFound();
            }

            var existingGenres = new List<int>();

            if (genresIds.Count != 0)
            {
                existingGenres = await genresRepository.Exists(genresIds);
            }

            if (genresIds.Count != existingGenres.Count)
            {
                var nonExistingGenres = genresIds.Except(existingGenres);

                var nonExistingGenresCSV = string.Join(",", nonExistingGenres);

                return TypedResults.BadRequest($"The genres of id {nonExistingGenresCSV} does not exist.");
            }

            await moviesRepository.Assign(id, genresIds);
            return TypedResults.NoContent();
        }

        static async Task<Results<NotFound, NoContent, BadRequest<string>>> AssignActors
            (int id, List<AssignActorMovieDTOs> actorsDTO, IMoviesRepository moviesRepository,
            IActorsRepository actorsRepository, IMapper mapper)
        {
            if (!await moviesRepository.Exists(id))
            {
                return TypedResults.NotFound();
            }

            var existingActors = new List<int>();
            var actorsIds = actorsDTO.Select(a => a.ActorId).ToList();

            if (actorsDTO.Count != 0)
            {
                existingActors = await actorsRepository.Exists(actorsIds);
            }

            if (existingActors.Count != actorsDTO.Count)
            {
                var nonExistingActors = actorsIds.Except(existingActors);
                var nonExistingActorsCSV = string.Join(",", nonExistingActors);
                return TypedResults.BadRequest($"The actors of id {nonExistingActorsCSV} do not exists");
            }

            var actors = mapper.Map<List<ActorMovie>>(actorsDTO);
            await moviesRepository.Assign(id, actors);
            return TypedResults.NoContent();
        }

    } 
}
