using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using ContosoDashboard.Models;
using ContosoDashboard.Pages;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace ContosoDashboard.Tests.Pages;

public sealed class ProjectCreateTests : TestContext
{
    private static readonly List<User> AllUsers =
    [
        new() { UserId = 1, DisplayName = "System Administrator", Email = "admin@contoso.com", Role = UserRole.Administrator },
        new() { UserId = 2, DisplayName = "Camille Nicole", Email = "camille.nicole@contoso.com", Role = UserRole.ProjectManager },
        new() { UserId = 3, DisplayName = "Floris Kregel", Email = "floris.kregel@contoso.com", Role = UserRole.TeamLead },
        new() { UserId = 4, DisplayName = "Ni Kang", Email = "ni.kang@contoso.com", Role = UserRole.Employee }
    ];

    public ProjectCreateTests()
    {
        Services.AddAuthorizationCore(options =>
        {
            options.AddPolicy("ProjectManager", policy => policy.RequireRole("ProjectManager", "Administrator"));
        });
    }

    private FakeProjectService SetUpAuthorizedProjectManager(FakeProjectService.CreateResult? createResult = null)
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("Camille Nicole");
        authContext.SetRoles("ProjectManager");
        authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, "2"));

        var fakeProjectService = new FakeProjectService(createResult);
        Services.AddSingleton<IProjectService>(fakeProjectService);
        Services.AddSingleton<IUserService>(new FakeUserService(AllUsers));
        return fakeProjectService;
    }

    [Fact]
    public void RendersForAuthorizedProjectManager()
    {
        SetUpAuthorizedProjectManager();

        var component = RenderComponent<ProjectCreate>();

        Assert.NotEmpty(component.FindAll("#project-title"));
    }

    [Fact]
    public void DeclaresProjectManagerPolicyGateForNonProjectManagerRoles()
    {
        // bUnit's RenderComponent bypasses the app Router's AuthorizeRouteView, so the
        // [Authorize] gate that actually blocks Employee/TeamLead access (verified end-to-end
        // by ProjectCreationAuthorizationTests at the service layer) cannot be exercised by
        // rendering this component in isolation. This instead asserts the routing-level gate
        // is declared with the correct policy, which is what AuthorizeRouteView enforces at runtime.
        var authorizeAttribute = typeof(ProjectCreate).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(authorizeAttribute);
        Assert.Equal("ProjectManager", authorizeAttribute!.Policy);
    }

    [Fact]
    public void SuccessfulSaveNavigatesToProjects()
    {
        SetUpAuthorizedProjectManager(new FakeProjectService.CreateResult(true, "Ignored error"));
        var component = RenderComponent<ProjectCreate>();
        component.Find("#project-title").Change("A Brand New Project");
        component.Find("#project-description").Change("Some description");
        component.Find("#project-manager").Change("2");

        component.Find("#save-project-button").Click();

        var navigation = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>() as FakeNavigationManager;
        Assert.EndsWith("/projects", navigation!.Uri);
    }

    [Fact]
    public void FailedSaveShowsMessageAndPreservesEnteredTitle()
    {
        SetUpAuthorizedProjectManager(new FakeProjectService.CreateResult(false, "A project with this title already exists."));
        var component = RenderComponent<ProjectCreate>();
        component.Find("#project-title").Change("ContosoDashboard Development");
        component.Find("#project-description").Change("Some description");
        component.Find("#project-manager").Change("2");

        component.Find("#save-project-button").Click();

        Assert.Contains("A project with this title already exists.", component.Markup);
        Assert.Equal("ContosoDashboard Development", component.Find("#project-title").GetAttribute("value"));
    }

    [Fact]
    public void NewTaskModalRejectsBlankTitleAndAddsNoRow()
    {
        SetUpAuthorizedProjectManager();
        var component = RenderComponent<ProjectCreate>();

        component.Find("#new-task-button").Click(); // Open "New Task" modal
        component.Find("#task-modal-ok-button").Click(); // Ok with blank title

        Assert.Contains("Title is required.", component.Markup);
        Assert.Contains("modal fade show", component.Markup); // modal stays open
        Assert.Contains("No tasks added yet.", component.Markup); // no row was added
    }

    [Fact]
    public void TeamMemberSelectionUpdatesCommaSeparatedSummary()
    {
        SetUpAuthorizedProjectManager();
        var component = RenderComponent<ProjectCreate>();

        var select = component.Find("select[multiple]");
        select.Change(new[] { "3", "4" });

        Assert.Contains("Floris Kregel, Ni Kang", component.Markup);
    }

    private sealed class FakeUserService : IUserService
    {
        private readonly List<User> _users;
        public FakeUserService(List<User> users) => _users = users;
        public Task<User?> GetUserByIdAsync(int userId) => Task.FromResult(_users.FirstOrDefault(u => u.UserId == userId));
        public Task<User?> GetUserByEmailAsync(string email) => Task.FromResult(_users.FirstOrDefault(u => u.Email == email));
        public Task<User> CreateOrUpdateUserAsync(string email, string displayName) => throw new NotImplementedException();
        public Task<bool> UpdateUserProfileAsync(User user, int requestingUserId) => throw new NotImplementedException();
        public Task<bool> UpdateAvailabilityStatusAsync(int userId, AvailabilityStatus status) => throw new NotImplementedException();
        public Task<List<User>> GetTeamMembersAsync(int userId) => Task.FromResult(_users);
        public Task<List<User>> GetAllUsersAsync() => Task.FromResult(_users);
    }

    private sealed class FakeProjectService : IProjectService
    {
        public sealed record CreateResult(bool Succeeded, string? ErrorMessage);

        private readonly CreateResult? _result;
        public FakeProjectService(CreateResult? result) => _result = result;

        public Task<List<Project>> GetUserProjectsAsync(int userId) => Task.FromResult(new List<Project>());
        public Task<Project?> GetProjectByIdAsync(int projectId, int requestingUserId) => throw new NotImplementedException();
        public Task<CreateProjectResult> CreateProjectAsync(CreateProjectRequest request, int actorUserId, CancellationToken cancellationToken = default)
        {
            var outcome = _result ?? new CreateResult(true, null);
            return Task.FromResult(outcome.Succeeded
                ? new CreateProjectResult(true, new Project { ProjectId = 99, Name = request.Title }, null)
                : CreateProjectResult.Failure(outcome.ErrorMessage ?? "Failed"));
        }
        public Task<bool> UpdateProjectAsync(Project project, int requestingUserId) => throw new NotImplementedException();
        public Task<bool> AddProjectMemberAsync(int projectId, int userId, string role, int requestingUserId) => throw new NotImplementedException();
        public Task<List<ProjectMember>> GetProjectMembersAsync(int projectId, int requestingUserId) => throw new NotImplementedException();
    }
}
