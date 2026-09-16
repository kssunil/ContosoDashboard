using Bunit;
using Microsoft.AspNetCore.Components;

namespace ContosoDashboard.Tests.Shared;

public sealed class MainLayoutTests
{
    private static readonly string[] StandardLinkLabels =
    [
        "Dashboard",
        "My Tasks",
        "My Projects",
        "Documents",
        "Team",
        "Notifications",
        "Profile"
    ];

    [Fact]
    public void StartsExpandedWithAccessibleCollapseControl()
    {
        using var context = new MainLayoutTestContext();

        var layout = context.RenderLayout();

        var control = layout.Find("button.action-panel-toggle");
        Assert.Equal("Collapse action panel", control.GetAttribute("aria-label"));
        Assert.Equal("true", control.GetAttribute("aria-expanded"));
        Assert.Equal("action-panel-navigation", control.GetAttribute("aria-controls"));
        Assert.Contains("action-panel-expanded", layout.Find(".page").ClassList);
        Assert.Contains("Dashboard", layout.Find("#action-panel-navigation").TextContent);
    }

    [Fact]
    public void CollapseHidesNavigationAndRetainsOnlyTheControlInThePanel()
    {
        using var context = new MainLayoutTestContext();
        var layout = context.RenderLayout();

        layout.Find("button.action-panel-toggle").Click();

        var control = layout.Find("button.action-panel-toggle");
        Assert.Equal("Expand action panel", control.GetAttribute("aria-label"));
        Assert.Equal("false", control.GetAttribute("aria-expanded"));
        Assert.Contains("action-panel-collapsed", layout.Find(".page").ClassList);
        Assert.Empty(layout.FindAll("#action-panel-navigation"));
        Assert.Empty(layout.FindAll(".navbar-brand"));
        Assert.Single(layout.FindAll(".sidebar button"));
    }

    [Fact]
    public void CollapsePreservesHeadersAndRoutedBodyState()
    {
        using var context = new MainLayoutTestContext();
        RenderFragment body = builder =>
        {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "data-testid", "draft-value");
            builder.AddAttribute(2, "value", "Unsaved profile value");
            builder.CloseElement();
        };
        var layout = context.RenderLayout(body);
        var headerCount = layout.FindAll(".top-row").Count;

        layout.Find("button.action-panel-toggle").Click();

        Assert.Equal(headerCount, layout.FindAll(".top-row").Count);
        Assert.Equal("Unsaved profile value", layout.Find("[data-testid='draft-value']").GetAttribute("value"));
    }

    [Fact]
    public void ExpansionRestoresLinksInTheirOriginalOrderAndAccessibleState()
    {
        using var context = new MainLayoutTestContext();
        var layout = context.RenderLayout();
        layout.Find("button.action-panel-toggle").Click();

        layout.Find("button.action-panel-toggle").Click();

        var control = layout.Find("button.action-panel-toggle");
        Assert.Equal("Collapse action panel", control.GetAttribute("aria-label"));
        Assert.Equal("true", control.GetAttribute("aria-expanded"));
        Assert.Contains("action-panel-expanded", layout.Find(".page").ClassList);
        Assert.Equal(StandardLinkLabels, layout.FindAll("#action-panel-navigation .nav-link")
            .Select(link => link.TextContent.Trim()));
    }

    [Fact]
    public void ExpansionPreservesTheActiveDestination()
    {
        using var context = new MainLayoutTestContext();
        context.Navigation.NavigateTo("tasks");
        var layout = context.RenderLayout();
        layout.Find("button.action-panel-toggle").Click();

        layout.Find("button.action-panel-toggle").Click();

        Assert.Contains("active", layout.Find("#action-panel-navigation a[href='tasks']").ClassList);
    }

    [Fact]
    public void DocumentReportsRemainsAdministratorOnlyAfterExpansion()
    {
        using var administratorContext = new MainLayoutTestContext();
        administratorContext.SetAuthorizedUser("Administrator");
        var administratorLayout = administratorContext.RenderLayout();
        administratorLayout.Find("button.action-panel-toggle").Click();
        administratorLayout.Find("button.action-panel-toggle").Click();

        Assert.Single(administratorLayout.FindAll("a[href='documents/reports']"));

        using var employeeContext = new MainLayoutTestContext();
        var employeeLayout = employeeContext.RenderLayout();
        employeeLayout.Find("button.action-panel-toggle").Click();
        employeeLayout.Find("button.action-panel-toggle").Click();

        Assert.Empty(employeeLayout.FindAll("a[href='documents/reports']"));
    }

    [Fact]
    public void DifferentRoutePathResetsThePanelToExpanded()
    {
        using var context = new MainLayoutTestContext();
        var layout = context.RenderLayout();
        layout.Find("button.action-panel-toggle").Click();

        context.Navigation.NavigateTo("tasks");

        layout.WaitForAssertion(() => Assert.Equal(
            "true",
            layout.Find("button.action-panel-toggle").GetAttribute("aria-expanded")));
    }

    [Theory]
    [InlineData("documents?category=Reports")]
    [InlineData("documents#recent")]
    [InlineData("documents/")]
    public void EquivalentRoutePathRetainsTheCollapsedState(string destination)
    {
        using var context = new MainLayoutTestContext();
        context.Navigation.NavigateTo("documents");
        var layout = context.RenderLayout();
        layout.Find("button.action-panel-toggle").Click();

        context.Navigation.NavigateTo(destination);

        layout.WaitForAssertion(() => Assert.Equal(
            "false",
            layout.Find("button.action-panel-toggle").GetAttribute("aria-expanded")));
    }

    [Fact]
    public void DirectEntryStartsExpandedRegardlessOfQueryOrFragment()
    {
        using var context = new MainLayoutTestContext();
        context.Navigation.NavigateTo("profile?tab=settings#details");

        var layout = context.RenderLayout();

        Assert.Equal("true", layout.Find("button.action-panel-toggle").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void DisposedLayoutDoesNotHandleLaterNavigation()
    {
        using var context = new MainLayoutTestContext();
        var layout = context.RenderLayout();
        layout.Dispose();

        var exception = Record.Exception(() => context.Navigation.NavigateTo("tasks"));

        Assert.Null(exception);
    }
}