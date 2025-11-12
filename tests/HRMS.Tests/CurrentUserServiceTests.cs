using HRMS.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using Xunit;
using FluentAssertions;

namespace HRMS.Tests;

/// <summary>
/// Production-ready unit tests for CurrentUserService
/// Tests authentication context extraction from HTTP claims
/// Critical for audit trails and SOX/GDPR compliance
/// </summary>
public class CurrentUserServiceTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly CurrentUserService _service;

    public CurrentUserServiceTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _service = new CurrentUserService(_mockHttpContextAccessor.Object);
    }

    // ==================== USER ID EXTRACTION TESTS ====================

    [Fact]
    public void UserId_WithNameIdentifierClaim_ShouldReturnUserId()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, expectedUserId)
        };
        SetupHttpContext(claims, isAuthenticated: true);

        // Act
        var result = _service.UserId;

        // Assert
        result.Should().Be(expectedUserId);
    }

    [Fact]
    public void UserId_WithSubClaim_ShouldReturnUserId()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new Claim("sub", expectedUserId)
        };
        SetupHttpContext(claims, isAuthenticated: true);

        // Act
        var result = _service.UserId;

        // Assert
        result.Should().Be(expectedUserId);
    }

    [Fact]
    public void UserId_WithUserIdClaim_ShouldReturnUserId()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new Claim("user_id", expectedUserId)
        };
        SetupHttpContext(claims, isAuthenticated: true);

        // Act
        var result = _service.UserId;

        // Assert
        result.Should().Be(expectedUserId);
    }

    [Fact]
    public void UserId_NotAuthenticated_ShouldReturnNull()
    {
        // Arrange
        SetupHttpContext(new List<Claim>(), isAuthenticated: false);

        // Act
        var result = _service.UserId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void UserId_NoHttpContext_ShouldReturnNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null);

        // Act
        var result = _service.UserId;

        // Assert
        result.Should().BeNull();
    }

    // ==================== USERNAME EXTRACTION TESTS ====================

    [Fact]
    public void Username_WithPreferredUsernameClaim_ShouldReturnUsername()
    {
        // Arrange
        var expectedUsername = "john.doe";
        var claims = new List<Claim>
        {
            new Claim("preferred_username", expectedUsername)
        };
        SetupHttpContext(claims, isAuthenticated: true);

        // Act
        var result = _service.Username;

        // Assert
        result.Should().Be(expectedUsername);
    }

    [Fact]
    public void Username_WithNameClaim_ShouldReturnUsername()
    {
        // Arrange
        var expectedUsername = "john.doe";
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, expectedUsername)
        };
        SetupHttpContext(claims, isAuthenticated: true);

        // Act
        var result = _service.Username;

        // Assert
        result.Should().Be(expectedUsername);
    }

    [Fact]
    public void Username_WithUsernameClaim_ShouldReturnUsername()
    {
        // Arrange
        var expectedUsername = "john.doe";
        var claims = new List<Claim>
        {
            new Claim("username", expectedUsername)
        };
        SetupHttpContext(claims, isAuthenticated: true);

        // Act
        var result = _service.Username;

        // Assert
        result.Should().Be(expectedUsername);
    }

    [Fact]
    public void Username_WithEmailOnly_ShouldReturnEmail()
    {
        // Arrange
        var expectedEmail = "john.doe@company.com";
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, expectedEmail)
        };
        SetupHttpContext(claims, isAuthenticated: true);

        // Act
        var result = _service.Username;

        // Assert
        result.Should().Be(expectedEmail);
    }

    [Fact]
    public void Username_NotAuthenticated_ShouldReturnNull()
    {
        // Arrange
        SetupHttpContext(new List<Claim>(), isAuthenticated: false);

        // Act
        var result = _service.Username;

        // Assert
        result.Should().BeNull();
    }

    // ==================== EMAIL EXTRACTION TESTS ====================

    [Fact]
    public void Email_WithEmailClaim_ShouldReturnEmail()
    {
        // Arrange
        var expectedEmail = "john.doe@company.com";
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, expectedEmail)
        };
        SetupHttpContext(claims, isAuthenticated: true);

        // Act
        var result = _service.Email;

        // Assert
        result.Should().Be(expectedEmail);
    }

    [Fact]
    public void Email_WithEmailClaimLowercase_ShouldReturnEmail()
    {
        // Arrange
        var expectedEmail = "john.doe@company.com";
        var claims = new List<Claim>
        {
            new Claim("email", expectedEmail)
        };
        SetupHttpContext(claims, isAuthenticated: true);

        // Act
        var result = _service.Email;

        // Assert
        result.Should().Be(expectedEmail);
    }

    [Fact]
    public void Email_NotAuthenticated_ShouldReturnNull()
    {
        // Arrange
        SetupHttpContext(new List<Claim>(), isAuthenticated: false);

        // Act
        var result = _service.Email;

        // Assert
        result.Should().BeNull();
    }

    // ==================== TENANT ID EXTRACTION TESTS ====================

    [Fact]
    public void TenantId_WithTenantIdClaim_ShouldReturnGuid()
    {
        // Arrange
        var expectedTenantId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim("tenant_id", expectedTenantId.ToString())
        };
        SetupHttpContext(claims, isAuthenticated: true);

        // Act
        var result = _service.TenantId;

        // Assert
        result.Should().Be(expectedTenantId);
    }

    [Fact]
    public void TenantId_WithTenantIdClaimPascalCase_ShouldReturnGuid()
    {
        // Arrange
        var expectedTenantId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim("TenantId", expectedTenantId.ToString())
        };
        SetupHttpContext(claims, isAuthenticated: true);

        // Act
        var result = _service.TenantId;

        // Assert
        result.Should().Be(expectedTenantId);
    }

    [Fact]
    public void TenantId_InvalidGuid_ShouldReturnNull()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("tenant_id", "not-a-guid")
        };
        SetupHttpContext(claims, isAuthenticated: true);

        // Act
        var result = _service.TenantId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TenantId_NotAuthenticated_ShouldReturnNull()
    {
        // Arrange
        SetupHttpContext(new List<Claim>(), isAuthenticated: false);

        // Act
        var result = _service.TenantId;

        // Assert
        result.Should().BeNull();
    }

    // ==================== IS AUTHENTICATED TESTS ====================

    [Fact]
    public void IsAuthenticated_WithAuthenticatedUser_ShouldReturnTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        SetupHttpContext(claims, isAuthenticated: true);

        // Act
        var result = _service.IsAuthenticated;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_WithUnauthenticatedUser_ShouldReturnFalse()
    {
        // Arrange
        SetupHttpContext(new List<Claim>(), isAuthenticated: false);

        // Act
        var result = _service.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_NoHttpContext_ShouldReturnFalse()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null);

        // Act
        var result = _service.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    // ==================== GET AUDIT USERNAME TESTS ====================

    [Fact]
    public void GetAuditUsername_WithAuthenticatedUser_ShouldReturnUsername()
    {
        // Arrange
        var expectedUsername = "john.doe";
        var claims = new List<Claim>
        {
            new Claim("preferred_username", expectedUsername)
        };
        SetupHttpContext(claims, isAuthenticated: true);

        // Act
        var result = _service.GetAuditUsername();

        // Assert
        result.Should().Be(expectedUsername);
    }

    [Fact]
    public void GetAuditUsername_WithAuthenticatedUserEmailOnly_ShouldReturnEmail()
    {
        // Arrange
        var expectedEmail = "john.doe@company.com";
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, expectedEmail)
        };
        SetupHttpContext(claims, isAuthenticated: true);

        // Act
        var result = _service.GetAuditUsername();

        // Assert
        result.Should().Be(expectedEmail);
    }

    [Fact]
    public void GetAuditUsername_NotAuthenticated_ShouldReturnSystem()
    {
        // Arrange
        SetupHttpContext(new List<Claim>(), isAuthenticated: false);

        // Act
        var result = _service.GetAuditUsername();

        // Assert
        result.Should().Be("System");
    }

    [Fact]
    public void GetAuditUsername_NoHttpContext_ShouldReturnSystem()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null);

        // Act
        var result = _service.GetAuditUsername();

        // Assert
        result.Should().Be("System");
    }

    [Fact]
    public void GetAuditUsername_AuthenticatedButNoClaims_ShouldReturnSystem()
    {
        // Arrange
        SetupHttpContext(new List<Claim>(), isAuthenticated: true);

        // Act
        var result = _service.GetAuditUsername();

        // Assert
        result.Should().Be("System");
    }

    // ==================== MULTI-CLAIM PRIORITY TESTS ====================

    [Fact]
    public void Username_MultipleClaimsPresent_ShouldPrioritizePreferredUsername()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("preferred_username", "preferred"),
            new Claim(ClaimTypes.Name, "name"),
            new Claim("username", "username"),
            new Claim(ClaimTypes.Email, "email@test.com")
        };
        SetupHttpContext(claims, isAuthenticated: true);

        // Act
        var result = _service.Username;

        // Assert
        result.Should().Be("preferred");
    }

    [Fact]
    public void UserId_MultipleClaimsPresent_ShouldPrioritizeNameIdentifier()
    {
        // Arrange
        var nameIdentifierId = Guid.NewGuid().ToString();
        var subId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, nameIdentifierId),
            new Claim("sub", subId),
            new Claim("user_id", userId)
        };
        SetupHttpContext(claims, isAuthenticated: true);

        // Act
        var result = _service.UserId;

        // Assert
        result.Should().Be(nameIdentifierId);
    }

    // ==================== EDGE CASES ====================

    [Fact]
    public void Email_EmptyClaim_ShouldReturnEmptyString()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, "")
        };
        SetupHttpContext(claims, isAuthenticated: true);

        // Act
        var result = _service.Email;

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Username_EmptyClaims_ShouldReturnNull()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("some_other_claim", "value")
        };
        SetupHttpContext(claims, isAuthenticated: true);

        // Act
        var result = _service.Username;

        // Assert
        result.Should().BeNull();
    }

    // ==================== HELPER METHODS ====================

    private void SetupHttpContext(List<Claim> claims, bool isAuthenticated)
    {
        var identity = new ClaimsIdentity(claims, isAuthenticated ? "TestAuth" : null);
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
    }
}
