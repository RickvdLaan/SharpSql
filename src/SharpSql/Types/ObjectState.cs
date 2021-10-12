namespace SharpSql
{
    /// <summary>
    /// Gets the state of the entity.
    /// </summary>
    public enum ObjectState
    {
        /// <summary>
        /// Should throw an exception.
        /// </summary>
        Unset,
        /// <summary>
        /// New entity object.
        /// </summary>
        New,
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
    }
}
