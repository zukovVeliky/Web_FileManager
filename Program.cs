using WebFileManager.Services;
using WebFileManager.Services.ComponentSettings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddScoped<CustomFileManagerService>();
builder.Services.AddSingleton<IComponentSettingsProvider, JsonComponentSettingsProvider>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase),
    branch => branch.UseStatusCodePagesWithReExecute("/Error", "?statusCode={0}"));

app.MapControllers();
app.MapRazorPages();

app.Run();
