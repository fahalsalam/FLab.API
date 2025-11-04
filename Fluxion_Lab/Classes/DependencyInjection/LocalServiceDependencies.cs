using Fluxion_Lab.Services.Printing;
using Fluxion_Lab.Models.General;

public static class LocalServiceDependencies
{
    public static void AddLocalServiceDependencies(this IServiceCollection services)
    {
        services.AddSingleton<RawPrinterService>();
        services.AddTransient<APIResponse>();
    }
}