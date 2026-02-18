namespace RCommon.Entities
{
    /// <summary>
    /// Marks an entity as capable of being soft-deleted. When soft delete is requested,
    /// the repository will set <see cref="IsDeleted"/> to <c>true</c> instead of
    /// physically removing the record from the data store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is an opt-in capability interface. Entities that do not implement this interface
    /// will continue to be physically deleted. If a caller requests soft delete on an entity
    /// that does not implement this interface, an <see cref="System.InvalidOperationException"/>
    /// is thrown at runtime.
    /// </para>
    /// <para>
    /// <strong>Usage:</strong> To enable soft delete for an entity, implement this interface and
    /// ensure the underlying data store has a corresponding <c>IsDeleted</c> column (boolean).
    /// Then call <c>DeleteAsync(entity, isSoftDelete: true)</c> on the repository. The repository
    /// will set <see cref="IsDeleted"/> to <c>true</c> and perform an UPDATE instead of a DELETE.
    /// </para>
    /// <example>
    /// <code>
    /// public class Customer : BusinessEntity&lt;int&gt;, ISoftDelete
    /// {
    ///     public string Name { get; set; }
    ///     public bool IsDeleted { get; set; }
    /// }
    ///
    /// // Soft delete: sets IsDeleted = true, performs UPDATE
    /// await repository.DeleteAsync(customer, isSoftDelete: true);
    ///
    /// // Physical delete: removes the record entirely
    /// await repository.DeleteAsync(customer, isSoftDelete: false);
    /// </code>
    /// </example>
    /// </remarks>
    public interface ISoftDelete
    {
        /// <summary>
        /// Gets or sets a value indicating whether this entity has been soft-deleted.
        /// When <c>true</c>, the entity is considered deleted but remains in the data store.
        /// </summary>
        bool IsDeleted { get; set; }
    }
}
