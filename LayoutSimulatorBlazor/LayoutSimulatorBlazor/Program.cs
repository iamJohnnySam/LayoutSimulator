using Communicator;
using LayoutCommands;
using LayoutModels;
using LayoutModels.CommSpecs;
using LayoutSimulatorBlazor;
using LayoutSimulatorBlazor.Client.Pages;
using LayoutSimulatorBlazor.Components;

// CREATE SIMULATOR
Simulator simulator = new("simulation1.xml");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// CONNECT SIMULATOR
builder.Services.AddSingleton<LayoutSimulatorService>(new LayoutSimulatorService(simulator));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(LayoutSimulatorBlazor.Client._Imports).Assembly);

app.Run();
