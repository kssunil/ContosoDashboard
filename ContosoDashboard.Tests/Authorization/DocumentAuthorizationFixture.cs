using ContosoDashboard.Models;

namespace ContosoDashboard.Tests.Authorization;

public static class DocumentAuthorizationFixture
{
    public static User Administrator() => new() { UserId = 1, DisplayName = "Administrator", Role = UserRole.Administrator };
    public static User ProjectManager() => new() { UserId = 2, DisplayName = "Project Manager", Role = UserRole.ProjectManager };
    public static User TeamLead() => new() { UserId = 3, DisplayName = "Team Lead", Role = UserRole.TeamLead };
    public static User Employee() => new() { UserId = 4, DisplayName = "Employee", Role = UserRole.Employee };

    public static Project Project(int managerId = 2, params int[] memberIds)
    {
        var project = new Project { ProjectId = 1, Name = "Training Project", ProjectManagerId = managerId };
        project.ProjectMembers = memberIds.Select((id, index) => new ProjectMember
        {
            ProjectMemberId = index + 1,
            ProjectId = project.ProjectId,
            UserId = id,
            Role = "TeamMember"
        }).ToList();
        return project;
    }
}
