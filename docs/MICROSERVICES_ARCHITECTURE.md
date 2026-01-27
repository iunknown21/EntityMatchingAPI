# Microservices Architecture

The platform has been refactored into a microservices architecture with specialized services for different enrichment capabilities.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│           EntityMatchingAPI (Core Platform)            │
│                                                         │
│  - Universal Entity Storage                             │
│  - Vector Embeddings & Similarity Search                │
│  - Profile Management                                   │
│  - Matching & Recommendations                           │
│  - Conversation & Reputation                            │
└─────────────────────────────────────────────────────────┘
                         ▲  ▲  ▲
                         │  │  │
         ┌───────────────┘  │  └──────────────┐
         │                  │                  │
         │                  │                  │
┌────────┴────────┐  ┌──────┴──────┐  ┌───────┴────────┐
│ PropertyService │  │CareerService│  │  Future        │
│                 │  │             │  │  Services      │
│ - Property      │  │ - Career    │  │                │
│   Enrichment    │  │   Enrichment│  │                │
│ - Schools       │  │ - Major     │  │                │
│ - Demographics  │  │   Enrichment│  │                │
│ - Crime Stats   │  │ - O*NET Data│  │                │
│ - Walkability   │  │ - BLS Data  │  │                │
└─────────────────┘  └─────────────┘  └────────────────┘
```

## Services

### EntityMatchingAPI (Core Platform)

**Purpose:** Universal entity matching platform with vector similarity search

**Base URL:** `https://entityaiapi.azurewebsites.net/api/v1`

**Repository:** Original monorepo

**Features:**
- Entity management (Person, Job, Event, Product, Service, Property, Major, Career)
- Vector embeddings with OpenAI
- Semantic similarity search
- Profile-based search strategies
- Mutual matching
- Match requests
- Reputation & ratings
- Conversational profiling
- Admin tools

**Entity Types:**
- `0` - Person
- `1` - Job
- `2` - Event
- `3` - Product
- `4` - Service
- `5` - Property
- `6` - Major
- `7` - Career

**Documentation:** [CORE_PLATFORM_API.md](./CORE_PLATFORM_API.md)

---

### PropertyService

**Purpose:** Real estate property enrichment with location intelligence

**Base URL:** `https://propertyservice-func.azurewebsites.net/api/v1`

**Repository:** https://github.com/iunknown21/PropertyService

**Features:**
- Address geocoding (Google Maps)
- School data with ratings (GreatSchools)
- Demographics (US Census)
- Crime statistics
- Walkability scores (WalkScore)
- Nearby places and amenities
- Grid-based caching (80% cost reduction)
- Batch processing
- Natural language summaries (Groq AI)

**Key Endpoints:**
- `POST /properties/enrich` - Enrich single property
- `POST /properties/enrich/batch` - Batch enrichment
- `GET /properties/{id}` - Retrieve enriched property
- `GET /properties/cache/stats` - Cache statistics
- `DELETE /properties/cache` - Clear cache

**Integration:**
PropertyService stores enriched properties in Core Platform as Entity (EntityType=5) for semantic search capabilities.

**Documentation:** [PropertyService API Docs](https://github.com/iunknown21/PropertyService/blob/master/docs/PROPERTY_SERVICE_API.md)

---

### CareerService

**Purpose:** Career and academic major enrichment with O*NET and BLS data

**Base URL:** `https://careerservice-func.azurewebsites.net/api/v1`

**Repository:** https://github.com/iunknown21/CareerService

**Features:**
- Career enrichment using O*NET-SOC codes
- Major enrichment using CIP codes
- RIASEC interest scores (Holland Codes)
- Tasks, skills, knowledge, abilities
- Salary data (BLS)
- Job growth projections
- Education requirements
- Major-to-career pathways

**Key Endpoints:**
- `POST /careers/enrich?onetCode={code}` - Enrich career
- `POST /majors/enrich?cipCode={code}` - Enrich major
- `GET /careers/{onetCode}` - Retrieve career
- `GET /majors/{cipCode}` - Retrieve major

**Integration:**
CareerService stores enriched careers (EntityType=7) and majors (EntityType=6) in Core Platform for semantic search and matching.

**Documentation:** [CareerService API Docs](https://github.com/iunknown21/CareerService/blob/master/docs/CAREER_SERVICE_API.md)

---

## Integration Patterns

### Pattern 1: Enrich and Store

Specialized services enrich domain-specific data and store it in the Core Platform:

```typescript
// PropertyService enriches property
const enriched = await propertyService.enrich("123 Main St, Dallas, TX");

// Store in Core Platform with EntityType=5 (Property)
await corePlatform.createEntity({
  entityType: 5,
  name: enriched.address,
  description: enriched.naturalLanguageSummary,
  attributes: {
    enrichedPropertyId: enriched.id,
    walkabilityScore: enriched.metrics.walkabilityScore,
    latitude: enriched.location.latitude,
    longitude: enriched.location.longitude
  }
});

// Generate embedding from description
await corePlatform.uploadEmbedding(entityId, embedding);

// Now searchable via semantic search
const results = await corePlatform.search({
  query: "walkable property near good schools",
  entityTypes: [5]
});
```

### Pattern 2: Cross-Service Workflows

Services can call each other through the Core Platform:

```typescript
// Career exploration workflow
// 1. Student enriches their major in CareerService
const major = await careerService.enrichMajor("11.0701"); // Computer Science

// 2. Major includes related career O*NET codes
const relatedCareers = major.relatedCareers; // ["15-1252.00", "15-1299.08", ...]

// 3. Enrich each career
for (const onetCode of relatedCareers) {
  const career = await careerService.enrichCareer(onetCode);

  // 4. Store career in Core Platform
  await corePlatform.createEntity({
    entityType: 7,
    name: career.title,
    description: career.description,
    attributes: {
      onetCode: career.onetCode,
      medianSalary: career.medianSalary,
      interests: career.interests // RIASEC scores
    }
  });
}

// 5. Student searches careers by interests
const matchingCareers = await corePlatform.search({
  query: "investigative and realistic careers",
  entityTypes: [7],
  attributeFilters: {
    "interests.Investigative": { min: 6.0 }
  }
});
```

### Pattern 3: Unified Search

All entities (people, jobs, properties, careers, majors) are searchable together:

```typescript
// Find everything relevant to "software development in Dallas"
const results = await corePlatform.search({
  query: "software development in Dallas",
  entityTypes: [0, 1, 5, 7], // Person, Job, Property, Career
  limit: 50
});

// Results include:
// - People with software development skills (EntityType=0)
// - Software development job postings (EntityType=1)
// - Properties in Dallas (EntityType=5)
// - Software development careers (EntityType=7)
```

---

## Deployment

Each service has independent deployment via GitHub Actions:

### EntityMatchingAPI
- **Workflow:** `.github/workflows/azure-functions-deploy.yml`
- **Trigger:** Push to master
- **Steps:** Build → Test → Deploy
- **Target:** `entityaiapi` Azure Function

### PropertyService
- **Workflow:** `.github/workflows/azure-functions-deploy.yml`
- **Trigger:** Push to master
- **Steps:** Build → Test → Deploy
- **Target:** `propertyservice-func` Azure Function
- **Tests:** 12 unit tests

### CareerService
- **Workflow:** `.github/workflows/azure-functions-deploy.yml`
- **Trigger:** Push to master
- **Steps:** Build → Test → Deploy
- **Target:** `careerservice-func` Azure Function
- **Tests:** 16 unit tests

---

## Authentication

### Core Platform (EntityMatchingAPI)
```
Header: Ocp-Apim-Subscription-Key: YOUR_API_KEY
```

### PropertyService & CareerService
```
Header: x-functions-key: YOUR_FUNCTION_KEY
```

---

## Health Checks

Monitor all services:

```bash
# Core Platform
curl https://entityaiapi.azurewebsites.net/api/v1/version

# PropertyService
curl https://propertyservice-func.azurewebsites.net/api/v1/properties/health

# CareerService
curl https://careerservice-func.azurewebsites.net/api/v1/health
```

---

## Cost Optimization

### PropertyService
- **Grid-based caching**: 0.001 degree grids (~100m)
- **Cache TTL**: 30 days with refresh on read
- **Savings**: Up to 80% in batch operations
- **Example**: 100 properties in same neighborhood: $0.90 vs $4.50

### CareerService
- **Data caching**: O*NET and BLS data cached in Cosmos DB
- **Update frequency**: Refresh enriched data annually
- **Cost**: Free O*NET access, optional BLS API (currently using mock data)

---

## Testing

### EntityMatchingAPI
- **Total Tests:** 149
- **Passing:** 136 unit/service tests
- **Skipped:** 13 integration tests (require running Functions app)

### PropertyService
- **Total Tests:** 12
- **Passing:** 12 unit tests
- **Coverage:** Core models (EnrichedProperty, PropertyLocation)

### CareerService
- **Total Tests:** 16
- **Passing:** 16 unit tests
- **Coverage:** Core models (Career, Major)

---

## Data Flow Example

Complete workflow: Student finds career and searches for jobs in specific location

```bash
# 1. Student profile in Core Platform
POST https://entityaiapi.azurewebsites.net/api/v1/entities
{
  "entityType": 0,
  "name": "Jane Smith",
  "attributes": {
    "major": "Computer Science",
    "graduationYear": 2027,
    "interests": {
      "Investigative": 7.0,
      "Realistic": 5.5
    }
  }
}

# 2. Enrich major to find related careers
POST https://careerservice-func.azurewebsites.net/api/v1/majors/enrich?cipCode=11.0701
# Returns: relatedCareers = ["15-1252.00", ...]

# 3. Enrich careers
POST https://careerservice-func.azurewebsites.net/api/v1/careers/enrich?onetCode=15-1252.00
# Returns: Software Developer career with RIASEC scores

# 4. Store careers in Core Platform
POST https://entityaiapi.azurewebsites.net/api/v1/entities
{
  "entityType": 7,
  "name": "Software Developers",
  "attributes": { "onetCode": "15-1252.00", "medianSalary": 120730 }
}

# 5. Search for jobs in Dallas
POST https://entityaiapi.azurewebsites.net/api/v1/search
{
  "query": "Software Developer",
  "entityTypes": [1],
  "attributeFilters": { "location": { "contains": "Dallas" } }
}

# 6. Search for properties in Dallas near work
POST https://propertyservice-func.azurewebsites.net/api/v1/properties/enrich
{ "address": "Dallas, TX 75201" }

# 7. Store property in Core Platform
POST https://entityaiapi.azurewebsites.net/api/v1/entities
{
  "entityType": 5,
  "name": "Downtown Dallas Properties",
  "attributes": { "enrichedPropertyId": "prop_abc123" }
}

# 8. Find everything relevant to student
POST https://entityaiapi.azurewebsites.net/api/v1/search
{
  "query": "Software Developer Dallas good schools walkable",
  "entityTypes": [1, 5, 7],
  "limit": 20
}
```

---

## Future Services

Potential future microservices:

- **EventService**: Enrich events with venue, ticketing, performer data
- **ProductService**: Enrich products with reviews, pricing, availability
- **CompanyService**: Enrich companies with financials, culture, reviews
- **SkillService**: Skills taxonomy and skill gap analysis
- **EducationService**: Course catalog and learning pathway recommendations

---

## Links

### Core Platform
- [API Documentation](./API_DOCUMENTATION.md)
- [Entity Matching Guide](./UNIVERSAL_ENTITY_MATCHING.md)
- [Embedding Architecture](./EMBEDDING_ARCHITECTURE.md)
- [Developer Guide](./DEVELOPER_ONBOARDING.md)

### PropertyService
- [GitHub Repository](https://github.com/iunknown21/PropertyService)
- [API Documentation](https://github.com/iunknown21/PropertyService/blob/master/docs/PROPERTY_SERVICE_API.md)
- [Production Endpoint](https://propertyservice-func.azurewebsites.net)

### CareerService
- [GitHub Repository](https://github.com/iunknown21/CareerService)
- [API Documentation](https://github.com/iunknown21/CareerService/blob/master/docs/CAREER_SERVICE_API.md)
- [Production Endpoint](https://careerservice-func.azurewebsites.net)
