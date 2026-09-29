namespace BounceFit.Mvp
{
    /// <summary>One upgrade option presented when the player levels up.</summary>
    public sealed class UpgradeCard
    {
        public UpgradeCard(string id, string title, string description, string category)
        {
            Id = id;
            Title = title;
            Description = description;
            Category = category;
        }

        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public string Category { get; }
    }
}
