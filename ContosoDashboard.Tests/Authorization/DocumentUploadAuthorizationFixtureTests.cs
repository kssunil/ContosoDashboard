namespace ContosoDashboard.Tests.Authorization;

public class DocumentUploadAuthorizationFixtureTests
{
    [Fact]
    public void FixtureProvidesSeededRoleIds()
    {
        Assert.Equal(1, DocumentAuthorizationFixture.Administrator().UserId);
        Assert.Equal(2, DocumentAuthorizationFixture.ProjectManager().UserId);
        Assert.Equal(3, DocumentAuthorizationFixture.TeamLead().UserId);
        Assert.Equal(4, DocumentAuthorizationFixture.Employee().UserId);
    }
}
