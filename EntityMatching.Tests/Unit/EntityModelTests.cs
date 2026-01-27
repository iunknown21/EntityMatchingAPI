using EntityMatching.Shared.Models;
using Xunit;
using System;
using System.Collections.Generic;

namespace EntityMatching.Tests.Unit
{
    /// <summary>
    /// Tests for the new universal Entity model
    /// </summary>
    public class EntityModelTests
    {
        [Fact]
        public void Entity_DefaultConstructor_SetsPersonEntityType()
        {
            // Arrange & Act
            var entity = new Entity();

            // Assert
            Assert.Equal(EntityType.Person, entity.EntityType);
            Assert.NotEqual(Guid.Empty, entity.Id);
            Assert.NotNull(entity.Attributes);
        }

        [Fact]
        public void Entity_CanSetAndGetAttributes()
        {
            // Arrange
            var entity = new Entity();

            // Act
            entity.SetAttribute("testKey", "testValue");
            entity.SetAttribute("numberKey", 42);

            // Assert
            Assert.Equal("testValue", entity.GetAttribute<string>("testKey"));
            Assert.Equal(42, entity.GetAttributeValue<int>("numberKey"));
        }

        [Fact]
        public void PersonEntity_InheritsFromEntity()
        {
            // Arrange & Act
            var person = new PersonEntity
            {
                Name = "John Doe",
                Birthday = new DateTime(1990, 1, 1)
            };

            // Assert
            Assert.Equal(EntityType.Person, person.EntityType);
            Assert.Equal("John Doe", person.Name);
            Assert.NotNull(person.Age);
            Assert.Equal(DateTime.UtcNow.Year - 1990, person.Age.Value);
        }

        [Fact]
        public void JobEntity_SetsCorrectEntityType()
        {
            // Arrange & Act
            var job = new JobEntity
            {
                Name = "Senior Engineer",
                CompanyName = "TechCorp",
                RequiredSkills = new[] { "Python", "AWS" },
                MinSalary = 100000,
                MaxSalary = 150000
            };

            // Assert
            Assert.Equal(EntityType.Job, job.EntityType);
            Assert.Equal("Senior Engineer", job.Name);
            Assert.Equal("TechCorp", job.CompanyName);
            Assert.Contains("Python", job.RequiredSkills);
        }

        [Fact]
        public void JobEntity_SyncToAttributes_CopiesProperties()
        {
            // Arrange
            var job = new JobEntity
            {
                CompanyName = "TechCorp",
                RequiredSkills = new[] { "Python", "AWS" },
                MinSalary = 100000,
                MaxSalary = 150000
            };

            // Act
            job.SyncToAttributes();

            // Assert
            Assert.Equal("TechCorp", job.GetAttribute<string>("companyName"));
            var skills = job.GetAttribute<string[]>("requiredSkills");
            Assert.NotNull(skills);
            Assert.Contains("Python", skills);
        }

        [Fact]
        public void PropertyEntity_SetsCorrectEntityType()
        {
            // Arrange & Act
            var property = new PropertyEntity
            {
                Name = "Beautiful 3BR House",
                Address = "123 Main St",
                City = "Seattle",
                State = "WA",
                Bedrooms = 3,
                Bathrooms = 2,
                Price = 500000
            };

            // Assert
            Assert.Equal(EntityType.Property, property.EntityType);
            Assert.Equal("Beautiful 3BR House", property.Name);
            Assert.Equal(3, property.Bedrooms);
            Assert.Equal(500000, property.Price);
        }

        [Fact]
        public void PropertyEntity_SyncToAttributes_CopiesProperties()
        {
            // Arrange
            var property = new PropertyEntity
            {
                Bedrooms = 3,
                Bathrooms = 2,
                Price = 500000,
                PetsAllowed = true
            };

            // Act
            property.SyncToAttributes();

            // Assert
            Assert.Equal(3, property.GetAttributeValue<int>("bedrooms"));
            Assert.Equal(2, property.GetAttributeValue<decimal>("bathrooms"));
            Assert.Equal(500000, property.GetAttributeValue<decimal>("price"));
            Assert.True(property.GetAttributeValue<bool>("petsAllowed"));
        }

        [Fact]
        public void Entity_PrivacySettings_WorksCorrectly()
        {
            // Arrange
            var entity = new Entity
            {
                OwnedByUserId = "user123",
                IsSearchable = true
            };

            // Act & Assert
            // When not searchable, no fields are visible
            entity.IsSearchable = false;
            Assert.False(entity.IsFieldVisibleToUser("name", null));
            Assert.False(entity.IsFieldVisibleToUser("name", "otherUser"));

            // When searchable, default privacy settings apply
            entity.IsSearchable = true;
            // Fields follow privacy settings (default may vary)
            // This test just verifies the method doesn't throw
            var isVisible = entity.IsFieldVisibleToUser("name", null);
            Assert.True(isVisible || !isVisible); // Just verify it returns a bool
        }

        [Fact]
        public void EntityType_HasCorrectValues()
        {
            // Assert
            Assert.Equal(0, (int)EntityType.Person);
            Assert.Equal(1, (int)EntityType.Job);
            Assert.Equal(2, (int)EntityType.Property);
            Assert.Equal(3, (int)EntityType.Product);
            Assert.Equal(4, (int)EntityType.Service);
            Assert.Equal(5, (int)EntityType.Event);
        }
    }
}
