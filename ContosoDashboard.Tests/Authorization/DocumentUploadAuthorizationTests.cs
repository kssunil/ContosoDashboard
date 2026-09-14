using ContosoDashboard.Models;

namespace ContosoDashboard.Tests.Authorization;

public class DocumentUploadAuthorizationTests
{
    [Fact]
    public void ProjectMemberIsIncludedInProjectVisibility()
    {
        var project = DocumentAuthorizationFixture.Project(memberIds: [4]);
        Assert.Contains(project.ProjectMembers, member => member.UserId == 4);
    }

    [Fact]
    public void ProjectManagerIsProjectOwner()
    {
        var project = DocumentAuthorizationFixture.Project();
        Assert.Equal(UserRole.ProjectManager, DocumentAuthorizationFixture.ProjectManager().Role);
        Assert.Equal(2, project.ProjectManagerId);
    }
}
