﻿using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MinimalAPIsMovies.DTOs;
using MinimalAPIsMovies.Entities;
using MinimalAPIsMovies.Migrations;

namespace MinimalAPIsMovies.Repositories
{
    public class MoviesRepository(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context, IMapper mapper) : IMoviesRepository
    {
        public async Task<List<Movie>> GetAll(PaginationDTO pagination)
        {
            var queryable = context.Movies.AsQueryable();
            await httpContextAccessor.HttpContext!
                .InsertPaginationParameterInResponseHeader(queryable);
            return await queryable.OrderBy(m => m.Title).Paginate(pagination).ToListAsync();
        }

        public async Task<Movie?> GetById(int id)
        {
            return await context.Movies
                .Include(m => m.Comments)
                .Include(m => m.GenresMovies)
                    .ThenInclude(gm => gm.Genre)
                .Include(m => m.ActorsMovies.OrderBy(am => am.Order))
                    .ThenInclude(am => am.Actor)
                .AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<bool> Exists(int id)
        {
            return await context.Movies.AnyAsync(m => m.Id == id);
        }

        public async Task<int> Create(Movie movie)
        {
            context.Add(movie);
            await context.SaveChangesAsync();
            return movie.Id;
        }

        public async Task Update(Movie movie)
        {
            context.Update(movie);
            await context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
           await context.Movies.Where(m => m.Id == id).ExecuteDeleteAsync();    
        }


        //method to recieve a list of genres into a movie
        public async Task Assign(int id, List<int> genresIds)
        {
           

            var movie = await context.Movies.Include(m => m.GenresMovies)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie is null)
            {
                throw new ArgumentException($"There's not movie with id {id}");
            }

           // A new collection of GenreMovie objects is created from the provided genresIds
             var genresMovies = genresIds.Select(genreId => new GenreMovie  { GenreId = genreId });

            // Map the new genres to the movie, maintaining the same instance in memory
            movie.GenresMovies = mapper.Map(genresMovies, movie.GenresMovies);

            await context.SaveChangesAsync();
        }

        public async Task Assign(int id, List<ActorMovie> actors)
        {
            for (int i = 1; i <= actors.Count; i++)
            {
                actors[i - 1].Order = i;
            }

            var movie = await context.Movies.Include(m => m.ActorsMovies)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie is null)
            {
                throw new ArgumentException($"There's not movie with id {id}");
            }

            movie.ActorsMovies = mapper.Map(actors, movie.ActorsMovies);

            await context.SaveChangesAsync();
        }
    }

}
