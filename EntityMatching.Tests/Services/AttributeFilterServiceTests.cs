using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using EntityMatching.Shared.Models;
using EntityMatching.Shared.Models.Privacy;
using EntityMatching.Core.Models.Search;
using EntityMatching.Infrastructure.Services;
using Xunit;

namespace EntityMatching.Tests.Services
{
    /// <summary>
    /// Unit tests for AttributeFilterService
    /// Tests filter evaluation, privacy enforcement, and operator logic
    /// </summary>
    public class AttributeFilterServiceTests
    {
        private readonly AttributeFilterService _service;
        private readonly Mock<ILogger<AttributeFilterService>> _mockLogger;

        public AttributeFilterServiceTests()
        {
            _mockLogger = new Mock<ILogger<AttributeFilterService>>();
            _service = new AttributeFilterService(_mockLogger.Object);
        }

        #region EvaluateFilters Tests

        [Fact]
        public void EvaluateFilters_NoFilters_ReturnsTrue()
        {
            // Arrange
            var profile = CreateTestProfile();
            var emptyFilterGroup = new FilterGroup();

            // Act
            var result = _service.EvaluateFilters(profile, emptyFilterGroup, null, enforcePrivacy: false);

            // Assert
            result.Should().BeTrue("empty filter groups should match all profiles");
        }

        [Fact]
        public void EvaluateFilters_SimpleEquality_ReturnsTrue()
        {
            // Arrange
            var profile = CreateTestProfile();
            profile.Name = "John Doe";

            var filterGroup = new FilterGroup
            {
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter
                    {
                        FieldPath = "name",
                        Operator = FilterOperator.Equals,
                        Value = "John Doe"
                    }
                }
            };

            // Act
            var result = _service.EvaluateFilters(profile, filterGroup, null, enforcePrivacy: false);

            // Assert
            result.Should().BeTrue("profile name matches filter value");
        }

        [Fact]
        public void EvaluateFilters_SimpleEquality_ReturnsFalse()
        {
            // Arrange
            var profile = CreateTestProfile();
            profile.Name = "Jane Smith";

            var filterGroup = new FilterGroup
            {
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter
                    {
                        FieldPath = "name",
                        Operator = FilterOperator.Equals,
                        Value = "John Doe"
                    }
                }
            };

            // Act
            var result = _service.EvaluateFilters(profile, filterGroup, null, enforcePrivacy: false);

            // Assert
            result.Should().BeFalse("profile name does not match filter value");
        }

        [Fact]
        public void EvaluateFilters_BooleanIsTrue_ReturnsTrue()
        {
            // Arrange
            var profile = CreateTestProfile();
            profile.NaturePreferences.HasPets = true;

            var filterGroup = new FilterGroup
            {
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter
                    {
                        FieldPath = "naturePreferences.hasPets",
                        Operator = FilterOperator.IsTrue
                    }
                }
            };

            // Act
            var result = _service.EvaluateFilters(profile, filterGroup, null, enforcePrivacy: false);

            // Assert
            result.Should().BeTrue("profile has pets");
        }

        [Fact]
        public void EvaluateFilters_BooleanIsFalse_ReturnsTrue()
        {
            // Arrange
            var profile = CreateTestProfile();
            profile.NaturePreferences.HasPets = false;

            var filterGroup = new FilterGroup
            {
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter
                    {
                        FieldPath = "naturePreferences.hasPets",
                        Operator = FilterOperator.IsFalse
                    }
                }
            };

            // Act
            var result = _service.EvaluateFilters(profile, filterGroup, null, enforcePrivacy: false);

            // Assert
            result.Should().BeTrue("profile does not have pets");
        }

        [Fact]
        public void EvaluateFilters_ContainsInCollection_ReturnsTrue()
        {
            // Arrange
            var profile = CreateTestProfile();
            profile.NaturePreferences.PetTypes = new List<string> { "Dog", "Cat" };

            var filterGroup = new FilterGroup
            {
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter
                    {
                        FieldPath = "naturePreferences.petTypes",
                        Operator = FilterOperator.Contains,
                        Value = "Dog"
                    }
                }
            };

            // Act
            var result = _service.EvaluateFilters(profile, filterGroup, null, enforcePrivacy: false);

            // Assert
            result.Should().BeTrue("profile has a dog");
        }

        [Fact]
        public void EvaluateFilters_ContainsInString_ReturnsTrue()
        {
            // Arrange
            var profile = CreateTestProfile();
            profile.Description = "I love hiking and outdoor adventures";

            var filterGroup = new FilterGroup
            {
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter
                    {
                        FieldPath = "description",
                        Operator = FilterOperator.Contains,
                        Value = "hiking"
                    }
                }
            };

            // Act
            var result = _service.EvaluateFilters(profile, filterGroup, null, enforcePrivacy: false);

            // Assert
            result.Should().BeTrue("description contains 'hiking'");
        }

        [Fact]
        public void EvaluateFilters_AndLogic_AllMustMatch()
        {
            // Arrange
            var profile = CreateTestProfile();
            profile.Name = "John Doe";
            profile.NaturePreferences.HasPets = true;

            var filterGroup = new FilterGroup
            {
                LogicalOperator = LogicalOperator.And,
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter { FieldPath = "name", Operator = FilterOperator.Equals, Value = "John Doe" },
                    new AttributeFilter { FieldPath = "naturePreferences.hasPets", Operator = FilterOperator.IsTrue }
                }
            };

            // Act
            var result = _service.EvaluateFilters(profile, filterGroup, null, enforcePrivacy: false);

            // Assert
            result.Should().BeTrue("both filters match");
        }

        [Fact]
        public void EvaluateFilters_AndLogic_OneFails_ReturnsFalse()
        {
            // Arrange
            var profile = CreateTestProfile();
            profile.Name = "John Doe";
            profile.NaturePreferences.HasPets = false;

            var filterGroup = new FilterGroup
            {
                LogicalOperator = LogicalOperator.And,
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter { FieldPath = "name", Operator = FilterOperator.Equals, Value = "John Doe" },
                    new AttributeFilter { FieldPath = "naturePreferences.hasPets", Operator = FilterOperator.IsTrue }
                }
            };

            // Act
            var result = _service.EvaluateFilters(profile, filterGroup, null, enforcePrivacy: false);

            // Assert
            result.Should().BeFalse("second filter fails");
        }

        [Fact]
        public void EvaluateFilters_OrLogic_OneMatches_ReturnsTrue()
        {
            // Arrange
            var profile = CreateTestProfile();
            profile.Name = "John Doe";
            profile.NaturePreferences.HasPets = false;

            var filterGroup = new FilterGroup
            {
                LogicalOperator = LogicalOperator.Or,
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter { FieldPath = "name", Operator = FilterOperator.Equals, Value = "John Doe" },
                    new AttributeFilter { FieldPath = "naturePreferences.hasPets", Operator = FilterOperator.IsTrue }
                }
            };

            // Act
            var result = _service.EvaluateFilters(profile, filterGroup, null, enforcePrivacy: false);

            // Assert
            result.Should().BeTrue("first filter matches (OR logic)");
        }

        [Fact]
        public void EvaluateFilters_PrivateField_WithoutPermission_SkipsFilter()
        {
            // Arrange
            var profile = CreateTestProfile();
            profile.Birthday = new DateTime(1990, 1, 1);

            // Make birthday private
            profile.PrivacySettings.SetFieldVisibility("birthday", FieldVisibility.Private);

            var filterGroup = new FilterGroup
            {
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter
                    {
                        FieldPath = "birthday",
                        Operator = FilterOperator.Exists
                    }
                }
            };

            // Act - anonymous user (no permission)
            var result = _service.EvaluateFilters(profile, filterGroup, requestingUserId: null, enforcePrivacy: true);

            // Assert
            result.Should().BeFalse("private field skipped, no filters evaluated (fail-closed)");
        }

        [Fact]
        public void EvaluateFilters_PublicField_AllowsAnonymous()
        {
            // Arrange
            var profile = CreateTestProfile();
            profile.Name = "John Doe";

            // Make name public
            profile.PrivacySettings.SetFieldVisibility("name", FieldVisibility.Public);

            var filterGroup = new FilterGroup
            {
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter
                    {
                        FieldPath = "name",
                        Operator = FilterOperator.Equals,
                        Value = "John Doe"
                    }
                }
            };

            // Act - anonymous user
            var result = _service.EvaluateFilters(profile, filterGroup, requestingUserId: null, enforcePrivacy: true);

            // Assert
            result.Should().BeTrue("public field accessible to anonymous users");
        }

        [Fact]
        public void EvaluateFilters_NumericGreaterThan_ReturnsTrue()
        {
            // Arrange
            var profile = CreateTestProfile();
            profile.PersonalityClassifications.Extraversion = 8;

            var filterGroup = new FilterGroup
            {
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter
                    {
                        FieldPath = "personalityClassifications.extraversion",
                        Operator = FilterOperator.GreaterThan,
                        Value = 5
                    }
                }
            };

            // Act
            var result = _service.EvaluateFilters(profile, filterGroup, null, enforcePrivacy: false);

            // Assert
            result.Should().BeTrue("extraversion (8) > 5");
        }

        [Fact]
        public void EvaluateFilters_NumericInRange_ReturnsTrue()
        {
            // Arrange
            var profile = CreateTestProfile();
            profile.PersonalityClassifications.Extraversion = 7;

            var filterGroup = new FilterGroup
            {
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter
                    {
                        FieldPath = "personalityClassifications.extraversion",
                        Operator = FilterOperator.InRange,
                        MinValue = 5,
                        MaxValue = 10
                    }
                }
            };

            // Act
            var result = _service.EvaluateFilters(profile, filterGroup, null, enforcePrivacy: false);

            // Assert
            result.Should().BeTrue("extraversion (7) is between 5 and 10");
        }

        #endregion

        #region GetMatchedAttributes Tests

        [Fact]
        public void GetMatchedAttributes_ReturnsFieldValues()
        {
            // Arrange
            var profile = CreateTestProfile();
            profile.Name = "John Doe";
            profile.NaturePreferences.HasPets = true;
            profile.NaturePreferences.PetTypes = new List<string> { "Dog", "Cat" };

            var filterGroup = new FilterGroup
            {
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter { FieldPath = "name", Operator = FilterOperator.Equals, Value = "John Doe" },
                    new AttributeFilter { FieldPath = "naturePreferences.hasPets", Operator = FilterOperator.IsTrue },
                    new AttributeFilter { FieldPath = "naturePreferences.petTypes", Operator = FilterOperator.Contains, Value = "Dog" }
                }
            };

            // Act
            var matchedAttributes = _service.GetMatchedAttributes(profile, filterGroup, null, enforcePrivacy: false);

            // Assert
            matchedAttributes.Should().ContainKey("name");
            matchedAttributes["name"].Should().Be("John Doe");
            matchedAttributes.Should().ContainKey("naturePreferences.hasPets");
            matchedAttributes["naturePreferences.hasPets"].Should().Be(true);
            matchedAttributes.Should().ContainKey("naturePreferences.petTypes");
        }

        [Fact]
        public void GetMatchedAttributes_RespectsPrivacy()
        {
            // Arrange
            var profile = CreateTestProfile();
            profile.Name = "John Doe";
            profile.Birthday = new DateTime(1990, 1, 1);

            // Make name public, birthday private
            profile.PrivacySettings.SetFieldVisibility("name", FieldVisibility.Public);
            profile.PrivacySettings.SetFieldVisibility("birthday", FieldVisibility.Private);

            var filterGroup = new FilterGroup
            {
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter { FieldPath = "name", Operator = FilterOperator.Equals, Value = "John Doe" },
                    new AttributeFilter { FieldPath = "birthday", Operator = FilterOperator.Exists }
                }
            };

            // Act - anonymous user
            var matchedAttributes = _service.GetMatchedAttributes(profile, filterGroup, requestingUserId: null, enforcePrivacy: true);

            // Assert
            matchedAttributes.Should().ContainKey("name");
            matchedAttributes.Should().NotContainKey("birthday", "birthday is private");
        }

        #endregion

        #region BuildCosmosQuery Tests

        [Fact]
        public void BuildCosmosQuery_SimpleEquality_GeneratesCorrectQuery()
        {
            // Arrange
            var filterGroup = new FilterGroup
            {
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter
                    {
                        FieldPath = "name",
                        Operator = FilterOperator.Equals,
                        Value = "John Doe"
                    }
                }
            };

            // Act
            var query = _service.BuildCosmosQuery(filterGroup);

            // Assert
            query.Should().Contain("c.name = 'John Doe'");
        }

        [Fact]
        public void BuildCosmosQuery_AndLogic_UsesAndOperator()
        {
            // Arrange
            var filterGroup = new FilterGroup
            {
                LogicalOperator = LogicalOperator.And,
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter { FieldPath = "name", Operator = FilterOperator.Equals, Value = "John" },
                    new AttributeFilter { FieldPath = "naturePreferences.hasPets", Operator = FilterOperator.IsTrue }
                }
            };

            // Act
            var query = _service.BuildCosmosQuery(filterGroup);

            // Assert
            query.Should().Contain(" AND ");
            query.Should().Contain("c.name = 'John'");
            query.Should().Contain("c.naturePreferences.hasPets = true");
        }

        [Fact]
        public void BuildCosmosQuery_OrLogic_UsesOrOperator()
        {
            // Arrange
            var filterGroup = new FilterGroup
            {
                LogicalOperator = LogicalOperator.Or,
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter { FieldPath = "name", Operator = FilterOperator.Equals, Value = "John" },
                    new AttributeFilter { FieldPath = "name", Operator = FilterOperator.Equals, Value = "Jane" }
                }
            };

            // Act
            var query = _service.BuildCosmosQuery(filterGroup);

            // Assert
            query.Should().Contain(" OR ");
        }

        #endregion

        #region CanEvaluateInCosmosDb Tests

        [Fact]
        public void CanEvaluateInCosmosDb_SimpleFilters_ReturnsTrue()
        {
            // Arrange
            var filterGroup = new FilterGroup
            {
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter { FieldPath = "name", Operator = FilterOperator.Equals, Value = "John" },
                    new AttributeFilter { FieldPath = "naturePreferences.hasPets", Operator = FilterOperator.IsTrue }
                }
            };

            // Act
            var canEvaluate = _service.CanEvaluateInCosmosDb(filterGroup);

            // Assert
            canEvaluate.Should().BeTrue("simple equality and boolean filters are supported");
        }

        [Fact]
        public void CanEvaluateInCosmosDb_ContainsOperator_ReturnsFalse()
        {
            // Arrange
            var filterGroup = new FilterGroup
            {
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter { FieldPath = "bio", Operator = FilterOperator.Contains, Value = "hiking" }
                }
            };

            // Act
            var canEvaluate = _service.CanEvaluateInCosmosDb(filterGroup);

            // Assert
            canEvaluate.Should().BeFalse("Contains operator requires application-level evaluation");
        }

        [Fact]
        public void CanEvaluateInCosmosDb_ComputedProperty_ReturnsFalse()
        {
            // Arrange
            var filterGroup = new FilterGroup
            {
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter { FieldPath = "age", Operator = FilterOperator.GreaterThan, Value = 25 }
                }
            };

            // Act
            var canEvaluate = _service.CanEvaluateInCosmosDb(filterGroup);

            // Assert
            canEvaluate.Should().BeFalse("Age is a computed property");
        }

        #endregion

        #region Helper Methods

        private PersonEntity CreateTestProfile()
        {
            return new PersonEntity
            {
                Id = Guid.NewGuid(),
                Name = "Test User",
                Description = "Test bio",
                IsSearchable = true,
                PrivacySettings = new FieldVisibilitySettings
                {
                    DefaultVisibility = FieldVisibility.Public
                }
            };
        }

        #endregion
    }
}
