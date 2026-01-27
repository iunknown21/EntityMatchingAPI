using Newtonsoft.Json;
using System;

namespace EntityMatching.Shared.Models
{
    /// <summary>
    /// Represents a profile image with metadata
    /// </summary>
    public class ProfileImage
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty(PropertyName = "imageUrl")]
        public string ImageUrl { get; set; } = "";

        [JsonProperty(PropertyName = "thumbnailUrl")]
        public string ThumbnailUrl { get; set; } = "";

        [JsonProperty(PropertyName = "isDefault")]
        public bool IsDefault { get; set; } = false;

        [JsonProperty(PropertyName = "uploadedAt")]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty(PropertyName = "fileName")]
        public string FileName { get; set; } = "";

        [JsonProperty(PropertyName = "altText")]
        public string AltText { get; set; } = "";

        /// <summary>
        /// Gets a display-friendly alt text for the image
        /// </summary>
        [JsonIgnore]
        public string DisplayAltText => !string.IsNullOrWhiteSpace(AltText) ? AltText : "Profile photo";
    }

    /// <summary>
    /// Request model for uploading profile images
    /// </summary>
    public class UploadProfileImageRequest
    {
        public string ProfileId { get; set; } = "";
        public string FileName { get; set; } = "";
        public string ContentType { get; set; } = "";
        public bool SetAsDefault { get; set; } = false;
        public string AltText { get; set; } = "";
    }

    /// <summary>
    /// Response model for profile image upload
    /// </summary>
    public class UploadProfileImageResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public ProfileImage? Image { get; set; }
        public string? ErrorCode { get; set; }
    }

    /// <summary>
    /// Privacy levels for profile image sharing
    /// </summary>
    public enum ProfileImagePrivacyLevel
    {
        /// <summary>
        /// No images shared
        /// </summary>
        None = 0,

        /// <summary>
        /// Only default profile image shared
        /// </summary>
        DefaultOnly = 1,

        /// <summary>
        /// All profile images shared
        /// </summary>
        All = 2
    }
}
