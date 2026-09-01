using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MuscleRivalsBackend.Data;
using MuscleRivalsBackend.Mappers;
using MuscleRivalsBackend.Models.DTOs.Auth;
using MuscleRivalsBackend.Services;
using MuscleRivalsBackend.Validators;
using MuscleRivalsBackend.Hubs;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// Adding services to the container.

builder.Services.AddScoped<TokenService>();

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
// Validators
builder.Services.AddScoped<IValidator<LoginRequestDTO>, LoginRequestDTOValidator>();
builder.Services.AddScoped<IValidator<RegisterRequestDTO>, RegisterRequestDTOValidator>();
builder.Services.AddScoped<IValidator<RegisterWithGoogleRequestDTO>, RegisterWithGoogleRequestDTOValidator>();

builder.Services.AddScoped<UserMapper>();

builder.Services.AddScoped<AuthService>();

builder.Services.AddDbContext<MuscleRivalsDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(jwtOptions =>
{
    var jwtConfig = builder.Configuration.GetSection("Jwt");
    SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(jwtConfig["SecretKey"]!));


    jwtOptions.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtConfig["Issuer"],
        ValidAudience = jwtConfig["Audience"],
        IssuerSigningKey = key,
        ClockSkew = TimeSpan.Zero
    };

    jwtOptions.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            // If the request is for our hub...
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs/matchmaking"))
            {
                // Read the token out of the query string
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };

});


builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        string clientUsername = httpContext.User.FindFirstValue(ClaimTypes.Name) ?? httpContext.Request.Headers.Host.ToString();

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey: clientUsername, factory: _ =>
        {
            return new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100, // Max 100 requests
                Window = TimeSpan.FromMinutes(1), // Per 1 minute window
                QueueLimit = 0, // No queuing, immediate rejection if limit exceeded
            };
        });

    });

    options.AddPolicy("auth-policy", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey: clientIp, factory: _ =>
        {
            return new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10, // Max 10 requests
                Window = TimeSpan.FromMinutes(1), // Per 1 minute window
                QueueLimit = 0, // No queuing, immediate rejection if limit exceeded
            };
        });
    });
    options.AddPolicy("report-policy", httpContext =>
{
    var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    return RateLimitPartition.GetFixedWindowLimiter(partitionKey: clientIp, factory: _ =>
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5, // Max 5 requests
            Window = TimeSpan.FromMinutes(1), // Per 1 minute window
            QueueLimit = 0, // No queuing, immediate rejection if limit exceeded
        };
    });
});

    options.AddPolicy("email-policy", httpContext =>
{
    var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    return RateLimitPartition.GetFixedWindowLimiter(partitionKey: clientIp, factory: _ =>
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5, // Max 5 requests
            Window = TimeSpan.FromHours(1), // Per 1 hour window
            QueueLimit = 0, // No queuing, immediate rejection if limit exceeded
        };
    });
});

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();
        }

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", cancellationToken);

        Console.WriteLine($"Rate limit exceeded for IP: {context.HttpContext.Connection.RemoteIpAddress}");
    };
});

var app = builder.Build();

app.UseCors("cors-origin-policy");



// Add middleware to serve static files
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();

// Map razor and controller endpoints
app.MapControllers();


// Using authentication and authorization middleware to secure endpoints
app.UseAuthentication();
app.UseAuthorization();


app.UseMiddleware<ExceptionHandlerMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapHub<MatchmakingHub>("/hubs/matchmaking");
app.Run();

