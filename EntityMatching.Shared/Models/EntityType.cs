namespace EntityMatching.Shared.Models
{
    /// <summary>
    /// Defines the type of entity being represented
    /// Enables universal matching across different domains (people, jobs, properties, etc.)
    /// </summary>
    public enum EntityType
    {
        /// <summary>
        /// A person profile (job seeker, dating profile, customer, etc.)
        /// </summary>
        Person = 0,

        /// <summary>
        /// A job posting or position
        /// </summary>
        Job = 1,

        /// <summary>
        /// A real estate property
        /// </summary>
        Property = 2,

        /// <summary>
        /// A product listing
        /// </summary>
        Product = 3,

        /// <summary>
        /// A service offering
        /// </summary>
        Service = 4,

        /// <summary>
        /// An event or activity
        /// </summary>
        Event = 5,

        /// <summary>
        /// An academic major or degree program
        /// </summary>
        Major = 6,

        /// <summary>
        /// A career path or occupation (enriched with O*NET/BLS data)
        /// </summary>
        Career = 7
    }
}
