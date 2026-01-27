using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace WebServer.Pipeline;

public class ExcludeControllerConvention : IControllerModelConvention
{
    private readonly HashSet<string> mControllersToExclude;
    private readonly IWebHostEnvironment mEnvironment;

    public ExcludeControllerConvention(IWebHostEnvironment environment, params string[] controllerNames)
    {
        mControllersToExclude = controllerNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        mEnvironment = environment;
    }

    public void Apply(ControllerModel controller)
    {
        if (!mControllersToExclude.Contains(controller.ControllerName))
            return;

        if (mEnvironment.IsDevelopment())
            return;

        controller.ApiExplorer.IsVisible = false;
        controller.Actions.Clear();
        controller.Selectors.Clear();
    }
}