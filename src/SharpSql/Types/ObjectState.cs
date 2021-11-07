namespace SharpSql
{
    /// <summary>
    /// Gets the state of the entity.
    /// </summary>
    public enum ObjectState
    {
        /// <summary>
        /// Throws an exception.
        /// </summary>
        Unset,
        /// <summary>
        /// A new entity.
        /// </summary>
        New,
        /// <summary>
        /// Temporarily in-memory immutable object of the new entity.
        /// </summary>
        NewRecord,
        /// <summary>
        /// Temporarily in-memory object, but exists in the database.
        /// </summary>
        Record,
        /// <summary>
        /// Temporarily in-memory object, but its source is unknown.
        /// </summary>
        ExternalRecord,
        /// <summary>
        /// Fetched object from the database.
        /// </summary>
        Fetched,
        /// <summary>
        /// The original immutable fetched value.
        /// </summary>
        OriginalFetchedValue,
        /// <summary>
        /// An untracked object, could be new, or existing, and is always dirty.
        /// </summary>
        Untracked,
        /// <summary>
        /// Scheduled for deletion.
        /// </summary>
        ScheduledForDeletion,
        /// <summary>
        /// Object has been deleted.
        /// </summary>
        Deleted,
        /// <summary>
        /// The entity (changes) have been saved.
        /// </summary>
        Saved
    }
}
