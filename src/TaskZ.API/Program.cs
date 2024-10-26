using Microsoft.EntityFrameworkCore;
using TaskZ.Infrastructure.Data;
using TaskZ.Infrastructure.Repositories;
using TaskZ.Core.Interfaces;

var builder = WebApplication.CreateBuilder(args);
/*
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(80); 
});
*/
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();