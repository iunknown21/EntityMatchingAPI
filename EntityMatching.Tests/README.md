# EntityMatching.Tests

**Generic entity matching system tests - Entity-type agnostic**

## 🎯 Purpose

This test project contains **only generic entity tests** that work across all entity types (Person, Job, Property, Career, Major, etc.).

**Entity-specific tests belong in their respective projects:**
- `PersonEntity` tests → `ProfileMatchingAPI/ProfileMatching.Tests/`
- `JobEntity` tests → `JobMatchingAPI/JobMatching.Tests/` (if exists)
- `PropertyEntity` tests → `PropertyMatchingAPI/PropertyMatching.Tests/` (if exists)

## 🏗️ Architecture Philosophy

### This Project Tests:
✅ **Generic Entity base class** - Properties and methods on `Entity`
✅ **Universal services** - Services that work with any entity type
✅ **Entity-agnostic algorithms** - Matching, filtering, search without entity-specific logic
✅ **Infrastructure** - CosmosDB, caching, serialization (entity-independent)

### This Project Does NOT Test:
❌ **PersonEntity-specific logic** - Personality, preferences, love languages (see ProfileMatchingAPI)
❌ **JobEntity-specific logic** - Skills, requirements, compensation (separate project)
❌ **PropertyEntity-specific logic** - Location, amenities, pricing (separate project)
❌ **Entity-specific workflows** - Person matching, job matching (domain-specific projects)

## 📁 Test Structure

```
EntityMatching.Tests/
├── Unit/
│   └── GenericEntityTests.cs          # Base Entity class tests
├── Integration/                        # Reserved for future generic integration tests
├── EntityMatching.Tests.csproj
├── testsettings.json                   # Azure configuration
├── API_TESTING_GUIDE.md               # Generic API testing guide
└── README.md                           # This file
```

## 🧪 Running Tests

### All Tests
```bash
dotnet test
```

### Unit Tests Only
```bash
dotnet test --filter "FullyQualifiedName~Unit"
```

### Integration Tests Only
```bash
dotnet test --filter "FullyQualifiedName~Integration"
```

## ✅ What's Currently Tested

### Generic Entity Tests
- ✅ Entity initialization and default values
- ✅ Attribute storage and retrieval
- ✅ Metadata storage and retrieval
- ✅ Privacy settings
- ✅ Timestamps
- ✅ External references
- ✅ Entity type assignment

## 🚧 Future Generic Tests (To Add)

When implementing, ensure tests remain entity-agnostic:

### Generic Service Tests
- [ ] `EntityService` CRUD operations (any entity type)
- [ ] Generic similarity search algorithms
- [ ] Generic attribute filtering
- [ ] Privacy enforcement (cross-entity-type)

### Generic Integration Tests
- [ ] Cosmos DB serialization/deserialization (any entity)
- [ ] Generic embedding storage
- [ ] Cross-entity-type search

## 🔍 Example: Generic vs Entity-Specific Test

### ✅ GOOD (Generic - Belongs Here)
```csharp
[Fact]
public void Entity_SetAttribute_StoresValue()
{
    var entity = new Entity();
    entity.SetAttribute("key", "value");
    entity.GetAttribute<string>("key").Should().Be("value");
}
```

### ❌ BAD (PersonEntity-Specific - Belongs in ProfileMatchingAPI)
```csharp
[Fact]
public void PersonEntity_WithGiftPreferences_GeneratesSummary()
{
    var person = new PersonEntity
    {
        GiftPreferences = new GiftPreferences { ... }
    };
    // This tests PersonEntity-specific logic!
}
```

## 🔗 Related Projects

- **ProfileMatchingAPI** - Person matching with full PersonEntity test coverage
- **EntityMatchingAPI** - This project - Generic infrastructure

## 📝 Adding New Tests

### Guidelines:
1. **Ask yourself:** Does this test work with ANY entity type?
   - YES → Add it here
   - NO → Add it to the entity-specific project

2. **Use base `Entity` class only** - Don't reference `PersonEntity`, `JobEntity`, etc.

3. **Test contracts, not implementations** - Test that services accept and return `Entity`, not derived types

4. **Mock entity-specific behavior** - Use generic test data, not real-world person/job data

### Example Generic Test:
```csharp
[Fact]
public async Task EntityService_GetEntity_ReturnsEntity()
{
    // Arrange
    var entity = new Entity { Name = "Test", EntityType = EntityType.Person };
    await service.AddEntityAsync(entity);

    // Act
    var retrieved = await service.GetEntityAsync(entity.Id.ToString());

    // Assert
    retrieved.Should().NotBeNull();
    retrieved.Name.Should().Be("Test");
}
```

## 🎓 Why This Architecture?

### Benefits:
1. **Clear separation of concerns** - Generic infrastructure vs domain logic
2. **Independent development** - Person, Job, Property teams work independently
3. **Reduced coupling** - Core engine doesn't depend on specific entity types
4. **Easier testing** - Generic tests are simpler and faster
5. **Scalability** - New entity types don't bloat the core test suite

### Trade-offs:
- More projects to maintain
- Need to decide "generic vs specific" boundary
- Some duplication of test helpers across projects

## 🛠️ Configuration

Tests use `testsettings.json` for Azure configuration:
- Cosmos DB connection string
- OpenAI API key (for embedding tests)
- Test database name

See `TESTING_GUIDE.md` for full configuration instructions.

## 📊 Test Coverage

Current coverage:
- **Unit Tests**: Generic Entity model (100%)
- **Integration Tests**: None yet (to be added)

Target coverage:
- Core entity operations: 80%+
- Generic services: 70%+
- Infrastructure: 60%+

---

**Remember:** If your test mentions PersonEntity, StylePreferences, JobEntity, or any entity-specific type, it belongs in a domain-specific test project, not here!
