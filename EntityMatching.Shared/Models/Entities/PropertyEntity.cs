using Newtonsoft.Json;
using System;

namespace EntityMatching.Shared.Models
{
    /// <summary>
    /// Strongly-typed entity for real estate properties
    /// Enables bidirectional matching: properties can search for buyers/renters, buyers/renters can search for properties
    /// </summary>
    public class PropertyEntity : Entity
    {
        public PropertyEntity()
        {
            EntityType = EntityType.Property;
        }

        // === Property-Specific Properties ===

        /// <summary>
        /// Full street address
        /// </summary>
        [JsonProperty(PropertyName = "address")]
        public string Address { get; set; } = "";

        /// <summary>
        /// City
        /// </summary>
        [JsonProperty(PropertyName = "city")]
        public string City { get; set; } = "";

        /// <summary>
        /// State or province
        /// </summary>
        [JsonProperty(PropertyName = "state")]
        public string State { get; set; } = "";

        /// <summary>
        /// Postal/ZIP code
        /// </summary>
        [JsonProperty(PropertyName = "postalCode")]
        public string PostalCode { get; set; } = "";

        /// <summary>
        /// Type of property (House, Condo, Apartment, Townhouse, etc.)
        /// </summary>
        [JsonProperty(PropertyName = "propertyType")]
        public string PropertyType { get; set; } = "";

        /// <summary>
        /// Number of bedrooms
        /// </summary>
        [JsonProperty(PropertyName = "bedrooms")]
        public int Bedrooms { get; set; } = 0;

        /// <summary>
        /// Number of bathrooms
        /// </summary>
        [JsonProperty(PropertyName = "bathrooms")]
        public decimal Bathrooms { get; set; } = 0;

        /// <summary>
        /// Square footage
        /// </summary>
        [JsonProperty(PropertyName = "squareFeet")]
        public int SquareFeet { get; set; } = 0;

        /// <summary>
        /// Lot size in square feet (for houses)
        /// </summary>
        [JsonProperty(PropertyName = "lotSize")]
        public int? LotSize { get; set; }

        /// <summary>
        /// Year the property was built
        /// </summary>
        [JsonProperty(PropertyName = "yearBuilt")]
        public int? YearBuilt { get; set; }

        /// <summary>
        /// Price (sale or monthly rent)
        /// </summary>
        [JsonProperty(PropertyName = "price")]
        public decimal Price { get; set; } = 0;

        /// <summary>
        /// Whether this is a rental or sale
        /// </summary>
        [JsonProperty(PropertyName = "listingType")]
        public string ListingType { get; set; } = "Sale"; // Sale, Rent

        /// <summary>
        /// Whether pets are allowed
        /// </summary>
        [JsonProperty(PropertyName = "petsAllowed")]
        public bool PetsAllowed { get; set; } = false;

        /// <summary>
        /// Pet deposit amount (if pets allowed)
        /// </summary>
        [JsonProperty(PropertyName = "petDeposit")]
        public decimal? PetDeposit { get; set; }

        /// <summary>
        /// Number of parking spaces
        /// </summary>
        [JsonProperty(PropertyName = "parkingSpaces")]
        public int ParkingSpaces { get; set; } = 0;

        /// <summary>
        /// Property amenities (Pool, Gym, Security, etc.)
        /// </summary>
        [JsonProperty(PropertyName = "amenities")]
        public string[] Amenities { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Nearby features (Schools, Parks, Transit, etc.)
        /// </summary>
        [JsonProperty(PropertyName = "nearbyFeatures")]
        public string[] NearbyFeatures { get; set; } = Array.Empty<string>();

        /// <summary>
        /// School district name
        /// </summary>
        [JsonProperty(PropertyName = "schoolDistrict")]
        public string SchoolDistrict { get; set; } = "";

        /// <summary>
        /// School rating (0-10)
        /// </summary>
        [JsonProperty(PropertyName = "schoolRating")]
        public decimal? SchoolRating { get; set; }

        /// <summary>
        /// Whether the property is furnished
        /// </summary>
        [JsonProperty(PropertyName = "furnished")]
        public bool Furnished { get; set; } = false;

        /// <summary>
        /// HOA fees (monthly)
        /// </summary>
        [JsonProperty(PropertyName = "hoaFees")]
        public decimal? HoaFees { get; set; }

        /// <summary>
        /// Property tax (annual)
        /// </summary>
        [JsonProperty(PropertyName = "propertyTax")]
        public decimal? PropertyTax { get; set; }

        /// <summary>
        /// Available date for move-in
        /// </summary>
        [JsonProperty(PropertyName = "availableDate")]
        public DateTime? AvailableDate { get; set; }

        /// <summary>
        /// Sync strongly-typed properties to the base Attributes dictionary
        /// Call this before saving to ensure search filters can access property-specific fields
        /// </summary>
        public void SyncToAttributes()
        {
            SetAttribute("address", Address);
            SetAttribute("city", City);
            SetAttribute("state", State);
            SetAttribute("postalCode", PostalCode);
            SetAttribute("propertyType", PropertyType);
            SetAttribute("bedrooms", Bedrooms);
            SetAttribute("bathrooms", Bathrooms);
            SetAttribute("squareFeet", SquareFeet);

            if (LotSize.HasValue)
                SetAttribute("lotSize", LotSize.Value);

            if (YearBuilt.HasValue)
                SetAttribute("yearBuilt", YearBuilt.Value);

            SetAttribute("price", Price);
            SetAttribute("listingType", ListingType);
            SetAttribute("petsAllowed", PetsAllowed);

            if (PetDeposit.HasValue)
                SetAttribute("petDeposit", PetDeposit.Value);

            SetAttribute("parkingSpaces", ParkingSpaces);
            SetAttribute("amenities", Amenities);
            SetAttribute("nearbyFeatures", NearbyFeatures);
            SetAttribute("schoolDistrict", SchoolDistrict);

            if (SchoolRating.HasValue)
                SetAttribute("schoolRating", SchoolRating.Value);

            SetAttribute("furnished", Furnished);

            if (HoaFees.HasValue)
                SetAttribute("hoaFees", HoaFees.Value);

            if (PropertyTax.HasValue)
                SetAttribute("propertyTax", PropertyTax.Value);

            if (AvailableDate.HasValue)
                SetAttribute("availableDate", AvailableDate.Value);
        }
    }
}
