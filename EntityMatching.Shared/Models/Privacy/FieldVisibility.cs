namespace EntityMatching.Shared.Models.Privacy
{
    /// <summary>
    /// Visibility level for individual profile fields
    /// Controls who can search/view specific profile data
    /// </summary>
    public enum FieldVisibility
    {
        /// <summary>
        /// Field is not searchable or visible to anyone (except owner)
        /// Use for sensitive data: birthday, contact info, health data
        /// </summary>
        Private = 0,

        /// <summary>
        /// Field is searchable and visible to all users
        /// Use for: name, bio, basic preferences
        /// </summary>
        Public = 1,

        /// <summary>
        /// Field is searchable/visible only to users in the profile owner's friends list
        /// (Future: requires friendship system implementation)
        /// Use for: personality data, detailed preferences
        /// </summary>
        FriendsOnly = 2
    }
}
