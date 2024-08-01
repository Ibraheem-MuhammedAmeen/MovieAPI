using Microsoft.AspNetCore.Cors;
using MinimalAPIsMovies.Entities;
using Microsoft.AspNetCore.Builder;
using MinimalAPIsMovies;
using Microsoft.EntityFrameworkCore;
using MinimalAPIsMovies.Repositories;
using Microsoft.AspNetCore.OutputCaching;
using MinimalAPIsMovies.Endpoints;
using MinimalAPIsMovies.Services;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MinimalAPIsMovies.Utilities;

var builder = WebApplication.CreateBuilder(args);

// Services zone - BEGIN

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer("name=DefaultConnection"));

builder.Services.AddIdentityCore<IdentityUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<UserManager<IdentityUser>>();
builder.Services.AddScoped<SignInManager<IdentityUser>>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(configuration =>
    {
        configuration.WithOrigins(builder.Configuration["allowedOrigins"]!).AllowAnyMethod()
        .AllowAnyHeader();
    });

    options.AddPolicy("free", configuration =>
    {
        configuration.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddOutputCache();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IGenresRepository, GenreRepository>();
builder.Services.AddScoped<IActorsRepository, ActorsRepository>();
builder.Services.AddScoped<IMoviesRepository, MoviesRepository>();
builder.Services.AddScoped<ICommentsRepository, CommentsRepository>();
builder.Services.AddTransient<IFileStorage, LocalFileStorage>();
builder.Services.AddScoped<IErrorsRepository, ErrorsRepository>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddProblemDetails();

builder.Services.AddAuthentication().AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = false,
    ValidateAudience = false,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ClockSkew = TimeSpan.Zero,
    IssuerSigningKeys = KeysHandler.GetAllKeys(builder.Configuration),
});
builder.Services.AddAuthorization();

// Services zone - END

var app = builder.Build();

// Middlewares zone -BEGIN 

app.UseSwagger();
app.UseSwaggerUI();


app.UseExceptionHandler(exceptionHandlerApp => exceptionHandlerApp.Run(async context =>
{

    var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
    var exception = exceptionHandlerFeature?.Error!;

    var error = new Error();
    error.ErrorMessage = exception.Message;
    error.StackTrace = exception.StackTrace;

    var repository = context.RequestServices.GetRequiredService<IErrorsRepository>();
    await repository.Create(error);

    await Results
    .BadRequest(new
    {
        type = "error",
        message = "an unexpected exception has occurred",
        status = 500
    }).ExecuteAsync(context);
}));

app.UseExceptionHandler(); 
app.UseStatusCodePages();

app.UseStaticFiles(); 
app.UseCors();

app.UseOutputCache();

app.UseAuthorization();

app.MapGet("/", () => "Hello World!");
app.MapGet("/error", () =>
{
    throw new InvalidOperationException("example error");
});

app.MapGroup("/genres").MapGenres();
app.MapGroup("/actors").MapActors();
app.MapGroup("/movies").MapMovies();
app.MapGroup("/movie/{movieId:int}/comments").MapComments();
app.MapGroup("/users").MapUsers();
//Middlewares zone -END
app.Run();
