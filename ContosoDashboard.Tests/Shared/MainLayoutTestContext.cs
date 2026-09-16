using Bunit;
using Bunit.TestDoubles;
using ContosoDashboard.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ContosoDashboard.Tests.Shared;

public sealed class MainLayoutTestContext : TestContext
{
    private readonly TestAuthorizationContext authorization;

    public MainLayoutTestContext()
    {
        authorization = this.AddTestAuthorization();
        SetAuthorizedUser();
    }

    public FakeNavigationManager Navigation => Services.GetRequiredService<NavigationManager>() as FakeNavigationManager
        ?? throw new InvalidOperationException("The bUnit navigation manager is unavailable.");

    public void SetAuthorizedUser(params string[] roles)
    {
        authorization.SetAuthorized("Test User");
        authorization.SetRoles(roles);
    }

    public IRenderedComponent<MainLayout> RenderLayout(RenderFragment? body = null)
    {
        body ??= builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "data-testid", "layout-body");
            builder.AddContent(2, "Test body");
            builder.CloseElement();
        };

        return RenderComponent<MainLayout>(parameters => parameters.Add(layout => layout.Body, body));
    }
}