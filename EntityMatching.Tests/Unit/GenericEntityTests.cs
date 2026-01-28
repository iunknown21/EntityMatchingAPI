using EntityMatching.Shared.Models;
using FluentAssertions;
using Xunit;
using System;

namespace EntityMatching.Tests.Unit
{
    /// <summary>
    /// Unit tests for the generic Entity base class
    /// These tests are entity-type agnostic and test only base Entity functionality
    /// Entity-specific tests (PersonEntity, JobEntity, etc.) belong in their respective projects
    /// </summary>
    public class GenericEntityTests
    {
        [Fact]
        public void Entity_DefaultConstructor_InitializesBaseProperties()
        {
            // Arrange & Act
            var entity = new Entity();

            // Assert
            entity.Id.Should().NotBe(Guid.Empty);
            entity.EntityType.Should().Be(EntityType.Person); // Default
            entity.Name.Should().NotBeNull();
            entity.Description.Should().NotBeNull();
            entity.Attributes.Should().NotBeNull();
        }

        [Fact]
        public void Entity_SetAttribute_StoresAndRetrievesValue()
        {
            // Arrange
            var entity = new Entity();
            var testKey = "testKey";
            var testValue = "testValue";

            // Act
            entity.SetAttribute(testKey, testValue);
            var retrieved = entity.GetAttribute<string>(testKey);

            // Assert
            retrieved.Should().Be(testValue);
        }

        [Fact]
        public void Entity_SetAttribute_WithNumericValue_StoresAndRetrievesCorrectly()
        {
            // Arrange
            var entity = new Entity();
            var testKey = "count";
            var testValue = 42;

            // Act
            entity.SetAttribute(testKey, testValue);
            var retrieved = entity.GetAttributeValue<int>(testKey);

            // Assert
            retrieved.Should().Be(testValue);
        }

        [Fact]
        public void Entity_GetAttribute_NonExistentKey_ReturnsNull()
        {
            // Arrange
            var entity = new Entity();

            // Act
            var result = entity.GetAttribute<string>("nonExistentKey");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Entity_Metadata_CanBeSetAndRetrieved()
        {
            // Arrange
            var entity = new Entity();
            entity.Metadata = new System.Collections.Generic.Dictionary<string, object>();
            var metadataKey = "source";
            var metadataValue = "test-system";

            // Act
            entity.Metadata[metadataKey] = metadataValue;
            var retrieved = entity.Metadata[metadataKey];

            // Assert
            retrieved.Should().Be(metadataValue);
        }

        [Fact]
        public void Entity_PrivacySettings_InitializesWithDefaultValues()
        {
            // Arrange & Act
            var entity = new Entity();

            // Assert
            entity.PrivacySettings.Should().NotBeNull();
            entity.IsSearchable.Should().BeTrue(); // Default
        }

        [Theory]
        [InlineData(EntityType.Person)]
        [InlineData(EntityType.Job)]
        [InlineData(EntityType.Property)]
        [InlineData(EntityType.Career)]
        [InlineData(EntityType.Major)]
        public void Entity_CanSetAnyEntityType(EntityType entityType)
        {
            // Arrange & Act
            var entity = new Entity { EntityType = entityType };

            // Assert
            entity.EntityType.Should().Be(entityType);
        }

        [Fact]
        public void Entity_Timestamps_SetCorrectly()
        {
            // Arrange
            var beforeCreate = DateTime.UtcNow;

            // Act
            var entity = new Entity();
            entity.CreatedAt = DateTime.UtcNow;
            entity.LastModified = DateTime.UtcNow;

            var afterCreate = DateTime.UtcNow;

            // Assert
            entity.CreatedAt.Should().BeOnOrAfter(beforeCreate);
            entity.CreatedAt.Should().BeOnOrBefore(afterCreate);
            entity.LastModified.Should().BeOnOrAfter(beforeCreate);
            entity.LastModified.Should().BeOnOrBefore(afterCreate);
        }

        [Fact]
        public void Entity_ExternalReferences_CanBeSet()
        {
            // Arrange & Act
            var entity = new Entity
            {
                ExternalId = "EXT-12345",
                ExternalSource = "TestSystem"
            };

            // Assert
            entity.ExternalId.Should().Be("EXT-12345");
            entity.ExternalSource.Should().Be("TestSystem");
        }

        [Fact]
        public void Entity_OwnedByUserId_CanBeSet()
        {
            // Arrange
            var userId = "user-123";

            // Act
            var entity = new Entity { OwnedByUserId = userId };

            // Assert
            entity.OwnedByUserId.Should().Be(userId);
        }
    }
}
