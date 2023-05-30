using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class Comment
{
    public string? Text { get; set; }
}

public class Startup
{
    private static readonly List<User> users = new List<User>
    {
        new User { Id = 1, Name = "Alice", Email = "alice@example.com", Password = "password1" },
        new User { Id = 2, Name = "Bob", Email = "bob@example.com", Password = "password2" },
        new User { Id = 3, Name = "Charlie", Email = "charlie@example.com", Password = "password3" }
    };

    private static readonly List<Comment> comments = new List<Comment>
    {
        new Comment { Text = "This is a comment" }
    };

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddSession();
        services.AddDistributedMemoryCache();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        app.UseSession();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync(@"
                    <h1>Welcome to the Store</h1>
                    <form action=""/login"" method=""post"">
                        <label for=""email"">Email:</label>
                        <input type=""email"" name=""email"" id=""email"" required>
                        <label for=""password"">Password:</label>
                        <input type=""password"" name=""password"" id=""password"" required>
                        <button type=""submit"">Login</button>
                    </form>
                ");
            });

            endpoints.MapPost("/login", context =>
            {
                var email = context.Request.Form["email"];
                var password = context.Request.Form["password"];
                var user = users.Find(u => u.Email == email && u.Password == password);

                if (user != null)
                {
                    context.Session.SetInt32("UserId", user.Id);
                    context.Response.Redirect($"/user/{user.Id}");
                    return System.Threading.Tasks.Task.CompletedTask;
                }
                else
                {
                    return context.Response.WriteAsync("Invalid credentials. Please try again.");
                }
            });

            endpoints.MapGet("/user/{id:int}", context =>
            {
                var id = int.Parse(context.Request.RouteValues["id"].ToString());
                var user = users.Find(u => u.Id == id);

                if (user != null)
                {
                    var commentsHtml = string.Join("", comments.ConvertAll(c => $"<li>{c.Text}</li>"));

                    return context.Response.WriteAsync($@"
                        <h1>User Profile</h1>
                        <p>Name: {user.Name}</p>
                        <p>Email: {user.Email}</p>
                        <h2>Write a comment:</h2>
                        <form action=""/comments"" method=""post"">
                            <input type=""text"" name=""comment"" id=""comment"">
                            <button type=""submit"">Send</button>
                        </form>
                        <h2>Comments:</h2>
                        <ul>
                            {commentsHtml}
                        </ul>
                        <script>
                            var form = document.querySelector('form');
                            form.addEventListener('submit', function(event) {{
                                event.preventDefault();
                                var comment = document.getElementById('comment').value;
                                var xhr = new XMLHttpRequest();
                                xhr.open('POST', '/comments');
                                xhr.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
                                xhr.send('comment=' + encodeURIComponent(comment));

                                // After submit, reloads page to show the new comment
                                window.location.reload();
                            }});
                        </script>
                    ");
                }
                else
                {
                    context.Response.StatusCode = 404;
                    return context.Response.WriteAsync("User not found");
                }
            });

            endpoints.MapPost("/comments", context =>
            {
                if (context.Session.TryGetValue("UserId", out var userIdBytes))
                {
                    var userId = BitConverter.ToInt32(userIdBytes);
                    var commentText = context.Request.Form["comment"];
                    comments.Add(new Comment { Text = commentText });

                    // Redirects to the user profile page
                    context.Response.Redirect($"/user/{userId}");
                    return System.Threading.Tasks.Task.CompletedTask;

                    // return context.Response.Redirect($"/user/{userId}");
                }
                else
                {
                    context.Response.Redirect("/");
                    return System.Threading.Tasks.Task.CompletedTask;
                }
            });
        });
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
