namespace SharpSql
{
    internal enum ObjectState
    {
        Unset,               // Should throw an exception.
        New,                 // New object.
        Record,              // Temporarily in-memory object, but exists in the database.
        Fetched,             // Fetched object from the database.
        Untracked,           // An untracked object, could be new, or existing, and is always dirty.
        ScheduledForDeletion,// Scheduled for deletion.
        Deleted              // Object has been deleted.
    }
}
