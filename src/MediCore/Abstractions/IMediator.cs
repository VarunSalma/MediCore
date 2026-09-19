namespace MediCore.Abstractions;

/// <summary>Convenience composition of <see cref="ISender"/> and <see cref="IPublisher"/>.</summary>
public interface IMediator : ISender, IPublisher { }
